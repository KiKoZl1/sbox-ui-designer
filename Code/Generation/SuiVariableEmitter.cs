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
/// V1.5-M2-K — every Variable is a normal [Property]. The legacy
/// AcceptedProp concept was removed entirely (DEVIATIONS D-005); IsPublic
/// flag on Variable replaces it. FromComponent / FromActionGraph remain
/// TODO for M3.
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

			var srcKind = v.Source?.Kind ?? SuiVariableSourceKind.Manual;
			if ( srcKind != SuiVariableSourceKind.Manual )
			{
				sb.Append( "\t// TODO M3 — Variable '" )
					.Append( v.Name )
					.Append( "' has source '" )
					.Append( srcKind )
					.AppendLine( "', not yet emitted." );
				continue;
			}

			var csType = SuiTypeMapper.ToCSharp( v.Type );
			var def = SuiTypeMapper.DefaultLiteral( v.Type, v.Default );

			// V1.5-M2-K7 — Variables are emitted as plain public properties (no
			// [Property] attribute). The generated class inherits from Panel,
			// not Component — [Property] is a Component-only attribute. The
			// user-facing inspector lives on the wrapper class instead, which
			// stays a Component-friendly type and DOES use [Property].
			sb.Append( "\tpublic " ).Append( csType ).Append( ' ' ).Append( v.Name )
				.Append( " { get; set; } = " ).Append( def ).AppendLine( ";" );
		}
	}
}
