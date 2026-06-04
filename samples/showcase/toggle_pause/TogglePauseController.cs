using Sandbox;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>toggle_pause</c> showcase sample.
///
/// <para>Drop this on a GameObject in any scene, press Play, and a small
/// dark bar appears at the top centre of the screen with a "Pause" toggle
/// and a status label. Flipping the toggle flips the label between
/// "Running" (green) and "Paused" (red).</para>
///
/// <para>The UI itself lives entirely in <c>toggle_pause.sui</c>; this
/// Component owns the live state and keeps the status label in sync with
/// the toggle via a tiny <see cref="OnUpdate"/> driver.</para>
/// </summary>
public sealed class TogglePauseController : Component
{
	/// <summary>
	/// Generated wrapper instance. Exposes <c>Hud.IsPaused</c> (TwoWay-bound
	/// to the toggle's <c>Checked</c> property) and <c>Hud.StatusText</c>
	/// (OneWay-bound to the Text element).
	/// </summary>
	[Property] public TogglePause Hud { get; set; } = new();

	/// <summary>
	/// Cached previous value so we only push to <c>Hud.StatusText</c> on the
	/// edge — keeps OnChange triggers cheap and avoids per-frame redraws.
	/// </summary>
	private bool _lastIsPaused;

	protected override void OnStart()
	{
		// MouseOnly: the toggle needs click input, but we don't want this HUD
		// to grab keyboard focus from the rest of the game.
		Hud.Show( GameObject, SuiInputMode.MouseOnly );

		// Seed the status label so frame zero matches the default toggle state.
		_lastIsPaused = Hud.IsPaused;
		Hud.StatusText = _lastIsPaused ? "Paused" : "Running";
	}

	protected override void OnUpdate()
	{
		// Cheap edge-triggered sync. The toggle's TwoWay binding mutates
		// Hud.IsPaused directly when the player clicks, so we just mirror
		// any flip into the status label.
		if ( Hud.IsPaused == _lastIsPaused )
			return;

		_lastIsPaused = Hud.IsPaused;
		Hud.StatusText = _lastIsPaused ? "Paused" : "Running";
	}
}
