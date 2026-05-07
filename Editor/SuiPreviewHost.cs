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
		// Back to the Etapa-A camera position (worked for the cube). The panel
		// will be scaled DOWN to fit by setting GameObject.WorldScale rather
		// than fighting WorldPanel.RenderScale (which doesn't actually change
		// world size — verified by reading OnPreRender in WorldPanel.cs).
		Camera.WorldPosition = new Vector3( -200, 0, 0 );
		Camera.WorldRotation = Rotation.Identity;
		Camera.Enabled = true;

		// Scene.Camera is read-only — engine auto-resolves to the first
		// enabled CameraComponent in the scene. Logging it confirms our
		// camera is the chosen one.
		Log.Info( $"[Sui preview] camera built at {Camera.WorldPosition}, FOV {Camera.FieldOfView}, ZFar {Camera.ZFar}, Scene.Camera resolves to {Scene.Camera}" );
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
		// Anchor cube at origin — same as Etapa A. Confirms camera sees this
		// region. If THIS cube doesn't appear, no point debugging the panel.
		var anchorCube = new GameObject( true, "AnchorCube" );
		anchorCube.WorldPosition = Vector3.Zero;
		anchorCube.WorldScale = new Vector3( 0.3f, 0.3f, 0.3f );
		var anchorRenderer = anchorCube.GetOrAddComponent<ModelRenderer>( false );
		anchorRenderer.Model = Model.Load( "models/dev/box.vmdl" );
		anchorRenderer.Tint = new Color( 0.3f, 0.6f, 1f, 1f );
		anchorRenderer.Enabled = true;

		// Side reference cubes — one along each axis from origin so we can
		// see the panel's spatial relationship.
		AddProbeCube( "ProbeY+", new Vector3( 0,  60, 0 ), new Color( 1f, 0.4f, 0.2f, 1f ) );
		AddProbeCube( "ProbeY-", new Vector3( 0, -60, 0 ), new Color( 1f, 0.7f, 0.2f, 1f ) );
		AddProbeCube( "ProbeZ+", new Vector3( 0,   0, 60 ), new Color( 0.4f, 1f, 0.2f, 1f ) );
		AddProbeCube( "ProbeZ-", new Vector3( 0,   0, -60 ), new Color( 0.2f, 1f, 0.7f, 1f ) );

		// UI host: 1920x1080 panel scaled to 192x108 world units via
		// GameObject.WorldScale (RenderScale doesn't actually change world
		// size, verified). At camera distance 200 with FOV 60 vertical,
		// half-height visible ≈ 115 — panel half-height 54 fits comfortably.
		_uiHost = new GameObject( true, "UIHost" );
		_uiHost.WorldPosition = Vector3.Zero;
		_uiHost.WorldScale = new Vector3( 0.1f, 0.1f, 0.1f );

		var worldPanel = _uiHost.AddComponent<WorldPanel>();
		worldPanel.LookAtCamera = true;
		worldPanel.PanelSize = new Vector2( 1920, 1080 );
		worldPanel.RenderScale = 1.0f;
		worldPanel.HorizontalAlign = WorldPanel.HAlignment.Center;
		worldPanel.VerticalAlign = WorldPanel.VAlignment.Center;

		var testPanel = _uiHost.AddComponent<SuiTestPanel>();

		Log.Info( $"[Sui preview] anchor cube + 4 probes + UIHost built. WorldScale={_uiHost.WorldScale}" );
		Log.Info( $"[Sui preview] WorldPanel: PanelSize={worldPanel.PanelSize}, LookAtCamera={worldPanel.LookAtCamera}" );
		Log.Info( $"[Sui preview] SuiTestPanel attached: {testPanel != null}, IsValid={testPanel?.IsValid()}" );
	}

	private void AddProbeCube( string name, Vector3 position, Color tint )
	{
		var cube = new GameObject( true, name );
		cube.WorldPosition = position;
		cube.WorldScale = new Vector3( 0.2f, 0.2f, 0.2f );
		var r = cube.GetOrAddComponent<ModelRenderer>( false );
		r.Model = Model.Load( "models/dev/box.vmdl" );
		r.Tint = tint;
		r.Enabled = true;
	}

	/// <summary>
	/// Step the preview scene forward one frame. Called from the canvas
	/// widget's OnPreFrame so the scene runs while the editor is open.
	///
	/// Using GameTick instead of EditorTick because EditorTick appears to
	/// skip the OnPreRender lifecycle that WorldPanel relies on (it sets
	/// the panel's Transform / PanelBounds inside OnPreRender; without
	/// that pass, the panel exists but never positions itself).
	/// </summary>
	public void Tick()
	{
		if ( Scene == null ) return;
		Scene.GameTick( RealTime.Delta );
	}
}
