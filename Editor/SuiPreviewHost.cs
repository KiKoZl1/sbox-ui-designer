using System;
using Editor;
using Sandbox;

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

	private GameObject _placeholderModel;

	public SuiPreviewHost()
	{
		Scene = Scene.CreateEditorScene();

		using ( Scene.Push() )
		{
			BuildCamera();
			BuildLights();
			BuildPlaceholderContent();
		}
	}

	private void BuildCamera()
	{
		var cameraGo = new GameObject( true, "Camera" );
		Camera = cameraGo.GetOrAddComponent<CameraComponent>( false );
		Camera.BackgroundColor = new Color( 0.08f, 0.08f, 0.10f, 1f );
		Camera.ZFar = 4096;
		Camera.FieldOfView = 60;
		Camera.WorldPosition = new Vector3( -200, 0, 80 );
		Camera.WorldRotation = Rotation.From( 0, 0, 0 );
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
	/// Etapa A placeholder — a simple ModelRenderer with the engine's default
	/// box / arrow so we have something visible to prove the pipeline works.
	/// Etapa B replaces this with a GameObject carrying ScreenPanel + the
	/// generated PanelComponent.
	/// </summary>
	private void BuildPlaceholderContent()
	{
		_placeholderModel = new GameObject( true, "PreviewProbe" );
		var renderer = _placeholderModel.GetOrAddComponent<ModelRenderer>( false );
		renderer.Model = Model.Load( "models/dev/box.vmdl" );
		renderer.Tint = new Color( 0.3f, 0.6f, 1f, 1f );
		renderer.Enabled = true;
		_placeholderModel.WorldPosition = Vector3.Zero;
		_placeholderModel.WorldScale = new Vector3( 0.5f, 0.5f, 0.5f );
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
