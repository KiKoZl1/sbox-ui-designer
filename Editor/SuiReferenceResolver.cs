using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
		var cache = new Dictionary<string, SuiReferenceTarget>();
		var registry = SuiAssetRegistryService.Instance;
		var projectRoot = registry.ProjectRoot;

		return guid =>
		{
			if ( string.IsNullOrEmpty( guid ) ) return null;
			if ( cache.TryGetValue( guid, out var cached ) ) return cached;

			SuiReferenceTarget target = null;
			try
			{
				registry.EnsureInitialized();
				var relPath = registry.Registry.Resolve( guid );
				if ( string.IsNullOrEmpty( relPath ) || string.IsNullOrEmpty( projectRoot ) )
				{
					cache[guid] = null;
					return null;
				}

				var full = Path.Combine( projectRoot, relPath );
				if ( !File.Exists( full ) ) { cache[guid] = null; return null; }

				var asset = JsonSerializer.Deserialize<SuiAsset>( File.ReadAllText( full ) );
				var doc = asset?.Document;
				var className = doc?.Output?.ClassName;
				if ( string.IsNullOrEmpty( className ) ) { cache[guid] = null; return null; }

				target = new SuiReferenceTarget
				{
					Namespace = doc.Output?.Namespace,
					ClassName = className,
					AcceptedProps = doc.AcceptedProps,
				};
			}
			catch ( System.Exception e )
			{
				Log.Warning( $"[SUI] SuiReferenceResolver failed for GUID '{guid}': {e.Message}" );
			}

			cache[guid] = target;
			return target;
		};
	}
}
