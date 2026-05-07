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
		Camera.ZFar = 8192;
		Camera.FieldOfView = 60;
		// Reading WorldPanel.OnPreRender: PanelBounds get divided by RenderScale
		// (so they're 10x larger when RenderScale=0.1) and the world Transform
		// is scaled by RenderScale. Net world-space size = PanelSize, regardless
		// of RenderScale. So PanelSize=1920x1080 produces a 1920x1080 WORLD unit
		// panel (huge). At FOV 60 vertical, distance D needs D * tan(30°) >=
		// half-height (540). D = 540 / 0.577 ≈ 935. Pulling back to 1500 leaves
		// margin for the cyan WorldPanel gizmo to also be visible.
		Camera.WorldPosition = new Vector3( -1500, 0, 0 );
		Camera.WorldRotation = Rotation.Identity;
		Camera.Enabled = true;

		Log.Info( $"[Sui preview] camera built at {Camera.WorldPosition}, FOV {Camera.FieldOfView}, ZFar {Camera.ZFar}" );
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
		worldPanel.RenderScale = 1.0f; // keep simple for debug — see OnPreRender math in WorldPanel.cs
		worldPanel.HorizontalAlign = WorldPanel.HAlignment.Center;
		worldPanel.VerticalAlign = WorldPanel.VAlignment.Center;

		var testPanel = _uiHost.AddComponent<SuiTestPanel>();

		Log.Info( $"[Sui preview] WorldPanel built. PanelSize={worldPanel.PanelSize}, RenderScale={worldPanel.RenderScale}, LookAtCamera={worldPanel.LookAtCamera}" );
		Log.Info( $"[Sui preview] SuiTestPanel attached: {testPanel != null}, IsValid={testPanel?.IsValid()}" );

		// Reference cube at the panel's right side to confirm camera sees this
		// region. If the cube shows but the panel doesn't, the panel rendering
		// itself is the issue. If neither shows, it's a camera/scene problem.
		var probe = new GameObject( true, "DebugProbe" );
		probe.WorldPosition = new Vector3( 0, 1100, 0 ); // 1100 units to the right (s&box +Y is right? Let me try this and see)
		var probeRenderer = probe.GetOrAddComponent<ModelRenderer>( false );
		probeRenderer.Model = Model.Load( "models/dev/box.vmdl" );
		probeRenderer.Tint = new Color( 1f, 0.4f, 0.2f, 1f );
		probeRenderer.Enabled = true;
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
