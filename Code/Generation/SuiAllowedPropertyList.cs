using System.Collections.Generic;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Whitelist of CSS properties the generator is allowed to emit. Anything
/// outside this list is a hard error — the SCSS emitter REFUSES to produce
/// it. This is the engine-correctness boundary documented in PRD doc 08
/// and `references/ui-anti-patterns.md` (Confusion 2).
///
/// The list mirrors what the s&box runtime UI Yoga + style implementation
/// actually parses. Web CSS properties not in this list (display: grid,
/// position: fixed, float, etc.) are silently ignored at runtime AND
/// produce silent design bugs — so we forbid them at generate time.
/// </summary>
public static class SuiAllowedPropertyList
{
	private static readonly HashSet<string> _allowed = new( System.StringComparer.OrdinalIgnoreCase )
	{
		// Display & layout
		"display",
		"flex-direction", "flex-wrap", "flex-grow", "flex-shrink", "flex-basis", "flex",
		"gap", "row-gap", "column-gap",
		"justify-content", "align-items", "align-self",
		"position",
		"left", "top", "right", "bottom",
		"width", "height", "min-width", "min-height", "max-width", "max-height",
		"margin", "margin-left", "margin-top", "margin-right", "margin-bottom",
		"padding", "padding-left", "padding-top", "padding-right", "padding-bottom",
		"overflow", "z-index",

		// Visual
		"background-color",
		"background-image", "background-size", "background-position", "background-repeat",
		"background-image-tint",
		"border", "border-color", "border-width", "border-radius",
		"box-shadow", "text-shadow",
		"opacity",

		// Text
		"color",
		"font-family", "font-size", "font-weight", "font-style",
		"text-align", "text-transform", "text-decoration",
		"letter-spacing", "line-height", "white-space",
		"text-overflow",

		// Motion
		"transform",
		"transition", "transition-duration", "transition-property", "transition-timing-function", "transition-delay",
		"animation", "animation-name", "animation-duration", "animation-iteration-count", "animation-timing-function",

		// s&box-specific
		"pointer-events",
		"sound-in", "sound-out",
	};

	/// <summary>Hard-blocked properties that web tutorials suggest but the engine doesn't support.</summary>
	private static readonly HashSet<string> _forbidden = new( System.StringComparer.OrdinalIgnoreCase )
	{
		"grid-template-columns", "grid-template-rows", "grid-template-areas", "grid-area",
		"grid-row", "grid-column", "grid-gap", "grid-row-gap", "grid-column-gap",
		"float", "clear",
	};

	/// <summary>Hard-blocked VALUE strings — properties that exist but with disallowed values.</summary>
	private static readonly HashSet<string> _forbiddenDisplayValues = new( System.StringComparer.OrdinalIgnoreCase )
	{
		"grid", "block", "inline", "inline-block", "inline-flex", "table", "table-cell", "contents",
	};

	private static readonly HashSet<string> _forbiddenPositionValues = new( System.StringComparer.OrdinalIgnoreCase )
	{
		"fixed", "sticky",
	};

	public static bool IsAllowed( string property ) => _allowed.Contains( property );

	public static bool IsForbidden( string property ) => _forbidden.Contains( property );

	/// <summary>
	/// Validate a property/value pair. Returns null if accepted, an error
	/// string if rejected (will block the generation with a useful message).
	/// </summary>
	public static string Validate( string property, string value )
	{
		if ( string.IsNullOrEmpty( property ) ) return "empty property name";
		if ( _forbidden.Contains( property ) )
			return $"'{property}' is on the forbidden list (engine doesn't support it)";
		if ( !_allowed.Contains( property ) )
			return $"'{property}' is not on the allowed-property list";

		if ( property.Equals( "display", System.StringComparison.OrdinalIgnoreCase ) )
		{
			if ( !string.IsNullOrEmpty( value ) && _forbiddenDisplayValues.Contains( value.Trim() ) )
				return $"display: {value} is not supported (only flex, none)";
		}
		else if ( property.Equals( "position", System.StringComparison.OrdinalIgnoreCase ) )
		{
			if ( !string.IsNullOrEmpty( value ) && _forbiddenPositionValues.Contains( value.Trim() ) )
				return $"position: {value} is not supported (only static, relative, absolute)";
		}

		return null;
	}
}
