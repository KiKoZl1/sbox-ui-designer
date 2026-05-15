using System.IO;
using Sandbox;
using SboxUiDesigner.EditorUi.Widgets;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Writes the C# stub for a user-authored <c>[SuiConverter]</c> method into
/// <c>Assets/GameConverters.cs</c> (PRD 18 § 5.6 — Code path). The file is
/// created on the first call with a template scaffold; subsequent calls insert
/// each new method between the BEGIN / END markers so existing user code is
/// never overwritten.
///
/// Editor-only — uses <c>System.IO</c>; depends on <c>Sandbox.Project</c> for
/// project-root resolution.
/// </summary>
public static class SuiUserConverterScaffolder
{
	// .cs files in Assets/ are NOT compiled by the s&box csproj (Assets is for
	// runtime resources — models, sounds, sui docs). User converters need to be
	// in Code/ to actually become part of the assembly the engine loads.
	private const string TargetRelativePath = "Code/GameConverters.cs";

	// Legacy path from before the Assets→Code fix. We auto-migrate any file
	// found here into the correct path on the next scaffold so the user never
	// has to touch the filesystem.
	private const string LegacyRelativePath = "Assets/GameConverters.cs";

	private const string BeginMarker = "// SUI:USER-CONVERTERS:BEGIN";
	private const string EndMarker   = "// SUI:USER-CONVERTERS:END";

	/// <summary>
	/// Append a new method stub for <paramref name="config"/> to the user
	/// converters file. Returns the absolute file path written, or null on
	/// failure (project root unresolved, IO exception).
	/// </summary>
	public static string Scaffold( SuiCustomConverterDialog.Config config )
	{
		if ( config == null ) return null;

		var projectRoot = Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) )
		{
			Log.Warning( "[SUI] Custom converter scaffolder: project root unresolved." );
			return null;
		}

		var fullPath = Path.Combine(
			projectRoot,
			TargetRelativePath.Replace( '/', Path.DirectorySeparatorChar ) );
		var legacyPath = Path.Combine(
			projectRoot,
			LegacyRelativePath.Replace( '/', Path.DirectorySeparatorChar ) );

		try
		{
			Directory.CreateDirectory( Path.GetDirectoryName( fullPath ) );

			// Auto-migrate the legacy Assets/ location → Code/. Pre-fix scaffolds
			// landed in Assets/GameConverters.cs which never compiled. Move the
			// file (preserving any user edits) so the next scaffold appends to
			// the working location without the user having to touch the FS.
			if ( File.Exists( legacyPath ) )
			{
				if ( !File.Exists( fullPath ) )
				{
					File.Move( legacyPath, fullPath );
					Log.Info( $"[SUI] Migrated legacy {LegacyRelativePath} → {TargetRelativePath} (Assets/ isn't compiled by s&box)." );
				}
				else
				{
					// Both exist — keep the new one, warn so the user can review.
					Log.Warning(
						$"[SUI] Found both {LegacyRelativePath} (legacy) and {TargetRelativePath}. " +
						"Using the new location; the legacy file is orphaned and safe to delete." );
				}
			}

			if ( !File.Exists( fullPath ) )
				File.WriteAllText( fullPath, BuildTemplate() );

			var existing = File.ReadAllText( fullPath );
			var method = BuildMethod( config );

			var beginIdx = existing.IndexOf( BeginMarker );
			var endIdx   = existing.IndexOf( EndMarker );

			string updated;
			if ( beginIdx < 0 || endIdx < 0 || endIdx < beginIdx )
			{
				// Markers absent — best-effort append at the end of the file.
				// (User probably hand-edited; we don't try to outsmart them.)
				updated = existing.TrimEnd() + "\n\n" + method + "\n";
			}
			else
			{
				// Insert immediately before the END marker, preserving anything
				// the user has already written inside the markers.
				var before = existing.Substring( 0, endIdx ).TrimEnd();
				var after  = existing.Substring( endIdx );
				updated = before + "\n\n" + method + "\n\n\t" + after;
			}

			File.WriteAllText( fullPath, updated );
			return fullPath;
		}
		catch ( System.Exception e )
		{
			Log.Warning( $"[SUI] Custom converter scaffolder failed: {e.Message}" );
			return null;
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Template + method rendering
	// ─────────────────────────────────────────────────────────────────────

	private static string BuildTemplate()
	{
		return $@"// SUI:USER-OWNED:BEGIN ==========================================================
// User-defined SUI converters. Methods marked [SuiConverter] become available
// in the bind popup's converter chain after the next compile.
//
// This file is created once by the Sbox UI Designer and only EXTENDED on each
// subsequent scaffold — code inside the BEGIN/END markers below is never
// overwritten, so it's safe to edit by hand.
// SUI:USER-OWNED:END ============================================================

using Sandbox;
using SboxUiDesigner.Runtime;

namespace Game;

public static class GameConverters
{{
	{BeginMarker}

	{EndMarker}
}}
";
	}

	private static string BuildMethod( SuiCustomConverterDialog.Config config )
	{
		var ret = MapTypeRef( config.ReturnType );
		var parameters = new System.Text.StringBuilder();
		for ( int i = 0; i < config.Inputs.Count; i++ )
		{
			if ( i > 0 ) parameters.Append( ", " );
			parameters.Append( MapTypeRef( config.Inputs[i].type ) );
			parameters.Append( ' ' );
			parameters.Append( config.Inputs[i].name );
		}

		var displayName = EscapeStringLiteral( config.Name );
		var category    = EscapeStringLiteral( string.IsNullOrEmpty( config.Category ) ? "User" : config.Category );
		var description = EscapeStringLiteral( config.Description ?? "" );

		return
			$"\t[SuiConverter( \"{displayName}\", Category = \"{category}\", Description = \"{description}\" )]\n" +
			$"\tpublic static {ret} {config.Name}( {parameters} )\n" +
			$"\t{{\n" +
			$"\t\t// TODO: implement\n" +
			$"\t\treturn default;\n" +
			$"\t}}";
	}

	/// <summary>Map a SUI TypeRef ("int"/"Color"/…) to a C#-source-readable type name.</summary>
	private static string MapTypeRef( string typeRef ) => typeRef switch
	{
		"int"       => "int",
		"long"      => "long",
		"float"     => "float",
		"double"    => "double",
		"bool"      => "bool",
		"string"    => "string",
		"Color"     => "Color",
		"Vector2"   => "Vector2",
		"Vector3"   => "Vector3",
		"Vector4"   => "Vector4",
		"Angles"    => "Angles",
		"Rotation"  => "Rotation",
		"Transform" => "Transform",
		"Texture"   => "Texture",
		"Resource"  => "Resource",
		"Sound"     => "SoundEvent",
		"Material"  => "Material",
		_           => "object",
	};

	private static string EscapeStringLiteral( string s )
		=> string.IsNullOrEmpty( s ) ? "" : s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
}
