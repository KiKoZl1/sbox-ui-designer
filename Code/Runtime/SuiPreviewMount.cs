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
		if ( hostComponent?.Panel == null )
		{
			Log.Warning( "[SuiPreviewMount] SuiHostPanelComponent did not initialize its Panel (OnEnabled may not have fired)." );
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
