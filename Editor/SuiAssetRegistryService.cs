using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Editor-side owner of the <see cref="SuiAssetRegistry"/> (PRD 22 § 3). Performs
/// ALL disk access — cold build (walking the project for .sui files), reading
/// document headers, persisting the JSON cache at
/// <c>&lt;projectRoot&gt;/.sui-cache/asset-registry.json</c>, and running the
/// <see cref="SuiAssetRegistryWatcher"/>. The registry itself stays sandbox-clean.
///
/// Singleton, lazily initialised on first SUI Designer window open
/// (<see cref="EnsureInitialized"/> — idempotent).
/// </summary>
public sealed class SuiAssetRegistryService
{
	private static SuiAssetRegistryService _instance;
	public static SuiAssetRegistryService Instance => _instance ??= new SuiAssetRegistryService();

	/// <summary>The in-memory registry. Sandbox-clean; this service is its disk layer.</summary>
	public SuiAssetRegistry Registry { get; } = new();

	private string _projectRoot;

	/// <summary>Project root directory as resolved by the bootstrap. Null until <see cref="EnsureInitialized"/> succeeds.</summary>
	public string ProjectRoot => _projectRoot;
	private string _cacheDir;
	private string _cacheFile;
	private bool _initialized;
	private SuiAssetRegistryWatcher _watcher;

	private SuiAssetRegistryService() { }

	// ─────────────────────────────────────────────────────────────────────
	//  Bootstrap (PRD 22 § 3.6)
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Resolve the project root, load the cached registry if it is fresh and
	/// intact, otherwise run a cold build, then hook the file-system watcher.
	/// Idempotent — safe to call on every SUI Designer window open.
	/// </summary>
	public void EnsureInitialized()
	{
		if ( _initialized ) return;
		if ( !ResolvePaths() ) return;

		if ( !TryLoadCache() )
			Rebuild();

		StartWatcher();
		_initialized = true;
	}

	private bool ResolvePaths()
	{
		if ( !string.IsNullOrEmpty( _projectRoot ) ) return true;

		_projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( _projectRoot ) )
		{
			Log.Warning( "[SUI] Asset Registry: could not resolve project root — registry disabled this session." );
			return false;
		}

		_cacheDir = Path.Combine( _projectRoot, ".sui-cache" );
		_cacheFile = Path.Combine( _cacheDir, "asset-registry.json" );
		return true;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Cold build + per-file refresh (PRD 22 § 3.3)
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>Full rebuild from disk — walks the project for .sui files, re-reads every header (PRD 22 § 3.3.1).</summary>
	public void Rebuild()
	{
		if ( !ResolvePaths() ) return;

		var sw = System.Diagnostics.Stopwatch.StartNew();
		Registry.Clear();

		int count = 0;
		foreach ( var file in EnumerateSuiFiles() )
		{
			if ( TryReadHeader( file, out var guid, out var name ) )
			{
				Registry.AddOrUpdate( guid, ToProjectRelative( file ), name );
				count++;
			}
		}

		Registry.LastBuilt = NowIso();
		sw.Stop();
		Persist();

		Log.Info( $"[SUI] Asset Registry rebuilt: {count} document(s) in {sw.ElapsedMilliseconds}ms"
			+ ( Registry.HasConflicts ? $" — {Registry.Conflicts.Count} duplicate-GUID conflict(s)!" : "" ) );
	}

	/// <summary>Re-read a single .sui file and refresh its entry (call after a save).</summary>
	public void RefreshFile( string fullPath )
	{
		if ( !ResolvePaths() || string.IsNullOrEmpty( fullPath ) ) return;
		if ( !File.Exists( fullPath ) ) { RemoveFile( fullPath ); return; }
		if ( TryReadHeader( fullPath, out var guid, out var name ) )
		{
			Registry.AddOrUpdate( guid, ToProjectRelative( fullPath ), name );
			Persist();
		}
	}

	/// <summary>Drop the entry for a deleted .sui file.</summary>
	public void RemoveFile( string fullPath )
	{
		if ( !ResolvePaths() || string.IsNullOrEmpty( fullPath ) ) return;
		Registry.RemoveByPath( ToProjectRelative( fullPath ) );
		Persist();
	}

