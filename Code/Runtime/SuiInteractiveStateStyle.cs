namespace SboxUiDesigner.Runtime;

/// <summary>
/// Per-state visual override for interactive elements (Button, InventorySlot,
/// ItemIcon) — V1.5 M3.5 (PRD 25 § 3). Each interactive element holds up to
/// four nullable instances: HoverStyle, PressedStyle, DisabledStyle, FocusedStyle.
///
/// The element's base Layout/Style props act as the implicit Normal state.
/// When a state override is null, the SCSS pseudo-class is not emitted —
/// the element falls back to Normal visuals on hover/press/etc.
///
/// All fields are nullable strings/floats so the codegen can decide whether
/// to emit each property: empty string or default value → no emit (avoids
/// overwriting Normal-state cascade for properties the user didn't author).
/// </summary>
public sealed class SuiInteractiveStateStyle
{
	/// <summary>Asset path of a background image to swap in for this state. Empty = inherit Normal.</summary>
	public string BackgroundImage { get; set; } = "";

	/// <summary>Hex color string ("#1f2937") or empty = inherit Normal.</summary>
	public string BackgroundColor { get; set; } = "";

	/// <summary>Hex color string for the border in this state. Empty = inherit.</summary>
	public string BorderColor { get; set; } = "";

	/// <summary>
	/// Border width override in px. Negative = no override (-1 sentinel).
	/// We use a sentinel rather than nullable so the GameResource serializer
	/// round-trips without nullable-float ceremony.
	/// </summary>
	public float BorderWidth { get; set; } = -1f;

	/// <summary>Border radius override in px. Negative = no override.</summary>
	public float BorderRadius { get; set; } = -1f;

	/// <summary>Hex color string overriding the foreground text color in this state. Empty = inherit.</summary>
	public string TextColor { get; set; } = "";

	/// <summary>
	/// Transform scale (CSS <c>transform: scale(N)</c>). 1.0 = identity (no emit).
	/// Sub-1 shrinks, &gt;1 grows. Common pattern: Hover 1.05, Pressed 0.95.
	/// </summary>
	public float Scale { get; set; } = 1f;

	/// <summary>Opacity 0–1. Negative = no override. Common pattern: Disabled 0.5.</summary>
	public float Opacity { get; set; } = -1f;

	public bool IsEmpty()
	{
		return string.IsNullOrEmpty( BackgroundImage )
			&& string.IsNullOrEmpty( BackgroundColor )
			&& string.IsNullOrEmpty( BorderColor )
			&& BorderWidth < 0f
			&& BorderRadius < 0f
			&& string.IsNullOrEmpty( TextColor )
			&& Scale == 1f
			&& Opacity < 0f;
	}

	public SuiInteractiveStateStyle Clone() => new()
	{
		BackgroundImage = BackgroundImage,
		BackgroundColor = BackgroundColor,
		BorderColor = BorderColor,
		BorderWidth = BorderWidth,
		BorderRadius = BorderRadius,
		TextColor = TextColor,
		Scale = Scale,
		Opacity = Opacity,
	};
}
