using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// In-memory <c>SourceGuid → path</c> index for .sui documents (PRD 22 § 3).
/// Every <c>SuiReference.SourceGuid</c> resolves through this.
///
/// This type is <b>sandbox-clean — it performs NO file I/O.</b> The editor-side
/// <c>SuiAssetRegistryService</c> owns all disk access (walking the project,
/// reading document headers, the FileSystemWatcher, persisting the JSON cache)
/// and feeds this registry through <see cref="AddOrUpdate"/> /
/// <see cref="RemoveByPath"/> / <see cref="UpdatePath"/>. Persistence is the
/// IO-free <see cref="ToJson"/> / <see cref="LoadFromJson"/> pair.
///
/// (PRD 22 § 3.5 originally listed <c>Rebuild()</c>/<c>Refresh()</c> on this type;
/// the s&amp;box runtime sandbox blocks <c>System.IO</c>, so all disk-touching
/// operations were moved to the editor service — see the M0 patch note in PRD 22.)
/// </summary>
public sealed class SuiAssetRegistry
{
	/// <summary>Registry-file schema version — bumps independently of the .sui document schema.</summary>
	public const int FileSchemaVersion = 1;

	private readonly Dictionary<string, SuiAssetEntry> _entries = new();
	private readonly Dictionary<string, SuiAssetConflict> _conflicts = new();

	/// <summary>ISO-8601 UTC timestamp of the last full cold build.</summary>
	public string LastBuilt { get; set; }

	public IReadOnlyDictionary<string, SuiAssetEntry> Entries => _entries;
	public IReadOnlyCollection<SuiAssetConflict> Conflicts => _conflicts.Values;
	public bool HasConflicts => _conflicts.Count > 0;

	public event Action<string> EntryAdded;       // GUID
	public event Action<string> EntryRemoved;     // GUID
	public event Action<string> EntryChanged;     // GUID (path or name updated)
	public event Action<string> ConflictDetected; // GUID

	// ---------- lookup ----------

	/// <summary>Resolve a SourceGuid to a project-relative path. Returns null if unknown or conflicted.</summary>
	public string Resolve( string sourceGuid )
	{
		if ( string.IsNullOrEmpty( sourceGuid ) ) return null;
		return _entries.TryGetValue( sourceGuid, out var e ) ? e.Path : null;
	}

	/// <summary>Reverse lookup: project-relative path → SourceGuid. Null if not indexed.</summary>
	public string GuidForPath( string projectRelativePath )
	{
		if ( string.IsNullOrEmpty( projectRelativePath ) ) return null;
		var norm = Normalize( projectRelativePath );
		foreach ( var kv in _entries )
		{
			if ( string.Equals( kv.Value.Path, norm, StringComparison.OrdinalIgnoreCase ) )
				return kv.Key;
		}
		return null;
	}

	/// <summary>True if this GUID is currently in a duplicate-GUID conflict.</summary>
	public bool IsConflicted( string sourceGuid )
		=> !string.IsNullOrEmpty( sourceGuid ) && _conflicts.ContainsKey( sourceGuid );

	// ---------- mutation (fed by the editor service) ----------

	/// <summary>
	/// Register (or refresh) a document. If <paramref name="guid"/> is already
	/// mapped to a <i>different</i> path, this is a duplicate-GUID conflict: the
	/// entry is removed and the GUID is recorded in <see cref="Conflicts"/>
	/// (PRD 22 § 3.4). Re-registering the same path just refreshes Name/LastSeen.
	/// </summary>
	public void AddOrUpdate( string guid, string projectRelativePath, string name, string lastSeenIso = null )
	{
		if ( string.IsNullOrEmpty( guid ) || string.IsNullOrEmpty( projectRelativePath ) ) return;

		var path = Normalize( projectRelativePath );
		var stamp = lastSeenIso ?? NowIso();

		// Already conflicted — just track the additional path, stay unresolvable.
		if ( _conflicts.TryGetValue( guid, out var existingConflict ) )
		{
			if ( !existingConflict.Paths.Contains( path ) )
				existingConflict.Paths.Add( path );
			return;
		}

		if ( _entries.TryGetValue( guid, out var entry ) )
		{
			// Same file — plain refresh of Name/LastSeen.
			if ( string.Equals( entry.Path, path, StringComparison.OrdinalIgnoreCase ) )
			{
				var changed = entry.Name != name;
				entry.Name = name;
				entry.LastSeen = stamp;
				if ( changed ) EntryChanged?.Invoke( guid );
				return;
			}

			// A different file declares the same GUID — conflict.
			var conflict = new SuiAssetConflict { Guid = guid, FirstSeen = NowIso() };
			conflict.Paths.Add( entry.Path );
			conflict.Paths.Add( path );
			_conflicts[guid] = conflict;
			_entries.Remove( guid );
			EntryRemoved?.Invoke( guid );
			ConflictDetected?.Invoke( guid );
			return;
		}

		_entries[guid] = new SuiAssetEntry { Path = path, Name = name, LastSeen = stamp };
		EntryAdded?.Invoke( guid );
	}

