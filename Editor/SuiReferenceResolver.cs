using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Generation;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Editor-side resolver that turns a SuiReference <c>SourceGuid</c> into a
/// <see cref="SuiReferenceTarget"/> (Namespace + ClassName + AcceptedProps)
/// the generator needs to emit the embedded child tag (PRD 19 § 7.1).
///
/// <para>Uses <see cref="SuiAssetRegistryService"/> to resolve GUID → path,
/// then reads the referenced <c>.sui</c> JSON and pulls the <c>Output</c>
/// + <c>AcceptedProps</c> blocks. Cached per <see cref="Build"/> call so a
/// generation pass touches each child file at most once.</para>
/// </summary>
public static class SuiReferenceResolver
{
	/// <summary>
	/// Build a resolver function that the generator can hand SuiReference GUIDs to.
	/// Caches results across calls within the same generation. Returns null when
	/// the GUID is unknown, the file is missing, or the doc has no Output.ClassName.
	/// </summary>
	public static System.Func<string, SuiReferenceTarget> Build()
	{
		// Only cache successful resolves so a transient registry miss during
		// bootstrap doesn't poison the cache for the rest of the session.
		var cache = new Dictionary<string, SuiReferenceTarget>();
		var registry = SuiAssetRegistryService.Instance;

		return guid =>
		{
			if ( string.IsNullOrEmpty( guid ) ) return null;
			if ( cache.TryGetValue( guid, out var cached ) && cached != null ) return cached;

			try
			{
				registry.EnsureInitialized();
				var relPath = registry.Registry.Resolve( guid );
				if ( string.IsNullOrEmpty( relPath ) )
				{
					Log.Warning( $"[SUI] SuiReferenceResolver: registry has no entry for '{guid}'." );
					return null;
				}

				// Use AssetSystem so engine converters (SuiScaleMode etc) apply.
				// System.Text.Json doesn't know how to deserialize these and
				// throws on every load. AssetSystem.FindByPath wants a path
				// relative to Assets/ — strip the "Assets/" prefix the registry
				// stores so the lookup hits.
				var assetPath = StripAssetsPrefix( relPath );
				var asset = AssetSystem.FindByPath( assetPath );
				if ( asset == null )
				{
					Log.Warning( $"[SUI] SuiReferenceResolver: AssetSystem couldn't find '{assetPath}' (registry path '{relPath}') for GUID '{guid}'." );
					return null;
				}

				var loaded = asset.LoadResource<SuiAsset>();
				var doc = loaded?.Document;
				if ( doc == null )
				{
					Log.Warning( $"[SUI] SuiReferenceResolver: LoadResource returned null for '{relPath}'." );
					return null;
				}

				// Migrate legacy AcceptedProps → public Variables before reading
				// so codegen sees the V1.5-M2-K shape regardless of when the
				// referenced .sui was last saved.
				doc.MigrateAcceptedPropsToPublicVariables();

				var className = doc.Output?.ClassName;
				if ( string.IsNullOrEmpty( className ) )
				{
					Log.Warning( $"[SUI] SuiReferenceResolver: '{relPath}' has no Output.ClassName — codegen embed will be skipped." );
					return null;
				}

				// Only the IsPublic Variables become props parents can set.
				var publics = new System.Collections.Generic.List<SuiVariable>();
				if ( doc.Variables != null )
				{
					foreach ( var v in doc.Variables )
						if ( v != null && v.IsPublic ) publics.Add( v );
				}

				var target = new SuiReferenceTarget
				{
					Namespace = doc.Output?.Namespace,
					ClassName = className,
					PublicVariables = publics,
				};
				cache[guid] = target;
				return target;
			}
			catch ( System.Exception e )
			{
				Log.Warning( $"[SUI] SuiReferenceResolver failed for GUID '{guid}': {e.Message}" );
				return null;
			}
		};
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
