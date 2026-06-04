using System.Collections.Generic;
using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Emits the <c>[Property]</c> field declarations for a document's Variables
/// into the generated <c>@code</c> block. Gameplay code assigns directly to
/// these fields, and the bound element attributes read them via Razor
/// expressions.
///
/// V1.5-M4 close — every Variable is now Manual-source only. The legacy
/// AcceptedProp concept was removed in M2-K (DEVIATIONS D-005). The
/// FromComponent / FromActionGraph kinds were ripped at M4 close
/// (DEVIATION D-017) — only Manual ships in V1.5.
/// </summary>
public static class SuiVariableEmitter
{
	/// <summary>Append <c>public T Name { get; set; } = default;</c> per Variable.</summary>
	public static void EmitProperties( IList<SuiVariable> vars, StringBuilder sb )
	{
		if ( vars == null || sb == null ) return;

		foreach ( var v in vars )
		{
			if ( v == null || string.IsNullOrEmpty( v.Name ) ) continue;

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
