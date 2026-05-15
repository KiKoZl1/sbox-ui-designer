namespace SboxUiDesigner.Generation;

/// <summary>
/// Maps a single binding (target property + expected type + bound expression)
/// to an inline-CSS declaration the generator drops into the element's
/// <c>style=""</c> attribute. The mapper is the link that turns "designer-bind"
/// into "actual visual feedback at runtime" — without it, bindings just put
/// data-attributes onto the DOM and the user has to write SCSS / partial.cs
/// to consume them.
///
/// Coverage: every bindable property in <c>SuiBindingModeMatrix</c> that has a
/// natural CSS equivalent — colours, sizes, opacities, fonts, visibility,
/// pointer state, image URLs. The composite cases (ProgressBar Value/Min/Max,
/// Grid Columns/Rows) are handled directly in <c>SuiRazorGenerator</c> since
/// they need a wrapper element or multi-property emission.
/// </summary>
public static class SuiBindingCssMapper
{
	/// <summary>
	/// Render one CSS declaration for a binding, e.g. <c>"font-size: @(FontSize)px;"</c>.
	/// Returns null when the property has no direct inline-CSS mapping (the caller
	/// then either skips it or treats it as a composite case).
	/// </summary>
	public static string ToCssDeclaration( string property, string expression, string targetType )
	{
		if ( string.IsNullOrEmpty( property ) || string.IsNullOrEmpty( expression ) ) return null;

		switch ( property )
		{
			// ── Colours ── (Color → ".Hex" so the CSS gets "#RRGGBBAA")
			case "Color":            return $"color: @(({expression}).Hex);";
			case "Tint":             return $"background-color: @(({expression}).Hex);";
			case "BackgroundColor":  return $"background-color: @(({expression}).Hex);";
			case "BorderColor":      return $"border-color: @(({expression}).Hex);";
			case "FillColor":        return $"background-color: @(({expression}).Hex);"; // for fill-style elements

			// ── Pixel sizes ──
			case "Width":            return $"width: @({expression})px;";
			case "Height":           return $"height: @({expression})px;";
			case "FontSize":         return $"font-size: @({expression})px;";
			case "BorderWidth":      return $"border-width: @({expression})px;";
			case "BorderRadius":     return $"border-radius: @({expression})px;";
			case "LetterSpacing":    return $"letter-spacing: @({expression})px;";

			// ── Unitless numerics ──
			case "Opacity":          return $"opacity: @({expression});";
			case "LineHeight":       return $"line-height: @({expression});";

			// ── String-typed CSS properties (font / text) ──
			case "FontFamily":       return $"font-family: @({expression});";
			case "FontWeight":       return $"font-weight: @({expression});";
			case "TextAlign":        return $"text-align: @({expression});";

			// ── Bool conditionals ──
			case "Visibility":       return $"display: @(({expression}) ? \"flex\" : \"none\");";
			case "Enabled":          return $"pointer-events: @(({expression}) ? \"auto\" : \"none\"); opacity: @(({expression}) ? 1f : 0.5f);";

			// ── Image / asset URLs ──
			case "ImagePath":
			case "IconPath":         return $"background-image: url(\"@({expression})\");";

			// Properties the body emit handles, or composites the generator handles directly.
			case "Text":
			case "ButtonText":
			case "Value":
			case "Min":
			case "Max":
			case "Columns":
			case "Rows":
			case "CellWidth":
			case "CellHeight":
			case "Gap":
			case "Direction":
			case "SlotIndex":
			case "Count":
			case "Checked":
			case "SelectedIndex":
			case "Placeholder":
				return null;

			default:                 return null;
		}
	}
}
