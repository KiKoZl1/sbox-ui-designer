namespace SboxUiDesigner.Runtime;

/// <summary>
/// When a TwoWay (or OneWayToSource) binding pushes the UI's current value
/// back into the source Variable. Default is <see cref="OnChange"/> — every
/// keystroke / drag tick / click. Other triggers defer the write so user
/// code can read a stable value at a specific moment.
///
/// <para>Available triggers per widget are gated by
/// <see cref="SuiBindingModeMatrix.AllowedUpdateTriggers"/> — the BindPopup
/// hides the dropdown entirely when only one trigger applies.</para>
/// </summary>
public enum SuiBindingUpdateTrigger
{
	/// <summary>Fire on every change (keystroke, drag tick, click). Default.</summary>
	OnChange,

	/// <summary>TextEntry only — fire when the field loses focus (click outside / Tab).</summary>
	OnLostFocus,

	/// <summary>TextEntry only — fire when Enter is pressed.</summary>
	OnSubmit,

	/// <summary>Slider only — fire when the mouse is released after a drag.</summary>
	OnRelease,

	/// <summary>
	/// Never auto-fire. The wrapper exposes a <c>Commit&lt;FieldName&gt;()</c>
	/// method the user calls explicitly (e.g. from a Save button handler).
	/// </summary>
	Manual,
}
