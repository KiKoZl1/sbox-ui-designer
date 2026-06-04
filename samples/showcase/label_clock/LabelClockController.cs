using Sandbox;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>label_clock</c> showcase sample.
/// Drop this on a GameObject in the scene, hit Play, and the clock HUD appears
/// in the top-right corner. The text updates every frame from the system clock.
/// </summary>
/// <remarks>
/// The .sui defines a single <c>ClockText : string</c> Variable bound OneWay to
/// the <c>Text</c> property of the clock value label. This controller simply
/// writes the formatted current time to that Variable in <c>OnUpdate</c>, which
/// triggers the binding's OnChange path and refreshes the rendered text.
/// </remarks>
public sealed class LabelClockController : Component
{
	/// <summary>
	/// The generated UI wrapper. SUI Designer generates <see cref="LabelClock"/>
	/// as a <c>PanelComponent</c> with a public <c>ClockText</c> auto-property
	/// matching the Variable declared in the .sui.
	/// </summary>
	[Property] public LabelClock Hud { get; set; } = new();

	/// <summary>
	/// .NET format string fed to <see cref="System.DateTime.ToString(string)"/>.
	/// Defaults to <c>HH:mm:ss</c> (e.g. <c>14:07:42</c>). Try <c>hh:mm:ss tt</c>
	/// for 12-hour, or <c>HH:mm</c> for a slim version.
	/// </summary>
	[Property] public string Format { get; set; } = "HH:mm:ss";

	/// <summary>
	/// When true, uses UTC instead of local time. Useful for showing a server
	/// clock that stays consistent across players in different timezones.
	/// </summary>
	[Property] public bool UseUtc { get; set; } = false;

	protected override void OnStart()
	{
		// Passive: the clock is read-only HUD, no mouse / keyboard input needed.
		// The wrapper still receives layout + rendering; pointer events fall
		// straight through to gameplay.
		Hud?.Show( SuiInputMode.Passive );

		// Seed once so the first visible frame is correct, not "00:00:00".
		PushTime();
	}

	protected override void OnUpdate()
	{
		PushTime();
	}

	private void PushTime()
	{
		if ( Hud is null )
			return;

		var now = UseUtc ? System.DateTime.UtcNow : System.DateTime.Now;
		Hud.ClockText = now.ToString( Format );
	}
}
