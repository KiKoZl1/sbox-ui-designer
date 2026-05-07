using Editor;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Details dock — right side. Shows properties of the selected element (or the
/// document/canvas if nothing is selected).
///
/// M4: placeholder body — just shows what would be displayed.
/// M5/M8: full property editor wired through the controller.
/// </summary>
public class SuiDetailsWidget : Widget
{
	private SuiDocument _document;
	private SuiElement _selected;
	private Label _bodyLabel;

	public SuiDetailsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Details";
		Name = "SuiDetails";
		MinimumSize = new Vector2( 260, 200 );

		Layout = Layout.Column();
		Layout.Margin = 6;
		Layout.Spacing = 4;

		var header = new Label( "Details", this );
		header.SetStyles( "font-weight: bold; color: #e5e7eb; padding-bottom: 4px;" );
		Layout.Add( header );

		_bodyLabel = new Label( "(no selection)", this );
		_bodyLabel.WordWrap = true;
		_bodyLabel.SetStyles( "color: #9ca3af; font-size: 11px;" );
		Layout.Add( _bodyLabel );

		Layout.AddStretchCell();
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		Refresh();
	}

	public void SetSelected( SuiElement element )
	{
		_selected = element;
		Refresh();
	}

	private void Refresh()
	{
		if ( _selected != null )
		{
			_bodyLabel.Text =
				$"id: {_selected.Id}\n" +
				$"name: {_selected.Name}\n" +
				$"type: {_selected.Type}\n" +
				$"parent: {_selected.ParentId ?? "(root)"}\n" +
				$"children: {_selected.Children.Count}\n" +
				$"layout: {_selected.Layout?.Mode}\n" +
				$"pointer: {_selected.Style?.PointerEvents}\n" +
				"\nM4 placeholder — full property editor in M8.";
		}
		else if ( _document != null )
		{
			_bodyLabel.Text =
				$"Document: {_document.Name}\n" +
				$"id: {_document.DocumentId}\n" +
				$"schema: v{_document.SchemaVersion}\n" +
				$"elements: {_document.Elements.Count}\n" +
				$"canvas: {_document.Canvas?.BaseWidth}x{_document.Canvas?.BaseHeight}\n" +
				"\nClick an element in the Hierarchy to edit its properties.";
		}
		else
		{
			_bodyLabel.Text = "(no document loaded)";
		}
	}
}
