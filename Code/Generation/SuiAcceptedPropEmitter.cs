using System.Collections.Generic;
using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Emits a <c>[Property]</c> declaration per <see cref="SuiAcceptedProp"/>
/// on the generated PanelComponent (PRD 19 § 7.3). AcceptedProps are the
/// external contract: parent <c>SuiReference.Props</c> values flow into these
/// properties as Razor attribute=value when the parent is generated.
///
/// <para>Bindings inside the child <c>.sui</c> reference a matching
/// <see cref="SuiVariable"/> with <c>Source.Kind = FromAcceptedProp</c>; the
/// alias is emitted by <see cref="SuiVariableEmitter"/> as a thin
/// <c>get =&gt; PropName;</c> over this property.</para>
/// </summary>
public static class SuiAcceptedPropEmitter
{
	public static void EmitProperties( IList<SuiAcceptedProp> props, StringBuilder sb )
	{
		if ( props == null || sb == null ) return;

		foreach ( var p in props )
		{
			if ( p == null || string.IsNullOrEmpty( p.Name ) ) continue;

			var csType = SuiTypeMapper.ToCSharp( p.Type );
			var def = SuiTypeMapper.DefaultLiteral( p.Type, p.Default );
			var group = string.IsNullOrEmpty( p.Group ) ? "Slot" : p.Group;

			sb.Append( "\t[Property, Group( \"" )
				.Append( group.Replace( "\"", "\\\"" ) )
				.Append( "\" )]" );
			sb.Append( " public " ).Append( csType ).Append( ' ' ).Append( p.Name )
				.Append( " { get; set; } = " ).Append( def ).AppendLine( ";" );
		}
	}
}
