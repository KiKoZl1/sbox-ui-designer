using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Per-element visual style block. Generated to SCSS by the generator.
/// Field values null/default mean "do not emit a rule".
/// </summary>
public sealed class SuiStyleData
{
	/// <summary>
	/// Primary CSS class name for this element. The generator sanitizes and
	/// optionally suffix-deduplicates against siblings.
	/// </summary>
	public string ClassName { get; set; }

	/// <summary>Extra class names appended after <see cref="ClassName"/>.</summary>
	public List<string> CustomClasses { get; set; } = new();

	/// <summary>Hex color (e.g. "#151515cc"). Null = no rule.</summary>
	public string BackgroundColor { get; set; }

	public string BorderColor { get; set; }
	public float BorderWidth { get; set; } = 0f;
	public float BorderRadius { get; set; } = 0f;

	/// <summary>
	/// V1.5 M3.5 (PRD 25) — background image path for the element's Normal
	/// visual state. Lives on SuiStyleData so any element type can carry it
	/// (the runtime engine resolves it via background-image: url(...) just like
	/// any standard CSS rule). Empty = no emit. Compatible with V3 schema —
	/// additive field, default empty.
	/// </summary>
	public string BackgroundImage { get; set; } = "";

	/// <summary>
	/// V1.5 M3.5 — how the background image is sized within the element rect.
	/// Default Contain (no distortion) — preferred for image-based buttons
	/// where the author wants the full art visible. Cover crops to fill.
	/// </summary>
	public SuiBackgroundSize BackgroundSize { get; set; } = SuiBackgroundSize.Contain;

	/// <summary>Pixel width used when <see cref="BackgroundSize"/> is Custom. 0 = no emit.</summary>
	public float BackgroundWidth { get; set; } = 0f;

	/// <summary>Pixel height used when <see cref="BackgroundSize"/> is Custom. 0 = no emit.</summary>
	public float BackgroundHeight { get; set; } = 0f;

	/// <summary>0..1 — values outside this range are clamped by the validator.</summary>
	public float Opacity { get; set; } = 1f;

	public SuiVisibility Visibility { get; set; } = SuiVisibility.Visible;

	/// <summary>Default depends on element type — see <see cref="SuiPointerEvents"/>.</summary>
	public SuiPointerEvents PointerEvents { get; set; } = SuiPointerEvents.None;

	public SuiOverflow Overflow { get; set; } = SuiOverflow.Visible;

	public SuiStyleData Clone() => new()
	{
		ClassName = ClassName,
		CustomClasses = new List<string>( CustomClasses ?? new() ),
		BackgroundColor = BackgroundColor,
		BorderColor = BorderColor,
		BorderWidth = BorderWidth,
		BorderRadius = BorderRadius,
		BackgroundImage = BackgroundImage,
		BackgroundSize = BackgroundSize,
		BackgroundWidth = BackgroundWidth,
		BackgroundHeight = BackgroundHeight,
		Opacity = Opacity,
		Visibility = Visibility,
		PointerEvents = PointerEvents,
		Overflow = Overflow,
	};
}
