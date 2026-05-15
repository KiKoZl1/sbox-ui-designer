using System.Collections.Generic;
using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Emits the <c>[Property]</c> field declarations for a document's Variables
/// into the generated <c>@code</c> block (PRD 18 § 3.8). Gameplay code assigns
/// directly to these fields, and the bound element attributes read them via
/// Razor expressions.
///
/// M1-D scope: <see cref="SuiVariableSourceKind.Manual"/> only. FromComponent
/// and FromActionGraph sources need additional infrastructure (auto-pulled
/// derived properties; <c>Func&lt;T&gt;</c> slots) — they land with the
/// broader codegen integration in M3.
/// </summary>
public static class SuiVariableEmitter
{
	/// <summary>Append <c>[Property] T Name { get; set; } = default;</c> per Manual Variable.</summary>
	public static void EmitProperties( IList<SuiVariable> vars, StringBuilder sb )
	{
		if ( vars == null || sb == null ) return;

		foreach ( var v in vars )
		{
			if ( v == null || string.IsNullOrEmpty( v.Name ) ) continue;

			if ( v.Source?.Kind != SuiVariableSourceKind.Manual )
			{
				sb.Append( "\t// TODO M3 — Variable '" )
					.Append( v.Name )
					.Append( "' has source '" )
					.Append( v.Source?.Kind )
					.AppendLine( "', not yet emitted." );
				continue;
			}

			var csType = SuiTypeMapper.ToCSharp( v.Type );
			var def = SuiTypeMapper.DefaultLiteral( v.Type, v.Default );

			sb.Append( "\t[Property" );
			if ( !string.IsNullOrEmpty( v.Group ) )
				sb.Append( ", Group( \"" ).Append( v.Group.Replace( "\"", "\\\"" ) ).Append( "\" )" );
			sb.Append( "] public " ).Append( csType ).Append( ' ' ).Append( v.Name )
				.Append( " { get; set; } = " ).Append( def ).AppendLine( ";" );
		}
	}
}