	/// <summary>
	/// Drain any file-system changes the watcher queued on its background thread,
	/// applying them on the calling (main) thread. Cheap when the queue is empty.
	/// Bursts (e.g. a git checkout) are coalesced — last change per path wins.
	/// PRD 22 § 3.3.3 + § 7A.5.
	/// </summary>
	public void PumpPendingChanges()
	{
		if ( _watcher == null ) return;

		var coalesced = new Dictionary<string, SuiAssetRegistryWatcher.Change>( StringComparer.OrdinalIgnoreCase );
		while ( _watcher.TryDequeue( out var change ) )
			coalesced[change.FullPath] = change;

		if ( coalesced.Count == 0 ) return;

		foreach ( var change in coalesced.Values )
		{
			if ( change.Kind == SuiAssetRegistryWatcher.ChangeKind.Deleted )
			{
				Registry.RemoveByPath( ToProjectRelative( change.FullPath ) );
				continue;
			}

			if ( File.Exists( change.FullPath ) && TryReadHeader( change.FullPath, out var guid, out var name ) )
				Registry.AddOrUpdate( guid, ToProjectRelative( change.FullPath ), name );

			if ( !string.IsNullOrEmpty( change.OldFullPath ) )
				Registry.RemoveByPath( ToProjectRelative( change.OldFullPath ) );
		}

		Persist();
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Persistence (PRD 22 § 3.2, § 3.7)
	// ─────────────────────────────────────────────────────────────────────

	private bool TryLoadCache()
	{
		try
		{
			if ( !File.Exists( _cacheFile ) ) return false;
			if ( !Registry.LoadFromJson( File.ReadAllText( _cacheFile ) ) ) return false;

			// Stale if older than 24h, or if any cached path no longer exists on disk.
			if ( !DateTime.TryParse( Registry.LastBuilt, out var built )
				|| ( DateTime.UtcNow - built.ToUniversalTime() ).TotalHours > 24 )
				return false;

			foreach ( var kv in Registry.Entries )
			{
				var abs = Path.Combine( _projectRoot, kv.Value.Path.Replace( '/', Path.DirectorySeparatorChar ) );
				if ( !File.Exists( abs ) ) return false;
			}

			Log.Info( $"[SUI] Asset Registry: loaded cache ({Registry.Entries.Count} document(s))." );
			return true;
		}
		catch
		{
			return false; // corrupt cache → cold build
		}
	}

	private void Persist()
	{
		try
		{
			if ( string.IsNullOrEmpty( _cacheDir ) ) return;
			Directory.CreateDirectory( _cacheDir );
			File.WriteAllText( _cacheFile, Registry.ToJson() );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[SUI] Asset Registry: failed to persist cache — {e.Message}" );
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  File-system watcher
	// ─────────────────────────────────────────────────────────────────────

	private void StartWatcher()
	{
		try
		{
			_watcher?.Dispose();
			_watcher = new SuiAssetRegistryWatcher( _projectRoot );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[SUI] Asset Registry: file watcher unavailable — {e.Message}. "
				+ "Falling back to per-save refresh + manual Rebuild." );
			_watcher = null;
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Disk helpers
	// ─────────────────────────────────────────────────────────────────────

	private IEnumerable<string> EnumerateSuiFiles()
	{
		List<string> files;
		try
		{
			files = new List<string>( Directory.EnumerateFiles( _projectRoot, "*.sui", SearchOption.AllDirectories ) );
		}
		catch ( Exception e )
		{
			Log.Warning( $"[SUI] Asset Registry: project scan failed — {e.Message}" );
			return new List<string>();
		}

		var filtered = new List<string>( files.Count );
		foreach ( var f in files )
		{
			var rel = ToProjectRelative( f );
			if ( rel.StartsWith( ".sui-cache/", StringComparison.OrdinalIgnoreCase ) ) continue;
			if ( rel.StartsWith( ".sui-backups/", StringComparison.OrdinalIgnoreCase ) ) continue;
			if ( rel.StartsWith( ".git/", StringComparison.OrdinalIgnoreCase ) ) continue;
			if ( rel.Contains( "/bin/", StringComparison.OrdinalIgnoreCase ) ) continue;
			if ( rel.Contains( "/obj/", StringComparison.OrdinalIgnoreCase ) ) continue;
			filtered.Add( f );
		}
		return filtered;
	}

	/// <summary>Reads a .sui document's header — extracts <c>Document.DocumentId</c> + <c>Document.Name</c>.</summary>
	private static bool TryReadHeader( string fullPath, out string guid, out string name )
	{
		guid = null;
		name = null;
		try
		{
			var root = JsonNode.Parse( File.ReadAllText( fullPath ) )?.AsObject();
			var doc = root?["Document"]?.AsObject();
			if ( doc == null ) return false;
			guid = doc["DocumentId"]?.GetValue<string>();
			name = doc["Name"]?.GetValue<string>();
			return !string.IsNullOrEmpty( guid );
		}
		catch
		{
			return false; // corrupt / hand-edited bad JSON → skip (PRD 22 § 3.9)
		}
	}

	private string ToProjectRelative( string fullPath )
		=> Path.GetRelativePath( _projectRoot, fullPath ).Replace( '\\', '/' );

	private static string NowIso()
		=> DateTime.UtcNow.ToString( "yyyy-MM-ddTHH:mm:ss.fffZ" );
}