	/// <summary>
	/// Update the stored path for a GUID after an out-of-editor move/rename
	/// (the GUID is stable, only the path changed).
	/// </summary>
	public void UpdatePath( string guid, string newProjectRelativePath )
	{
		if ( string.IsNullOrEmpty( guid ) ) return;
		if ( !_entries.TryGetValue( guid, out var entry ) ) return;
		entry.Path = Normalize( newProjectRelativePath );
		entry.LastSeen = NowIso();
		EntryChanged?.Invoke( guid );
	}

	/// <summary>Remove whatever entry (or conflict path) currently lives at <paramref name="projectRelativePath"/>.</summary>
	public void RemoveByPath( string projectRelativePath )
	{
		if ( string.IsNullOrEmpty( projectRelativePath ) ) return;
		var path = Normalize( projectRelativePath );

		string removeGuid = null;
		foreach ( var kv in _entries )
		{
			if ( string.Equals( kv.Value.Path, path, StringComparison.OrdinalIgnoreCase ) )
			{
				removeGuid = kv.Key;
				break;
			}
		}
		if ( removeGuid != null )
		{
			_entries.Remove( removeGuid );
			EntryRemoved?.Invoke( removeGuid );
		}

		// The path may also be one side of a conflict — drop it. If a conflict
		// collapses to a single remaining path it is no longer a conflict; the
		// editor service re-reads the survivor and re-registers it.
		var collapsed = new List<string>();
		foreach ( var kv in _conflicts )
		{
			kv.Value.Paths.RemoveAll( p => string.Equals( p, path, StringComparison.OrdinalIgnoreCase ) );
			if ( kv.Value.Paths.Count <= 1 )
				collapsed.Add( kv.Key );
		}
		foreach ( var guid in collapsed )
			_conflicts.Remove( guid );
	}

	public void Clear()
	{
		_entries.Clear();
		_conflicts.Clear();
	}

	// ---------- IO-free persistence ----------

	/// <summary>Serialise the registry to the on-disk JSON shape (PRD 22 § 3.2).</summary>
	public string ToJson()
	{
		var root = new JsonObject
		{
			["SchemaVersion"] = FileSchemaVersion,
			["LastBuilt"] = LastBuilt ?? NowIso(),
		};

		var entries = new JsonObject();
		foreach ( var kv in _entries )
		{
			entries[kv.Key] = new JsonObject
			{
				["Path"] = kv.Value.Path,
				["Name"] = kv.Value.Name,
				["LastSeen"] = kv.Value.LastSeen,
			};
		}
		root["Entries"] = entries;

		var conflicts = new JsonArray();
		foreach ( var c in _conflicts.Values )
		{
			var paths = new JsonArray();
			foreach ( var p in c.Paths ) paths.Add( p );
			conflicts.Add( new JsonObject
			{
				["Guid"] = c.Guid,
				["Paths"] = paths,
				["FirstSeen"] = c.FirstSeen,
			} );
		}
		root["Conflicts"] = conflicts;

		return root.ToJsonString( new JsonSerializerOptions { WriteIndented = true } );
	}

	/// <summary>
	/// Replace all in-memory state from a persisted registry JSON string.
	/// Tolerant of malformed input — clears state and returns false.
	/// </summary>
	public bool LoadFromJson( string json )
	{
		Clear();
		if ( string.IsNullOrWhiteSpace( json ) ) return false;

		try
		{
			var root = JsonNode.Parse( json )?.AsObject();
			if ( root == null ) return false;

			LastBuilt = root["LastBuilt"]?.GetValue<string>();

			if ( root["Entries"] is JsonObject entries )
			{
				foreach ( var kv in entries )
				{
					if ( kv.Value is not JsonObject e ) continue;
					_entries[kv.Key] = new SuiAssetEntry
					{
						Path = e["Path"]?.GetValue<string>(),
						Name = e["Name"]?.GetValue<string>(),
						LastSeen = e["LastSeen"]?.GetValue<string>(),
					};
				}
			}

			if ( root["Conflicts"] is JsonArray conflicts )
			{
				foreach ( var node in conflicts )
				{
					if ( node is not JsonObject c ) continue;
					var conflict = new SuiAssetConflict
					{
						Guid = c["Guid"]?.GetValue<string>(),
						FirstSeen = c["FirstSeen"]?.GetValue<string>(),
					};
					if ( c["Paths"] is JsonArray pl )
					{
						foreach ( var p in pl )
							if ( p != null ) conflict.Paths.Add( p.GetValue<string>() );
					}
					if ( !string.IsNullOrEmpty( conflict.Guid ) )
						_conflicts[conflict.Guid] = conflict;
				}
			}
			return true;
		}
		catch
		{
			Clear();
			return false;
		}
	}

	// ---------- helpers ----------

	private static string Normalize( string path )
		=> string.IsNullOrEmpty( path ) ? path : path.Replace( '\\', '/' );

	private static string NowIso()
		=> DateTime.UtcNow.ToString( "yyyy-MM-ddTHH:mm:ss.fffZ" );
}
