using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Sandbox;
using SboxUiDesigner.Generation;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Final-mode compile writer — takes the in-memory <see cref="SuiGenerationResult"/>
/// and writes it to the user's output folder, honoring file ownership rules:
///
///  1. New file → write (Generated).
///  2. Existing file with our header + same hash → no-op (Skipped).
///  3. Existing file with our header + different hash → backup the prior
///     content into a timestamped folder, then overwrite (Preserved).
///  4. Existing file without our header (or another doc's header) → BLOCK.
///     Don't touch. Report as Conflict so the user can move/rename it.
///
/// PRD reference: doc 11 § Conflict rules + § Warning overwrite + § Block overwrite.
///
/// Manifest persistence: `&lt;outputFolder&gt;/.sui-manifest/&lt;DocumentId&gt;.json`.
/// Per-doc isolation lets multiple .sui files share an output folder safely.
/// </summary>
public static class SuiCompileWriter
{
	private const string ManifestSubFolder = ".sui-manifest";
	private const string BackupSubFolder = "sui-generated-backups";

	/// <summary>
	/// Write the generation result to disk under <paramref name="outputFolderAbs"/>.
	/// Returns a classification report. Caller decides how to surface it.
	/// </summary>
	public static SuiCompileResult Run(
		SuiGenerationResult generation,
		SuiDocument document,
		string outputFolderAbs )
	{
		var result = new SuiCompileResult { OutputFolder = outputFolderAbs };

		// Propagate generator errors/warnings up so the user sees them in one
		// surface (the Compile Results dock).
		foreach ( var w in generation?.Warnings ?? new List<string>() ) result.Warnings.Add( w );
		foreach ( var e in generation?.Errors ?? new List<string>() ) result.Errors.Add( e );

		if ( generation == null || !generation.Ok )
			return result; // bail before touching disk
		if ( document == null )
		{
			result.Errors.Add( "compile: document is null" );
			return result;
		}
		if ( string.IsNullOrEmpty( outputFolderAbs ) )
		{
			result.Errors.Add( "compile: output folder not configured" );
			return result;
		}

		try
		{
			Directory.CreateDirectory( outputFolderAbs );
		}
		catch ( Exception ex )
		{
			result.Errors.Add( $"compile: failed to create output folder '{outputFolderAbs}': {ex.Message}" );
			return result;
		}

		var manifest = LoadManifest( outputFolderAbs, document.DocumentId )
			?? new SuiGeneratedFileManifest();
		var oldManifest = manifest.Clone();
		var newManifest = new SuiGeneratedFileManifest();

		// Backups must live OUTSIDE the output folder if that folder is inside
		// `Code/` — otherwise the engine's Razor compiler picks up the backup
		// .razor files and tries to declare `partial class <Name>` a second
		// time, blowing up with CS0111. Resolve to project-root `.sui-backups`
		// when we have a project root; otherwise nest under output as before.
		var projectRootForBackups = Sandbox.Project.Current?.RootDirectory?.FullName;
		var backupRootBase = !string.IsNullOrEmpty( projectRootForBackups )
			? Path.Combine( projectRootForBackups, ".sui-backups" )
			: Path.Combine( outputFolderAbs, BackupSubFolder );

		// One backup folder per compile session (only created lazily on first preserve).
		var backupRoot = Path.Combine(
			backupRootBase,
			SafeName( document.Name ?? document.DocumentId ),
			DateTime.UtcNow.ToString( "yyyy-MM-dd_HHmmss" ) );

		foreach ( var file in generation.Files )
		{
			if ( string.IsNullOrEmpty( file.Path ) )
			{
				result.Warnings.Add( $"compile: skipped file with empty path (kind={file.Kind})" );
				continue;
			}

			var abs = Path.GetFullPath( Path.Combine( outputFolderAbs, file.Path ) );

			// Defense in depth: refuse to write outside the output folder.
			if ( !abs.StartsWith( Path.GetFullPath( outputFolderAbs ), StringComparison.OrdinalIgnoreCase ) )
			{
				result.Conflicts.Add( new SuiCompileFileEntry
				{
					AbsolutePath = abs,
					RelativePath = file.Path,
					ConflictReason = "Output path escapes the configured output folder. Refusing to write.",
				} );
				continue;
			}

			var entry = new SuiCompileFileEntry
			{
				AbsolutePath = abs,
				RelativePath = file.Path,
				Sha256 = file.Sha256,
			};

			if ( !File.Exists( abs ) )
			{
				if ( !TryWriteAtomic( abs, file.Content, out var writeErr ) )
				{
					result.Errors.Add( $"compile: write failed for '{file.Path}': {writeErr}" );
					continue;
				}
				result.Generated.Add( entry );
				AddOrUpdateManifest( newManifest, file, document );
				continue;
			}

			// File exists. Inspect its header to decide ownership.
			string existing;
			try { existing = File.ReadAllText( abs ); }
			catch ( Exception ex )
			{
				result.Errors.Add( $"compile: read failed for '{file.Path}': {ex.Message}" );
				continue;
			}

			var header = SuiHeaderEmitter.Parse( existing );
			var ownedByUs = header != null && header.MatchesDocument( document );

			if ( !ownedByUs )
			{
				// Conflict: someone else's file (or hand-edited stripped header).
				var ownerHint = header?.DocumentId ?? "(no SUI header)";
				entry.ConflictReason = $"File exists without ownership match. Header DocumentId: {ownerHint}. Move or rename it before recompiling.";
				result.Conflicts.Add( entry );
				continue;
			}

			// Owned by us. Compare hashes.
			var existingHash = SuiHashUtility.Sha256( existing );
			if ( string.Equals( existingHash, file.Sha256, StringComparison.Ordinal ) )
			{
				result.Skipped.Add( entry );
				AddOrUpdateManifest( newManifest, file, document );
				continue;
			}

			// Owned + content differs → backup + overwrite.
			var backupAbs = Path.Combine( backupRoot, file.Path );
			try
			{
				Directory.CreateDirectory( Path.GetDirectoryName( backupAbs ) );
				File.Copy( abs, backupAbs, overwrite: false );
			}
			catch ( Exception ex )
			{
				result.Errors.Add( $"compile: backup failed for '{file.Path}': {ex.Message}. File NOT overwritten." );
				continue;
			}

			if ( !TryWriteAtomic( abs, file.Content, out var writeErr2 ) )
			{
				result.Errors.Add( $"compile: write failed for '{file.Path}' (backup at '{backupAbs}'): {writeErr2}" );
				continue;
			}

			entry.BackupPath = backupAbs;
			result.Preserved.Add( entry );
			result.BackupFolder = backupRoot;
			AddOrUpdateManifest( newManifest, file, document );
		}

		// User-owned sidecar — write `<className>.User.scss` once next to each
		// generated .razor.scss. Subsequent compiles never overwrite. The
		// generator emits an `@import` for it inside the main .scss so user
		// edits flow into the cascade without touching the generated file.
		EmitUserScssSidecars( generation, outputFolderAbs, result );

		// Detect obsolete: anything in the old manifest that's not in the new one.
		foreach ( var oldEntry in oldManifest.GeneratedFiles )
		{
			if ( newManifest.FindByPath( oldEntry.Path ) != null ) continue;

			// Old manifest entry didn't show up in new generation → obsolete.
			var abs = Path.GetFullPath( Path.Combine( outputFolderAbs, oldEntry.Path ) );
			result.Obsolete.Add( new SuiCompileFileEntry
			{
				AbsolutePath = abs,
				RelativePath = oldEntry.Path,
				Sha256 = oldEntry.LastHash,
			} );
		}

		// Persist new manifest only if no errors (so a partial failure doesn't poison state).
		if ( result.Errors.Count == 0 )
		{
			SaveManifest( outputFolderAbs, document.DocumentId, newManifest, result );
		}

		return result;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Manifest IO
	// ─────────────────────────────────────────────────────────────────────

	private static string ManifestPath( string outputFolderAbs, string documentId )
		=> Path.Combine( outputFolderAbs, ManifestSubFolder, $"{SafeName( documentId )}.json" );

	private static SuiGeneratedFileManifest LoadManifest( string outputFolderAbs, string documentId )
	{
		var path = ManifestPath( outputFolderAbs, documentId );
		if ( !File.Exists( path ) ) return null;
		try
		{
			var json = File.ReadAllText( path );
			return JsonSerializer.Deserialize<SuiGeneratedFileManifest>( json )
				?? new SuiGeneratedFileManifest();
		}
		catch
		{
			// Corrupt manifest = start fresh. Don't trash user data; just rebuild.
			return null;
		}
	}

	private static void SaveManifest(
		string outputFolderAbs, string documentId,
		SuiGeneratedFileManifest manifest, SuiCompileResult result )
	{
		try
		{
			var path = ManifestPath( outputFolderAbs, documentId );
			Directory.CreateDirectory( Path.GetDirectoryName( path ) );
			var json = JsonSerializer.Serialize( manifest, new JsonSerializerOptions { WriteIndented = true } );
			File.WriteAllText( path, json );
		}
		catch ( Exception ex )
		{
			result.Warnings.Add( $"compile: failed to write manifest: {ex.Message}. Files were written but tracking is now inconsistent." );
		}
	}

	private static void AddOrUpdateManifest(
		SuiGeneratedFileManifest manifest, SuiGeneratedFile file, SuiDocument doc )
	{
		manifest.GeneratedFiles.Add( new SuiGeneratedFileEntry
		{
			Kind = file.Kind,
			Path = file.Path,
			LastHash = file.Sha256,
			OwnedByDocumentId = doc.DocumentId,
			GeneratorVersion = doc.DesignerVersion,
		} );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  User-owned sidecar (.User.scss)
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// For each generated SCSS file, write a `&lt;baseName&gt;.User.scss` sidecar
	/// next to it ONCE. If the sidecar already exists it is left untouched
	/// (user-owned). Records all sidecars in <c>result.UserOwned</c> so the
	/// Compile Results UI can surface them.
	/// </summary>
	private static void EmitUserScssSidecars(
		SuiGenerationResult generation, string outputFolderAbs, SuiCompileResult result )
	{
		foreach ( var file in generation.Files )
		{
			if ( file?.Path == null ) continue;
			if ( file.Kind != SuiGeneratedFileKind.Scss ) continue;
			if ( !file.Path.EndsWith( ".razor.scss", StringComparison.OrdinalIgnoreCase ) ) continue;

			// Strip ".razor.scss" → keep just the base, then append ".User.scss".
			var baseName = file.Path.Substring( 0, file.Path.Length - ".razor.scss".Length );
			var userRelative = baseName + ".User.scss";
			var userAbs = Path.GetFullPath( Path.Combine( outputFolderAbs, userRelative ) );

			// Defense in depth — same as the main writer.
			if ( !userAbs.StartsWith( Path.GetFullPath( outputFolderAbs ), StringComparison.OrdinalIgnoreCase ) )
				continue;

			var entry = new SuiCompileFileEntry
			{
				AbsolutePath = userAbs,
				RelativePath = userRelative,
			};

			if ( File.Exists( userAbs ) )
			{
				// Already there — leave it alone. Record so user sees it was preserved.
				result.UserOwned.Add( entry );
				continue;
			}

			// Create boilerplate. Use the type name from the path (basename without
			// the directory) as the outer selector hint.
			var typeNameHint = Path.GetFileNameWithoutExtension( baseName );
			var content =
				$"// {typeNameHint}.User.scss — your custom styles for {typeNameHint}.\n" +
				$"// This file is created once and never overwritten by the SUI compiler.\n" +
				$"// Edit freely — your rules win the cascade because the generated\n" +
				$"// .razor.scss imports this file last.\n\n" +
				$"{typeNameHint} {{\n" +
				$"\t// Example:\n" +
				$"\t// .my-class {{ color: red; }}\n" +
				$"}}\n";

			if ( !TryWriteAtomic( userAbs, content, out var writeErr ) )
			{
				result.Warnings.Add( $"compile: failed to create user sidecar '{userRelative}': {writeErr}" );
				continue;
			}
			result.UserOwned.Add( entry );
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Atomic write (mirror of SuiPreviewCacheWriter pattern)
	// ─────────────────────────────────────────────────────────────────────

	private static bool TryWriteAtomic( string absPath, string content, out string error )
	{
		error = null;
		try
		{
			Directory.CreateDirectory( Path.GetDirectoryName( absPath ) );
			var tmp = absPath + ".tmp";
			File.WriteAllText( tmp, content ?? "" );
			try
			{
				if ( File.Exists( absPath ) ) File.Replace( tmp, absPath, null );
				else File.Move( tmp, absPath );
			}
			catch
			{
				// Fallback for cross-volume / permission edge cases.
				File.Copy( tmp, absPath, overwrite: true );
				try { File.Delete( tmp ); } catch { }
			}
			return true;
		}
		catch ( Exception ex )
		{
			error = ex.Message;
			return false;
		}
	}

	private static string SafeName( string raw )
	{
		if ( string.IsNullOrEmpty( raw ) ) return "_";
		var chars = raw.ToCharArray();
		for ( int i = 0; i < chars.Length; i++ )
		{
			var c = chars[i];
			if ( char.IsLetterOrDigit( c ) || c == '_' || c == '-' || c == '.' ) continue;
			chars[i] = '_';
		}
		return new string( chars );
	}
}
