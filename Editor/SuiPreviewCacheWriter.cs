using System;
using System.Collections.Generic;
using System.IO;
using Sandbox;
using SboxUiDesigner.Generation;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Writes generated Razor + SCSS for a .sui document into the preview cache
/// inside the consumer project's compilable code root, so the editor's hotload
/// pipeline picks them up and turns them into a real PanelComponent type.
///
/// Cache layout (per M1 inspection report):
///   &lt;project root&gt;/Code/_sui_preview/&lt;ClassName&gt;/&lt;ClassName&gt;.razor
///   &lt;project root&gt;/Code/_sui_preview/&lt;ClassName&gt;/&lt;ClassName&gt;.razor.scss
///
/// We write atomically (write-to-temp + replace) and skip the write entirely if
/// the file's sha256 matches what's already on disk — avoids a hotload churn for
/// noop regens (e.g. selection-only events that still fire DocumentChanged).
/// </summary>
public static class SuiPreviewCacheWriter
{
	public sealed class WriteResult
	{
		public bool Ok { get; set; }
		public string TypeFullName { get; set; }   // <namespace>.<ClassName>
		public string CacheFolder { get; set; }    // absolute on-disk folder
		public string RazorPath { get; set; }      // absolute
		public string ScssPath { get; set; }       // absolute (may be null if no SCSS)
		public bool RazorChanged { get; set; }     // true iff content differed and we wrote
		public bool ScssChanged { get; set; }
		public List<string> Errors { get; } = new();
		public List<string> Warnings { get; } = new();
	}

	/// <summary>
	/// Generate + write to the preview cache for the given document. Returns
	/// metadata including the fully-qualified type name the editor's compiler
	/// will produce after hotload — caller polls TypeLibrary for it.
	/// </summary>
	public static WriteResult Write( SuiDocument document )
	{
		var result = new WriteResult();
		if ( document == null )
		{
			result.Errors.Add( "document is null" );
			return result;
		}

		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) )
		{
			result.Errors.Add( "Project.Current.RootDirectory is null — cannot resolve project root" );
			return result;
		}

		// Drive the generator in Preview mode. ClassName/Namespace defaults come
		// from the document's own Output settings; falls back to sanitized Name.
		var ctx = new SuiGenerationContext
		{
			Document = document,
			Mode = SuiGenerationMode.Preview,
			OutputFolder = "", // intentionally blank — pipeline returns plain filenames
		};

		var gen = SuiGenerationPipeline.Run( ctx );
		foreach ( var w in gen.Warnings ) result.Warnings.Add( w );
		foreach ( var e in gen.Errors ) result.Errors.Add( e );
		if ( !gen.Ok )
			return result;

		var className = ctx.ClassName;
		var ns = ctx.Namespace;
		if ( string.IsNullOrEmpty( className ) )
		{
			result.Errors.Add( "generator did not resolve a ClassName" );
			return result;
		}

		var cacheFolder = Path.Combine( projectRoot, "Code", "_sui_preview", className );
		try
		{
			Directory.CreateDirectory( cacheFolder );
		}
		catch ( Exception ex )
		{
			result.Errors.Add( $"failed to create cache folder '{cacheFolder}': {ex.Message}" );
			return result;
		}

		// Razor
		var razor = gen.FindByKind( SuiGeneratedFileKind.Razor );
		if ( razor != null )
		{
			var path = Path.Combine( cacheFolder, $"{className}.razor" );
			result.RazorPath = path;
			try
			{
				result.RazorChanged = WriteIfChanged( path, razor.Content );
			}
			catch ( Exception ex )
			{
				result.Errors.Add( $"razor write failed: {ex.Message}" );
				return result;
			}
		}

		// SCSS
		var scss = gen.FindByKind( SuiGeneratedFileKind.Scss );
		if ( scss != null )
		{
			var path = Path.Combine( cacheFolder, $"{className}.razor.scss" );
			result.ScssPath = path;
			try
			{
				result.ScssChanged = WriteIfChanged( path, scss.Content );
			}
			catch ( Exception ex )
			{
				result.Errors.Add( $"scss write failed: {ex.Message}" );
				return result;
			}
		}

		result.CacheFolder = cacheFolder;
		result.TypeFullName = string.IsNullOrEmpty( ns ) ? className : $"{ns}.{className}";
		result.Ok = true;
		return result;
	}

	/// <summary>
	/// Compares the hash of the new content to whatever is already on disk; only
	/// writes (atomically via .tmp + Replace) when the content actually changed.
	/// Returns true iff a write happened.
	/// </summary>
	private static bool WriteIfChanged( string path, string content )
	{
		var newHash = SuiHashUtility.Sha256( content ?? "" );
		if ( File.Exists( path ) )
		{
			try
			{
				var existing = File.ReadAllText( path );
				if ( SuiHashUtility.Sha256( existing ) == newHash )
					return false; // no-op
			}
			catch
			{
				// If we can't read, fall through and overwrite.
			}
		}

		var tmp = path + ".tmp";
		File.WriteAllText( tmp, content ?? "" );
		try
		{
			if ( File.Exists( path ) )
				File.Replace( tmp, path, null );
			else
				File.Move( tmp, path );
		}
		catch ( Exception )
		{
			// Replace can fail across volumes / on permission quirks. Fall back
			// to plain overwrite so we don't leave the .tmp dangling.
			if ( File.Exists( tmp ) )
			{
				File.Copy( tmp, path, overwrite: true );
				try { File.Delete( tmp ); } catch { }
			}
		}
		return true;
	}
}
