using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Editor;
using Sandbox;
using SboxUiDesigner.Generation;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// One-shot bulk upgrade tool used after a Designer version bump. Walks every
/// <c>.sui</c> document under the project, loads each via <see cref="SuiAsset"/>
/// (which triggers <see cref="SuiDocumentMigration.Apply"/>), saves the
/// migrated JSON back to disk, and re-emits every generated file.
///
/// <para>Safe to run multiple times — migration + codegen are idempotent.
/// A doc that already matches <see cref="SuiSchemaVersion.Current"/> still
/// passes through the pipeline but the regenerated output equals what's on
/// disk (the writer reports it as <c>Skipped</c>).</para>
///
/// <para>What it touches (see <c>_V15_UPGRADE_AUDIT.md</c> § D):</para>
/// <list type="bullet">
///   <item>Deletes <c>Code/_sui_preview/</c> entirely — fully regenerable on next Preview tab open.</item>
///   <item>Loads each <c>.sui</c>, lets the migration apply, then resaves the JSON.</item>
///   <item>Runs <see cref="SuiGenerationPipeline"/> + <see cref="SuiCompileWriter"/> for each doc
///   that has an <c>Output.RootFolder</c> configured.</item>
///   <item>Never touches <c>*.User.scss</c>, <c>*.partial.cs</c>, or <c>GameConverters.cs</c>
///   user regions.</item>
/// </list>
/// </summary>
public static class SuiForceRegenService
{
	/// <summary>
	/// Aggregate report from a <see cref="RunAsync"/> call. Counts are
	/// per-document; the writer's per-file Generated/Preserved/Skipped/Obsolete
	/// counters are folded into <see cref="FilesWritten"/> for a top-line
	/// summary that doesn't drown the log in 200 lines.
	/// </summary>
	public sealed class RegenReport
	{
		public int TotalSuiFound;
		public int MigratedFromOlderSchema;
		public int Resaved;
		public int CompiledOk;
		public int CompileSkippedNoOutput;
		public int CompileFailed;
		public int FilesWritten;
		public bool PreviewCacheDeleted;
		public readonly List<string> Failures = new();
	}

