using System;
using System.Reflection;
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
		if ( hostComponent == null )
		{
			Log.Warning( "[SuiPreviewMount] Components.Create<SuiHostPanelComponent> returned null." );
			return;
		}

		// V1.5-M4 — `Components.Create` doesn't always fire OnEnabledInternal
		// synchronously even in Play mode, so the host's `Panel` field can
		// still be null when we check it. Force the lifecycle via reflection
		// (same workaround SuiPreviewHost uses for editor scenes; harmless to
		// call when OnEnabled has already fired).
		TryInvokeLifecycle( hostComponent, "OnEnabledInternal" );

		if ( hostComponent.Panel == null )
		{
			Log.Warning( "[SuiPreviewMount] SuiHostPanelComponent.Panel still null after OnEnabledInternal invoke — engine API may have changed." );
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

	/// <summary>
	/// Force-invoke a lifecycle method via reflection. Mirrors the editor-side
	/// SuiPreviewHost helper. Safe no-op when the method has already fired —
	/// PanelComponent's EnsurePanelCreated is idempotent.
	/// </summary>
	private static void TryInvokeLifecycle( Component target, string methodName )
	{
		if ( target == null ) return;
		var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var type = target.GetType();
		MethodInfo m = null;
		while ( type != null )
		{
			m = type.GetMethod( methodName, bf | BindingFlags.DeclaredOnly );
			if ( m != null ) break;
			type = type.BaseType;
		}
		if ( m == null )
		{
			Log.Warning( $"[SuiPreviewMount] {target.GetType().Name}.{methodName} not found via reflection." );
			return;
		}
		try
		{
			m.Invoke( target, null );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[SuiPreviewMount] {target.GetType().Name}.{methodName} threw: {ex.InnerException?.Message ?? ex.Message}" );
		}
	}
}
