using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Per-element binding-attribute emitter. For every binding on the element it
/// appends a <c>data-sui-{property}="@(expression)"</c> attribute to the open
/// tag — small, syntactically-valid Razor that's visible in the generated
/// source (so users can see their bindings live in the markup) and survives
/// without forcing a generator-wide swap to Sandbox.UI control tags
/// (deferred to M4 alongside the input-widget tag spike).
///
/// The Text element gets a special case: when its <c>Text</c> property is
/// bound, the literal body is replaced with the <c>@(expression)</c> so the
/// label content updates live (this is the most common single-property bind).
/// </summary>
public static class SuiBindingEmitter
{
	/// <summary>
	/// Build the attribute string to inject after the element's <c>class</c>
	/// attribute. Empty when the element has no bindings (or only Text-body
	/// bindings, which are handled by <see cref="TryGetTextBodyExpression"/>).
	/// </summary>
	public static string EmitElementAttributes( SuiElement el, SuiDocument doc )
	{
		if ( el?.Bindings == null || el.Bindings.Count == 0 ) return "";

		var sb = new StringBuilder();
		foreach ( var b in el.Bindings )
		{
			if ( b == null || string.IsNullOrEmpty( b.Property ) ) continue;
			// Text-body bindings are emitted into the label's inner content;
			// skip them here so we don't also duplicate them as data attrs.
			if ( el.Type == SuiElementType.Text && b.Property == "Text" ) continue;

			var expr = SuiBindingExpressionEmitter.Emit( b, doc );
			sb.Append( ' ' ).Append( "data-sui-" ).Append( b.Property.ToLowerInvariant() )
				.Append( "=\"@(" ).Append( expr ).Append( ")\"" );
		}
		return sb.ToString();
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