	/// <summary>
	/// Run the regen pass synchronously. Long enough that the user may see
	/// the editor freeze briefly on huge projects (~100ms per doc on a SSD);
	/// short enough that we don't need a background task or progress modal
	/// at V1.5 scale.
	/// </summary>
	public static RegenReport Run()
	{
		var report = new RegenReport();
		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) )
		{
			report.Failures.Add( "Could not resolve project root — refusing to run." );
			return report;
		}

		// 1) Wipe preview cache. Safe — fully regenerable on next Preview tab open.
		report.PreviewCacheDeleted = TryDeletePreviewCache( projectRoot, report );

		// 2) Enumerate .sui files (skip our own caches / backups / build output).
		var suiFiles = EnumerateSuiFiles( projectRoot );
		report.TotalSuiFound = suiFiles.Count;

		Log.Info( $"[Sui] Force Regen starting — {report.TotalSuiFound} .sui document(s) under project root." );

		// 3) Build the reference resolver ONCE up front. Cascade compile inside
		//    the loop needs it to translate child SuiReference GUIDs to type names.
		var resolver = SuiReferenceResolver.Build();

		int i = 0;
		foreach ( var fullPath in suiFiles )
		{
			i++;
			var rel = ToProjectRelative( projectRoot, fullPath );
			try
			{
				MigrateAndRecompileOne( fullPath, rel, resolver, report, i, report.TotalSuiFound );
			}
			catch ( Exception ex )
			{
				report.Failures.Add( $"{rel}: {ex.Message}" );
				Log.Warning( $"[Sui] Force Regen {i}/{report.TotalSuiFound}: '{rel}' FAILED — {ex.Message}" );
			}
		}

		// 4) Persist "we did this" diagnostic in the designer state.
		try
		{
			var state = SuiDesignerState.Load();
			state.UpgradeRegenCompletedAtLeastOnce = true;
			state.LastForceRegenUtc = DateTime.UtcNow.ToString( "yyyy-MM-ddTHH:mm:ss.fffZ" );
			state.LastSeenSchemaVersion = SuiSchemaVersion.Current;
			state.DismissedUpgradePromptForVersion = SuiSchemaVersion.Current;
			state.Save();
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[Sui] Force Regen: could not update designer-state — {ex.Message}" );
		}

		Log.Info(
			$"[Sui] Force Regen done — total: {report.TotalSuiFound}, " +
			$"migrated: {report.MigratedFromOlderSchema}, resaved: {report.Resaved}, " +
			$"compiled OK: {report.CompiledOk}, no-output: {report.CompileSkippedNoOutput}, " +
			$"failed: {report.CompileFailed}, files written: {report.FilesWritten}." );

		if ( report.Failures.Count > 0 )
		{
			Log.Warning( $"[Sui] Force Regen had {report.Failures.Count} failure(s):" );
			foreach ( var f in report.Failures ) Log.Warning( $"[Sui]   - {f}" );
		}

		return report;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Per-doc pipeline
	// ─────────────────────────────────────────────────────────────────────

	private static void MigrateAndRecompileOne(
		string fullPath, string rel, Func<string, string> resolver,
		RegenReport report, int index, int total )
	{
		// Inspect raw schema version BEFORE load so we can report what changed.
		int rawSchemaVersion = TryReadSchemaVersion( fullPath );
		bool needsMigration = rawSchemaVersion > 0 && rawSchemaVersion < SuiSchemaVersion.Current;
		if ( needsMigration ) report.MigratedFromOlderSchema++;

		Log.Info( $"[Sui] Force Regen {index}/{total}: '{rel}' (schema V{rawSchemaVersion})..." );

		// Use AssetSystem.FindByPath so the load goes through the same engine
		// path the SUI Designer window uses on AssetOpen. This guarantees the
		// resulting GameResource has its file backing wired up for SaveToDisk.
		var assetPath = StripAssetsPrefix( rel );
		var asset = AssetSystem.FindByPath( assetPath );
		if ( asset == null )
		{
			report.Failures.Add( $"{rel}: AssetSystem could not find this path. Try Rebuild Asset Registry first." );
			return;
		}

		var resource = asset.LoadResource<SuiAsset>();
		if ( resource?.Document == null )
		{
			report.Failures.Add( $"{rel}: LoadResource returned null/empty (corrupt JSON?). Skipped." );
			return;
		}

		var doc = resource.Document;

		// Migration happens automatically inside SuiAsset deserialise via
		// SuiDesignerWindow.AssetOpen — but Force Regen bypasses that window,
		// so we apply the migration explicitly here. Idempotent on V3 docs.
		SuiDocumentMigration.Apply( doc );

		// Resave to disk so the schema version bump (and any field rewrites
		// like MigrateTextSizeMode) land on disk.
		resource.Document = doc;
		try
		{
			asset.SaveToDisk( resource );
			report.Resaved++;
		}
		catch ( Exception ex )
		{
			report.Failures.Add( $"{rel}: SaveToDisk failed — {ex.Message}" );
			return;
		}

		// Recompile generated outputs. Skip if the user never configured an
		// output folder (the doc has no generated files to refresh — pure
		// design-time source).
		if ( doc.Output == null || string.IsNullOrEmpty( doc.Output.RootFolder ) )
		{
			report.CompileSkippedNoOutput++;
			return;
		}

		var outputAbs = ResolveOutputFolderAbsolute( doc.Output.RootFolder );

		var ctx = new SuiGenerationContext
		{
			Document = doc,
			Mode = SuiGenerationMode.Final,
			OutputFolder = "",
			ClassName = doc.Output.ClassName,
			Namespace = doc.Output.Namespace,
			ResolveReferencedClass = resolver,
		};

		var generation = SuiGenerationPipeline.Run( ctx );
		if ( generation == null || !generation.Ok )
		{
			report.CompileFailed++;
			var first = generation?.Errors?.Count > 0 ? generation.Errors[0] : "unknown";
			report.Failures.Add( $"{rel}: generation error — {first}" );
			return;
		}

		var compile = SuiCompileWriter.Run( generation, doc, outputAbs );
		if ( compile == null || !compile.Ok )
		{
			report.CompileFailed++;
			var first = compile?.Errors?.Count > 0
				? compile.Errors[0]
				: (compile?.Conflicts?.Count > 0 ? compile.Conflicts[0].ConflictReason : "unknown");
			report.Failures.Add( $"{rel}: compile error — {first}" );
			return;
		}

		report.CompiledOk++;
		report.FilesWritten +=
			(compile.Generated?.Count ?? 0) +
			(compile.Preserved?.Count ?? 0);
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Disk helpers (mirror SuiAssetRegistryService's enumerator)
	// ─────────────────────────────────────────────────────────────────────

	private static List<string> EnumerateSuiFiles( string projectRoot )
	{
		var result = new List<string>();
		try
		{
			foreach ( var f in Directory.EnumerateFiles( projectRoot, "*.sui", SearchOption.AllDirectories ) )
			{
				var rel = ToProjectRelative( projectRoot, f );
				if ( rel.StartsWith( ".sui-cache/", StringComparison.OrdinalIgnoreCase ) ) continue;
				if ( rel.StartsWith( ".sui-backups/", StringComparison.OrdinalIgnoreCase ) ) continue;
				if ( rel.StartsWith( ".git/", StringComparison.OrdinalIgnoreCase ) ) continue;
				if ( rel.Contains( "/bin/", StringComparison.OrdinalIgnoreCase ) ) continue;
				if ( rel.Contains( "/obj/", StringComparison.OrdinalIgnoreCase ) ) continue;
				result.Add( f );
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[Sui] Force Regen: project scan failed — {ex.Message}" );
		}
		return result;
	}

	private static bool TryDeletePreviewCache( string projectRoot, RegenReport report )
	{
		var cacheRoot = Path.Combine( projectRoot, "Code", "_sui_preview" );
		if ( !Directory.Exists( cacheRoot ) ) return false;
		try
		{
			Directory.Delete( cacheRoot, recursive: true );
			Log.Info( $"[Sui] Force Regen: deleted preview cache at '{cacheRoot}'." );
			return true;
		}
		catch ( Exception ex )
		{
			report.Failures.Add( $"preview cache delete failed: {ex.Message}" );
			return false;
		}
	}

	private static int TryReadSchemaVersion( string fullPath )
	{
		try
		{
			var root = JsonNode.Parse( File.ReadAllText( fullPath ) )?.AsObject();
			var doc = root?["Document"]?.AsObject();
			var v = doc?["SchemaVersion"]?.GetValue<int>();
			return v ?? 0;
		}
		catch
		{
			return 0;
		}
	}

	private static string ToProjectRelative( string projectRoot, string fullPath )
		=> Path.GetRelativePath( projectRoot, fullPath ).Replace( '\\', '/' );

	private static string StripAssetsPrefix( string projectRelative )
	{
		if ( string.IsNullOrEmpty( projectRelative ) ) return projectRelative;
		const string prefix = "Assets/";
		if ( projectRelative.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) )
			return projectRelative.Substring( prefix.Length );
		return projectRelative;
	}

	private static string ResolveOutputFolderAbsolute( string storedFolder )
	{
		if ( string.IsNullOrEmpty( storedFolder ) ) return null;
		if ( Path.IsPathRooted( storedFolder ) ) return storedFolder;
		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) ) return storedFolder;
		return Path.GetFullPath( Path.Combine( projectRoot, storedFolder ) );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Detection — called on Designer session start
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Scan every <c>.sui</c> and return the count whose on-disk schema
	/// version is below <see cref="SuiSchemaVersion.Current"/>. Cheap —
	/// reads the version header only, no full deserialise.
	/// </summary>
	public static (int totalCount, int outdatedCount) CountOutdated()
	{
		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) ) return (0, 0);

		var files = EnumerateSuiFiles( projectRoot );
		int outdated = 0;
		foreach ( var f in files )
		{
			var v = TryReadSchemaVersion( f );
			if ( v > 0 && v < SuiSchemaVersion.Current ) outdated++;
		}
		return (files.Count, outdated);
	}
}
