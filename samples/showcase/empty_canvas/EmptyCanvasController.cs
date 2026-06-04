using Sandbox;
using Sandbox.UI;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the empty_canvas showcase sample.
///
/// Drop this on a GameObject in your scene, hit Play, and the bare
/// "Hello SUI!" panel appears centered on screen.
///
/// This is the minimum viable SUI integration: no Variables to drive,
/// no Bindings to update, no Events to handle. Its only job is to
/// prove the wrapper-mounting plumbing works end-to-end.
/// </summary>
public sealed class EmptyCanvasController : Component
{
	/// <summary>
	/// The generated SUI wrapper. Auto-instantiated so the inspector
	/// shows the property without requiring manual assignment.
	/// </summary>
	[Property]
	public EmptyCanvas Hud { get; set; } = new();

	protected override void OnStart()
	{
		// Passive = render only, no mouse/keyboard capture. Perfect for
		// a static HUD that shouldn't steal focus from the game.
		Hud?.Show( SuiInputMode.Passive );
	}

	protected override void OnDestroy()
	{
		// Tear the panel down when the component goes away so we don't
		// leak a RootPanel into the next scene load.
		Hud?.Hide();
	}
}
