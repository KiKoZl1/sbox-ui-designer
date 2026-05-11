namespace SboxUiDesigner.Runtime;

/// <summary>
/// Property binding entry — associates an element property (Text, Value, Items,
/// etc) with a runtime data path. Used by the Bindings tab in the Designer
/// (mockup: HealthBar.Value → Player.Health).
///
/// Distinct from <see cref="SuiEventBinding"/> which handles events (OnClick).
///
/// V1: persisted in the .sui document, edited via the Bindings tab UI, not
/// yet emitted into the generated Razor. V2 will translate these into
/// <c>@bind-Property</c> attribute pairs + corresponding code in the generated
/// PanelComponent.
/// </summary>
public sealed class SuiPropertyBinding
{
	/// <summary>Element id this binding targets.</summary>
	public string TargetElementId { get; set; }

	/// <summary>Property on the target element (e.g. "Value", "Text", "Items").</summary>
	public string Property { get; set; }

	/// <summary>Source object path, e.g. "Player", "Weapon".</summary>
	public string Source { get; set; }

	/// <summary>Path on the source object, e.g. "Health", "CurrentAmmo".</summary>
	public string Path { get; set; }

	/// <summary>Binding mode — typical values: "OneWay", "TwoWay", "OneTime".</summary>
	public string Mode { get; set; } = "OneWay";

	public SuiPropertyBinding Clone() => new()
	{
		TargetElementId = TargetElementId,
		Property = Property,
		Source = Source,
		Path = Path,
		Mode = Mode,
	};
}
