using Editor;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Canvas dock — center. The visual designer where elements are placed and resized.
///
/// M4: placeholder body. The real preview host (Editor.SceneRenderingWidget hosting
/// an editor-owned Scene with ScreenPanel + the generated PanelComponent) is built
/// in M10/Spike 01.
///
/// M11: wires canvas interaction tools (selection, move, resize, zoom/pan).
/// </summary>
public class SuiCanvasWidget : Widget
{
	private SuiDocument _document;
	private Label _placeholder;

	public SuiCanvasWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Canvas";
		Name = "SuiCanvas";
		MinimumSize = new Vector2( 400, 300 );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		_placeholder = new Label( "Visual designer canvas\n\nSpike 01 (M10) embeds Editor.SceneRenderingWidget here.\n\nThe scene-rendering widget hosts an editor-owned Scene whose root\nGameObject carries ScreenPanel + the generated PanelComponent.\nEditor selection/handle overlays render on a stacked Editor.Widget.", this );
		_placeholder.Alignment = TextFlag.Center;
		_placeholder.WordWrap = true;
		_placeholder.SetStyles( "color: #6b7280; font-size: 12px; padding: 24px;" );
		Layout.Add( _placeholder, 1 );
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		// M10 will replace the placeholder with the SceneRenderingWidget here.
		// For now, just update the placeholder text so the user knows a doc was loaded.
		if ( _placeholder != null && document != null )
		{
			var rootCount = document.Elements.Count;
			_placeholder.Text = $"Document loaded: {document.Name}\n{rootCount} element{(rootCount == 1 ? "" : "s")}\n\nSpike 01 (M10) will host the runtime preview here\nvia Editor.SceneRenderingWidget.";
		}
	}
}
