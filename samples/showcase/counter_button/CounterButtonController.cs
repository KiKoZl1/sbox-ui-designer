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
	/// Wired to <c>IncrementButton.OnClick</c> in the .sui (Events.Mode = Code,
	/// Handler = "OnIncrementClick"). The generator emits a delegate slot on the
	/// wrapper that resolves to this method by name.
	/// </summary>
	public void OnIncrementClick()
	{
		Count++;
		Hud.CountText = Count.ToString();
	}
}
