using System.Collections.Generic;
using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Emits a <c>BuildHash()</c> override that combines every Variable referenced
/// (directly or via converter args) by any <c>OneWay</c> / <c>TwoWay</c>
/// binding in the document (PRD 18 § 6.2). The Razor runtime re-renders
/// whenever the hash changes — so any property the UI displays via a binding
/// belongs in the hash.
///
/// <c>OneTime</c> bindings are excluded by design (the value is read once and
/// never refreshed). Variables not referenced by any binding are also excluded
/// — they don't affect the rendered tree, so they don't need to trigger renders.
/// </summary>
public static class SuiBuildHashEmitter
{
	/// <summary>
	/// Append a <c>protected override int BuildHash() => HashCode.Combine(...);</c>
	/// line. No-op when the document has no reactive bindings — the engine's
	/// default behaviour is fine.
	/// </summary>
	public static void EmitBuildHash( IList<SuiVariable> vars, IList<SuiElement> elements, StringBuilder sb )
		=> EmitBuildHash( vars, elements, null, sb );

	/// <summary>
	/// Overload that also folds expressions from embedded child references into
	/// the hash so that mutations like <c>parent.MyHud.Health -= 10</c> trigger
	/// a parent re-render and the child markup picks up the new value
	/// (V1.5-M2-K7-bugfix). Each entry should be a Razor-evaluable expression
	/// like <c>MyHud?.Health</c>.
	/// </summary>
	public static void EmitBuildHash( IList<SuiVariable> vars, IList<SuiElement> elements, IList<string> childExpressions, StringBuilder sb )
	{
		if ( sb == null ) return;

		var reactive = new List<string>();
		var seen = new HashSet<string>();
		var varsById = new Dictionary<string, SuiVariable>();
		if ( vars != null )
		{
			foreach ( var v in vars )
				if ( v != null && !string.IsNullOrEmpty( v.Id ) ) varsById[v.Id] = v;
		}

		if ( elements != null )
		{
			foreach ( var el in elements )
			{
				if ( el?.Bindings == null ) continue;
				foreach ( var b in el.Bindings )
				{
					if ( b == null ) continue;
					if ( b.Mode == SuiBindingMode.OneTime ) continue;

					AddIfReactive( b.Source?.VariableId, varsById, seen, reactive );

					if ( b.Converters == null ) continue;
					foreach ( var step in b.Converters )
					{
						if ( step?.Args == null ) continue;
						foreach ( var arg in step.Args )
						{
							if ( arg?.Kind != SuiConverterArgKind.Variable ) continue;
							AddIfReactive( arg.VariableId, varsById, seen, reactive );
						}
					}
				}
			}
		}

		if ( childExpressions != null )
		{
			foreach ( var expr in childExpressions )
			{
				if ( string.IsNullOrEmpty( expr ) ) continue;
				if ( seen.Add( expr ) ) reactive.Add( expr );
			}
		}

		if ( reactive.Count == 0 ) return;

		// HashCode.Combine peaks at 8 args — emit accumulator form so we don't
		// need to chunk by arity, and the body stays uniform regardless of
		// reactive count.
		sb.AppendLine( "\tprotected override int BuildHash()" );
		sb.AppendLine( "\t{" );
		sb.AppendLine( "\t\tvar h = new global::System.HashCode();" );
		foreach ( var r in reactive )
			sb.Append( "\t\th.Add( " ).Append( r ).AppendLine( " );" );
		sb.AppendLine( "\t\treturn h.ToHashCode();" );
		sb.AppendLine( "\t}" );
	}

	private static void AddIfReactive( string varId, Dictionary<string, SuiVariable> varsById, HashSet<string> seen, List<string> reactive )
	{
		if ( string.IsNullOrEmpty( varId ) ) return;
		if ( !varsById.TryGetValue( varId, out var v ) || v == null || string.IsNullOrEmpty( v.Name ) ) return;
		if ( seen.Add( v.Name ) ) reactive.Add( v.Name );
	}
}
