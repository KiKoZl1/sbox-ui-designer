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
	public static void EmitProperties( IList<SuiVariable> vars, StringBuilder sb, IList<SuiAcceptedProp> acceptedProps = null )
	{
		if ( vars == null || sb == null ) return;

		foreach ( var v in vars )
		{
			if ( v == null || string.IsNullOrEmpty( v.Name ) ) continue;

			var srcKind = v.Source?.Kind ?? SuiVariableSourceKind.Manual;

			// V1.5-M2 — FromAcceptedProp emits a thin alias property pointing
			// at the AcceptedProp's [Property] (PRD 19 § 3.2). The alias is
			// hidden in the inspector — only the AcceptedProp itself surfaces.
			if ( srcKind == SuiVariableSourceKind.FromAcceptedProp )
			{
				EmitAcceptedPropAlias( v, sb, acceptedProps );
				continue;
			}

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

			sb.Append( "\t[Property" );
			if ( !string.IsNullOrEmpty( v.Group ) )
				sb.Append( ", Group( \"" ).Append( v.Group.Replace( "\"", "\\\"" ) ).Append( "\" )" );
			sb.Append( "] public " ).Append( csType ).Append( ' ' ).Append( v.Name )
				.Append( " { get; set; } = " ).Append( def ).AppendLine( ";" );
		}
	}

	private static void EmitAcceptedPropAlias( SuiVariable v, StringBuilder sb, IList<SuiAcceptedProp> acceptedProps )
	{
		var propId = v.Source?.PropId;
		var targetName = ResolveAcceptedPropName( propId, acceptedProps );

		if ( string.IsNullOrEmpty( targetName ) )
		{
			sb.Append( "\t// WARN — FromAcceptedProp Variable '" ).Append( v.Name )
				.Append( "' has no resolvable PropId='" ).Append( propId )
				.AppendLine( "'; emitting Manual fallback so codegen still compiles." );
			var csType = SuiTypeMapper.ToCSharp( v.Type );
			var def = SuiTypeMapper.DefaultLiteral( v.Type, v.Default );
			sb.Append( "\t[Property, Hide] public " ).Append( csType ).Append( ' ' )
				.Append( v.Name ).Append( " { get; set; } = " ).Append( def ).AppendLine( ";" );
			return;
		}

		// Skip the alias if the Variable and the target AcceptedProp share the
		// same identifier — would emit `Hp => Hp;` which is infinite recursion.
		// Bindings reference the AcceptedProp directly in that case.
		if ( string.Equals( v.Name, targetName, System.StringComparison.Ordinal ) )
		{
			sb.Append( "\t// alias '" ).Append( v.Name )
				.AppendLine( "' = AcceptedProp of same name (no alias property needed)" );
			return;
		}

		var csTypeAlias = SuiTypeMapper.ToCSharp( v.Type );
		sb.Append( "\t[Hide] public " ).Append( csTypeAlias ).Append( ' ' )
			.Append( v.Name ).Append( " => " ).Append( targetName ).AppendLine( ";" );
	}

	private static string ResolveAcceptedPropName( string propId, IList<SuiAcceptedProp> acceptedProps )
	{
		if ( string.IsNullOrEmpty( propId ) || acceptedProps == null ) return null;
		foreach ( var p in acceptedProps )
		{
			if ( p?.PropId == propId ) return p.Name;
		}
		return null;
	}
}
