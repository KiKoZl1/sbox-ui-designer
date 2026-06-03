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

		// V1.5-M4 fix — use Scene.CreateObject(true) (same as SuiPanel<TView>.Add)
		// instead of `new GameObject(true,...)`. Scene.CreateObject attaches the
		// GO to the scene synchronously and fires the OnAwake → OnEnabled chain
		// inline, so Components.Create<SuiHostPanelComponent> below populates
		// Host.Panel immediately. The `new GameObject` path produced an orphan
		// whose lifecycle deferred to a later frame — the silent bug introduced
		// at K7 (commit 523df4e) and only surfaced when the user finally tested
		// Test-in-Play after M4.
		_panelHost = Scene.CreateObject( true );
		_panelHost.Name = "ScreenPanelHost";
		_panelHost.SetParent( GameObject );
		_panelHost.Components.Create<ScreenPanel>();

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

		// V1.5-M2-K7 (commit 7330416) — Panel subclasses generated from .razor
		// only run their BuildRenderTree (which populates children from the
		// <root> markup) when constructed via the Sandbox.UI factory path:
		// AddChild<T>() / Add.Panel<T>(). Plain `new T()` or `typeDesc.Create`
		// instantiates the Panel object but the template body stays empty.
		// We don't have T at compile time here, so we call the non-generic
		// AddChild(Type) overload which the engine threads to the same factory.
		var renderedPanel = hostComponent.Panel.AddChild( typeDesc.TargetType ) as Panel;
		if ( renderedPanel == null )
		{
			Log.Warning( $"[SuiPreviewMount] AddChild('{typeDesc.TargetType.FullName}') did not return a Panel — generated class shape mismatch." );
			return;
		}

		MountedFqn = fqn;
		Log.Info( $"[SuiPreviewMount] Mounted Panel '{fqn}' inside SuiHostPanelComponent." );

		// Clear so a future Play that wasn't initiated by the launcher doesn't
		// silently reuse a stale FQN.
		SuiPreviewState.Clear();
	}
}
