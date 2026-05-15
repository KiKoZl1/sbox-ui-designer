using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Per-element binding-attribute emitter. Each binding contributes (a) a
/// <c>data-sui-{property}="@(...)"</c> attribute for debug / DOM scanning and
/// (b) — when the property maps to a CSS declaration via
/// <see cref="SuiBindingCssMapper"/> — an entry in the element's inline
/// <c>style=""</c> attribute so the visual updates automatically.
///
/// The Text element gets a special case: when its <c>Text</c> property is
/// bound, the literal body is replaced with the <c>@(expression)</c> so the
/// label content updates live (this is the most common single-property bind).
///
/// Composite cases (ProgressBar Value/Min/Max as a fill-percent, grid track
/// counts, &amp;c.) are handled by <see cref="SuiRazorGenerator"/> directly —
/// they need wrapper elements or multi-CSS-property output.
/// </summary>
public static class SuiBindingEmitter
{
	/// <summary>
	/// Convenience that combines <see cref="EmitElementDataAttrs"/> and
	/// <see cref="EmitElementStyleBody"/> into a single attribute string —
	/// matches the existing call site for Text elements.
	/// </summary>
	public static string EmitElementAttributes( SuiElement el, SuiDocument doc )
	{
		var data = EmitElementDataAttrs( el, doc );
		var style = EmitElementStyleBody( el, doc );
		if ( string.IsNullOrEmpty( style ) ) return data;
		return data + " style=\"" + style + "\"";
	}

	/// <summary>
	/// <c>data-sui-{property}="@(...)"</c> attributes for every non-body binding
	/// on the element. Useful for DOM-scanning debug and visible-in-source bind
	/// documentation; the actual visual update flows through the inline style
	/// emitted by <see cref="EmitElementStyleBody"/>.
	/// </summary>
	public static string EmitElementDataAttrs( SuiElement el, SuiDocument doc )
	{
		if ( el?.Bindings == null || el.Bindings.Count == 0 ) return "";

		var sb = new StringBuilder();
		foreach ( var b in el.Bindings )
		{
			if ( b == null || string.IsNullOrEmpty( b.Property ) ) continue;
			// Text-body bindings live in the label's inner content; skip here.
			if ( el.Type == SuiElementType.Text && b.Property == "Text" ) continue;

			var expr = SuiBindingExpressionEmitter.Emit( b, doc );
			sb.Append( ' ' ).Append( "data-sui-" )
				.Append( b.Property.ToLowerInvariant() )
				.Append( "=\"@(" ).Append( expr ).Append( ")\"" );
		}
		return sb.ToString();
	}

	/// <summary>
	/// Concatenated inline-CSS body — every binding with a direct CSS mapping
	/// (per <see cref="SuiBindingCssMapper"/>) contributes a declaration. The
	/// returned string is the contents of <c>style="..."</c>, without surrounding
	/// quotes, so the generator can prepend container-positioning rules for
	/// special cases (ProgressBar fill, &amp;c.) before wrapping.
	/// </summary>
	public static string EmitElementStyleBody( SuiElement el, SuiDocument doc )
	{
		if ( el?.Bindings == null || el.Bindings.Count == 0 ) return "";

		var sb = new StringBuilder();
		foreach ( var b in el.Bindings )
		{
			if ( b == null || string.IsNullOrEmpty( b.Property ) ) continue;

			// ProgressBar's Value/Min/Max/FillColor are owned by the inner
			// progress-fill child the generator emits — don't paint the outer
			// track div with FillColor as background, etc.
			if ( el.Type == SuiElementType.ProgressBar
				&& ( b.Property == "Value" || b.Property == "Min" || b.Property == "Max" || b.Property == "FillColor" ) )
				continue;

			var expr = SuiBindingExpressionEmitter.Emit( b, doc );
			var targetType = SuiBindingModeMatrix.GetTargetType( el.Type, b.Property );
			var css = SuiBindingCssMapper.ToCssDeclaration( b.Property, expr, targetType );
			if ( string.IsNullOrEmpty( css ) ) continue;

			if ( sb.Length > 0 ) sb.Append( ' ' );
			sb.Append( css );
		}
		return sb.ToString();
	}

	/// <summary>
	/// True when the element has any binding that the progress-fill child
	/// emit needs to render (Value / Min / Max / FillColor). The generator uses
	/// this to decide whether to inject the inner <c>.sui-progress-fill</c> div
	/// and prepend <c>position: relative; overflow: hidden;</c> to the outer.
	/// </summary>
	public static bool HasProgressFillBindings( SuiElement el )
	{
		if ( el?.Type != SuiElementType.ProgressBar || el.Bindings == null ) return false;
		foreach ( var b in el.Bindings )
		{
			if ( b == null ) continue;
			if ( b.Property == "Value" || b.Property == "Min" || b.Property == "Max" || b.Property == "FillColor" )
				return true;
		}
		return false;
	}

	/// <summary>
	/// When a Text element has its <c>Text</c> property bound, return the
	/// <c>@(expression)</c> to use as the label body. Returns null otherwise —
	/// the caller falls back to the literal text.
	/// </summary>
	public static string TryGetTextBodyExpression( SuiElement el, SuiDocument doc )
	{
		if ( el?.Type != SuiElementType.Text || el.Bindings == null ) return null;
		foreach ( var b in el.Bindings )
		{
			if ( b == null ) continue;
			if ( b.Property != "Text" ) continue;
			var expr = SuiBindingExpressionEmitter.Emit( b, doc );
			return $"@({expr})";
		}
		return null;
	}
}
