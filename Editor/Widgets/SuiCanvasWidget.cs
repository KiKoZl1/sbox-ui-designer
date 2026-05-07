using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Canvas dock — center of the designer. Hosts the runtime preview via
/// <see cref="SceneRenderingWidget"/> bound to a <see cref="SuiPreviewHost"/>'s
/// editor-owned <see cref="Scene"/>.
///
/// Spike 01 etapas:
///  A. Editor-owned Scene with camera + lights + a placeholder model.
///     Proves SceneRenderingWidget can render an editor-owned Scene at all.
///  B. Replace placeholder model with a GameObject carrying ScreenPanel +
///     a static PanelComponent.
///  C. Generated Razor/SCSS lands in &lt;project&gt;/Code/_sui_preview/&lt;Doc&gt;/.
///  D. Reflect + instantiate the generated PanelComponent dynamically.
///  E. Wire SuiDesignerController.DocumentChanged → debounce → regen.
/// </summary>
public class SuiCanvasWidget : Widget
{
	private SuiDocument _document;

	private SuiPreviewHost _previewHost;
	private SceneRenderingWidget _sceneWidget;

	public SuiCanvasWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Canvas";
		Name = "SuiCanvas";
		MinimumSize = new Vector2( 400, 300 );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		_previewHost = new SuiPreviewHost();

		_sceneWidget = new SceneRenderingWidget( this );
		_sceneWidget.Scene = _previewHost.Scene;
		_sceneWidget.Camera = _previewHost.Camera;
		_sceneWidget.OnPreFrame += OnPreFrame;
		// Engine overlays include WorldPanel's DrawGizmos cyan rectangle —
		// turning them on lets us VISUALLY verify the WorldPanel is positioned
		// where we expect, even if its content isn't rendering yet.
		_sceneWidget.EnableEngineOverlays = true;
		_sceneWidget.SetSizeMode( SizeMode.CanGrow, SizeMode.CanGrow );
		Layout.Add( _sceneWidget, 1 );
	}

	private void OnPreFrame()
	{
		_previewHost?.Tick();
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		// Etapa B will pipe document into the preview host so the preview
		// reflects the loaded .sui — for Etapa A we just keep showing the
		// placeholder probe regardless of which document is loaded.
	}
}
