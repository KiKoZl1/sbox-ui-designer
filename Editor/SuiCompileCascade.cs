using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Cascade compile orchestrator. When the user compiles a parent <c>.sui</c>
/// that embeds child <c>.sui</c> docs via <see cref="SuiElementType.SuiReference"/>,
/// every child's generated trio (<c>&lt;Name&gt;.cs</c>, <c>&lt;Name&gt;Panel.razor</c>,
/// <c>&lt;Name&gt;Panel.razor.scss</c>) must already exist with a current API
/// shape — otherwise the C# build fails on missing type references.
///
/// <para>UE5 / UMG do this implicitly: compiling a parent widget recompiles
/// every dependent child widget first. <see cref="EnumerateInCompileOrder"/>
/// walks the SuiReference graph post-order so the caller can iterate the
/// returned list and compile each doc in dependency-respecting order
/// (leaves first, root last). Cycle detection is delegated to
/// <see cref="SuiReferenceCycleDetector.FindCycle"/> — the caller is expected
/// to bail before calling this helper on a cyclic graph.</para>
/// </summary>
public static class SuiCompileCascade
{
	/// <summary>
	/// Post-order DFS walk over <paramref name="root"/>'s SuiReference graph.
	/// Returns docs in compile-safe order: every doc appears AFTER all the
	/// docs it depends on. Root is always the last entry.
	///
	/// <para>Docs that fail to resolve (missing asset, broken registry entry,
	/// etc.) are skipped silently — the parent compile will surface the
	/// downstream error via Compile Results when its emitted markup tag
	/// can't be resolved.</para>
	/// </summary>
	public static List<SuiDocument> EnumerateInCompileOrder( SuiDocument root )
	{
		var result = new List<SuiDocument>();
		if ( root == null ) return result;

		var visited = new HashSet<string>(); // by DocumentId
		Visit( root, result, visited );
		return result;
	}

	private static void Visit( SuiDocument doc, List<SuiDocument> output, HashSet<string> visited )
	{
		if ( doc == null ) return;
		if ( !string.IsNullOrEmpty( doc.DocumentId ) )
		{
			if ( visited.Contains( doc.DocumentId ) ) return;
			visited.Add( doc.DocumentId );
		}

		foreach ( var srcGuid in SuiReferenceCycleDetector.CollectOutgoingRefs( doc ) )
		{
			var childDoc = LoadDocByGuid( srcGuid );
			if ( childDoc != null ) Visit( childDoc, output, visited );
		}

		output.Add( doc );
	}

	/// <summary>
	/// Resolve a SuiReference SourceGuid to its fully-loaded <see cref="SuiDocument"/>.
	/// Mirrors the load path used by <see cref="SuiReferenceResolver"/> (registry
	/// → AssetSystem → LoadResource&lt;SuiAsset&gt;) so generation and cascade
	/// agree on which file a given GUID points to.
	/// </summary>
	private static SuiDocument LoadDocByGuid( string guid )
	{
		if ( string.IsNullOrEmpty( guid ) ) return null;

		try
		{
			var registry = SuiAssetRegistryService.Instance;
			registry.EnsureInitialized();

			var relPath = registry.Registry.Resolve( guid );
			if ( string.IsNullOrEmpty( relPath ) )
			{
				Log.Warning( $"[SUI] Cascade: registry has no entry for '{guid}'." );
				return null;
			}

			var assetPath = StripAssetsPrefix( relPath );
			var asset = AssetSystem.FindByPath( assetPath );
			if ( asset == null )
			{
				Log.Warning( $"[SUI] Cascade: AssetSystem missing '{assetPath}' (registry '{relPath}')." );
				return null;
			}

			var loaded = asset.LoadResource<SuiAsset>();
			return loaded?.Document;
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[SUI] Cascade: failed to load doc for GUID '{guid}': {ex.Message}" );
			return null;
		}
	}

	private static string StripAssetsPrefix( string projectRelative )
	{
		if ( string.IsNullOrEmpty( projectRelative ) ) return projectRelative;
		const string prefix = "Assets/";
		if ( projectRelative.StartsWith( prefix, System.StringComparison.OrdinalIgnoreCase ) )
			return projectRelative.Substring( prefix.Length );
		return projectRelative;
	}
}
