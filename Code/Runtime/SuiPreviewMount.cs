using Sandbox;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Lives on a GameObject inside the Test-in-Play stage scene
/// (preview_stage.scene). On OnAwake it reads <see cref="SuiPreviewState.PendingTypeFullName"/>,
/// looks up the generated PanelComponent type via TypeLibrary, and mounts it
/// on a child <see cref="ScreenPanel"/> so the user's UI shows up as a real
/// HUD overlay in Play mode.
///
/// Unlike <c>SuiPreviewHost</c> (editor-owned scene + reflection workarounds),
/// this component runs under real Play mode lifecycle — no reflection needed.
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
			Log.Warning( $"[SuiPreviewMount] TypeLibrary.GetType('{fqn}') returned null. The generated PanelComponent isn't loaded — was the preview cache compiled before EditorScene.Play?" );
			return;
		}

		// Build the host hierarchy: this GO -> ScreenPanelHost -> ScreenPanel + user's PanelComponent.
		_panelHost = new GameObject( true, "ScreenPanelHost" );
		_panelHost.SetParent( GameObject );

		// ScreenPanel is the root for any PanelComponent stack — required so the
		// user's UI renders to the screen overlay.
		_panelHost.GetOrAddComponent<ScreenPanel>();

		var created = _panelHost.Components.Create( typeDesc );
		if ( created is not Component panel )
		{
			Log.Warning( $"[SuiPreviewMount] Components.Create('{fqn}') returned non-Component (got {created?.GetType().FullName ?? "null"})." );
			return;
		}

		MountedFqn = fqn;
		Log.Info( $"[SuiPreviewMount] Mounted '{fqn}' on ScreenPanel." );

		// Clear so a future Play that wasn't initiated by the launcher doesn't
		// silently reuse a stale FQN.
		SuiPreviewState.Clear();
	}
}
