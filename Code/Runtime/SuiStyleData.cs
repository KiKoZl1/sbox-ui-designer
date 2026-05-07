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
		Opacity = Opacity,
		Visibility = Visibility,
		PointerEvents = PointerEvents,
		Overflow = Overflow,
	};
}
