using System;
using Editor;
using Sandbox;
using Sandbox.UI;
using SboxUiDesigner.Runtime;
// Both Sandbox.WorldPanel (the Renderer component we want) and
// Sandbox.UI.WorldPanel (the runtime panel it wraps) are in scope via
// the usings above. Alias to disambiguate to the component type only.
using WorldPanel = Sandbox.WorldPanel;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Wraps an editor-owned <see cref="Scene"/> that hosts the runtime UI
/// preview. The Scene contains:
///  - A <see cref="CameraComponent"/> looking at a 2D ScreenPanel space
///  - Ambient + directional lights (so 3D content is visible)
///  - In Etapa B+: a GameObject carrying ScreenPanel + the generated
///    PanelComponent
///
/// Etapa A (current) — no UI yet, just camera + lights + a placeholder
/// model so we can VISUALLY confirm SceneRenderingWidget can host an
/// editor-owned Scene at all. This is the make-or-break test for the
/// preview embed architecture (Spike 01).
///
/// Pattern reference: Facepunch
/// sbox-public/game/addons/tools/Code/WidgetGallery/Examples/Scene/GizmoSceneTest.cs
/// </summary>
public sealed class SuiPreviewHost
{
	public Scene Scene { get; }
	public CameraComponent Camera { get; private set; }

	private GameObject _uiHost;

	public SuiPreviewHost()
	{
		Scene = Scene.CreateEditorScene();

		using ( Scene.Push() )
		{
			BuildCamera();
			BuildLights();
			BuildUiHost();
		}
	}

	private void BuildCamera()
	{
		var cameraGo = new GameObject( true, "Camera" );
		Camera = cameraGo.GetOrAddComponent<CameraComponent>( false );
		Camera.BackgroundColor = new Color( 0.08f, 0.08f, 0.10f, 1f );
		Camera.ZFar = 4096;
		Camera.FieldOfView = 60;
		// Camera looks at origin (+X forward by s&box convention). Distance
		// chosen so a 1920x1080 WorldPanel @ RenderScale 0.1 (so 192x108 world
		// units) fills the viewport at FOV 60: ~108 / (2 * tan(30°)) ≈ 94 units.
		// Pulling back to 200 leaves comfortable margin around the panel.
		Camera.WorldPosition = new Vector3( -200, 0, 0 );
		Camera.WorldRotation = Rotation.Identity;
		Camera.Enabled = true;
	}

	private void BuildLights()
	{
		var ambient = new GameObject( true, "Ambient" ).GetOrAddComponent<AmbientLight>( false );
		ambient.Color = new Color( 0.3f, 0.32f, 0.36f, 1f );
		ambient.Enabled = true;

		var directional = new GameObject( true, "Directional" ).GetOrAddComponent<DirectionalLight>( false );
		directional.WorldRotation = Rotation.From( 45, 45, 0 );
		directional.LightColor = Color.White;
		directional.Enabled = true;
	}

	/// <summary>
	/// Etapa B — GameObject that carries WorldPanel + a static PanelComponent
	/// (SuiTestPanel). WorldPanel is a Renderer, so it participates in the
	/// SceneWorld render that SceneRenderingWidget shows. ScreenPanel is a
	/// HUD overlay rendered AFTER the camera and isn't visible in the editor
	/// SceneRenderingWidget — that's why the previous attempt showed nothing.
	///
	/// LookAtCamera = true makes the panel billboard-face the camera, which
	/// gives us the flat HUD-style 2D preview UMG users expect (a "screen"
	/// floating in the editor 3D space). RenderScale 0.1 maps the document's
	/// 1920x1080 logical pixels to 192x108 world units so the camera can see
	/// the whole thing without having to back up 1500+ world units.
	/// </summary>
	private void BuildUiHost()
	{
		_uiHost = new GameObject( true, "UIHost" );
		_uiHost.WorldPosition = Vector3.Zero;

		var worldPanel = _uiHost.AddComponent<WorldPanel>();
		worldPanel.LookAtCamera = true;
		worldPanel.PanelSize = new Vector2( 1920, 1080 );
		worldPanel.RenderScale = 0.1f;
		worldPanel.HorizontalAlign = WorldPanel.HAlignment.Center;
		worldPanel.VerticalAlign = WorldPanel.VAlignment.Center;

		_uiHost.AddComponent<SuiTestPanel>();
	}

	/// <summary>
	/// Step the editor scene forward one frame. Called from the canvas
	/// widget's OnPreFrame so the scene runs while the editor is open.
	/// </summary>
	public void Tick()
	{
		Scene?.EditorTick( RealTime.Now, RealTime.Delta );
	}
}
