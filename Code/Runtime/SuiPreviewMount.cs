using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Lives on a GameObject inside the Test-in-Play stage scene
/// (preview_stage.scene). On OnAwake reads <see cref="SuiPreviewState.PendingTypeFullName"/>,
/// looks up the generated type via TypeLibrary, and mounts it.
///
/// <para>V1.5-M2-K7 — the generated class is now <see cref="Panel"/>
/// (not <see cref="PanelComponent"/>). We spawn a <see cref="SuiHostPanelComponent"/>
/// to provide a ScreenPanel root and add the generated Panel as its child.</para>
/// </summary>
public sealed class SuiPreviewMount : Component
{
	[Property, Title( "Mounted Type FQN (debug)" ), ReadOnly]
	public string MountedFqn { get; private set; } = "";

	private GameObject _panelHost;

	protected override void OnAwake()
	{
		// Defer to a Task so we can await Task.Frame() and let the host
		// component's OnEnabled fire naturally — Sandbox blocks reflection
		// in the runtime whitelist, so we can't force-invoke OnEnabledInternal
		// the way the editor-side SuiPreviewHost does.
		_ = MountAsync();
	}

	private async Task MountAsync()
	{
		var fqn = SuiPreviewState.PendingTypeFullName;
		if ( string.IsNullOrEmpty( fqn ) )
		{
			Log.Info( "[SuiPreviewMount] No PendingTypeFullName set — running stage without UI." );
			return;
		}

		var typeDesc = TypeLibrary.GetType( fqn );
		if ( typeDesc == null )
		{
			Log.Warning( $"[SuiPreviewMount] TypeLibrary.GetType('{fqn}') returned null. Was the preview cache compiled before EditorScene.Play?" );
			return;
		}

		_panelHost = new GameObject( true, "ScreenPanelHost" );
		_panelHost.SetParent( GameObject );
		_panelHost.GetOrAddComponent<ScreenPanel>();

		// V1.5-M2-K7 — the generated class is a Panel subclass, so we cannot
		// Components.Create<TPanel>() (Panel isn't a Component). Instead, host
		// a SuiHostPanelComponent (PanelComponent with empty render tree) and
		// add the generated Panel as a child of its root.
		var hostComponent = _panelHost.Components.Create<SuiHostPanelComponent>();
		if ( hostComponent == null )
		{
			Log.Warning( "[SuiPreviewMount] Components.Create<SuiHostPanelComponent> returned null." );
			return;
		}

		// V1.5-M4 — wait up to ~10 frames for the host's OnEnabled to fire.
		// Components.Create returns synchronously but the engine's lifecycle
		// chain (OnAwake → OnEnabled → EnsurePanelCreated) can take 1-2 frames
		// to complete. Don't busy-loop forever — bail with a warning if Panel
		// stays null past 10 frames (engine API regression).
		for ( int i = 0; i < 10 && hostComponent.Panel == null; i++ )
			await Task.Frame();

		if ( !this.IsValid() || !hostComponent.IsValid() ) return;

		if ( hostComponent.Panel == null )
		{
			Log.Warning( "[SuiPreviewMount] SuiHostPanelComponent.Panel still null after 10 frames — engine lifecycle may have changed." );
			return;
		}

		var instance = typeDesc.Create<object>();
		if ( instance is not Panel renderedPanel )
		{
			Log.Warning( $"[SuiPreviewMount] '{fqn}' is not a Panel (got {instance?.GetType().FullName ?? "null"}). Compile shape mismatch." );
			return;
		}

		hostComponent.Panel.AddChild( renderedPanel );

		MountedFqn = fqn;
		Log.Info( $"[SuiPreviewMount] Mounted Panel '{fqn}' inside SuiHostPanelComponent." );

		// Clear so a future Play that wasn't initiated by the launcher doesn't
		// silently reuse a stale FQN.
		SuiPreviewState.Clear();
	}
}
