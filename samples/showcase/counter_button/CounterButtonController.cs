using Sandbox;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>counter_button</c> showcase sample.
///
/// <para>Drop this on a GameObject in any scene, press Play, and a small
/// dark panel with a green "0" label and a "+1" button appears in the
/// middle of the screen. Clicking the button increments the counter and
/// the label updates immediately.</para>
///
/// <para>The UI itself lives entirely in <c>counter_button.sui</c>; this
/// Component only owns the click handler and pushes the new value back
/// into the Variable exposed by the generated wrapper.</para>
/// </summary>
public sealed class CounterButtonController : Component
{
	/// <summary>
	/// Generated wrapper instance. Constructing it inline gives us access
	/// to <c>Hud.CountText</c> (the Variable) and <c>Hud.Show()</c> (the
	/// mount API) without any extra wiring.
	/// </summary>
	[Property] public CounterButton Hud { get; set; } = new();

	/// <summary>How many times the button has been clicked this session.</summary>
	[Property] public int Count { get; set; } = 0;

	protected override void OnStart()
	{
		// Wire the click handler BEFORE Show() so SyncFieldsTo carries the
		// delegate into the rendered Panel on first mount. The generator
		// emits OnIncrementClick as an Action property on the wrapper
		// (Events.Mode = Code in the .sui), so the controller assigns it
		// to its own method — name parity is convention, not magic.
		Hud.OnIncrementClick = OnIncrementClick;

		// Mount the UI as a child of this GameObject. MouseOnly so the
		// player can click the button without the panel grabbing keyboard
		// focus away from gameplay.
		Hud.Show( GameObject, SuiInputMode.MouseOnly );

		// Push the initial value into the Variable so the bound Text element
		// shows the current count from frame zero (instead of the default "0"
		// that's stored in the .sui — they happen to match here, but doing
		// this is the pattern you'd follow for non-trivial defaults).
		Hud.CountText = Count.ToString();
	}

	/// <summary>
	/// Called when the +1 button is clicked. Assigned to
	/// <c>Hud.OnIncrementClick</c> in <see cref="OnStart"/> — the wrapper
	/// exposes that as a public <c>Action</c> property which the generator
	/// wires to the button's <c>onclick</c> in the renderer Panel.
	/// </summary>
	private void OnIncrementClick()
	{
		Count++;
		Hud.CountText = Count.ToString();
	}
}
