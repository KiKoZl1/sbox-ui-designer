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
				// throws on every load.
				var asset = AssetSystem.FindByPath( relPath );
				if ( asset == null )
				{
					Log.Warning( $"[SUI] SuiReferenceResolver: AssetSystem couldn't find '{relPath}' for GUID '{guid}'." );
					return null;
				}

				var loaded = asset.LoadResource<SuiAsset>();
				var doc = loaded?.Document;
				var className = doc?.Output?.ClassName;
				if ( string.IsNullOrEmpty( className ) )
				{
					Log.Warning( $"[SUI] SuiReferenceResolver: '{relPath}' has no Output.ClassName — codegen embed will be skipped." );
					return null;
				}

				var target = new SuiReferenceTarget
				{
					Namespace = doc.Output?.Namespace,
					ClassName = className,
					AcceptedProps = doc.AcceptedProps,
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
}
