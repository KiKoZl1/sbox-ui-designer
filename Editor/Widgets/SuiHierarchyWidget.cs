using System;
using System.Collections.Generic;
using Editor;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Hierarchy dock — left side, below palette. Shows the document's element tree.
///
/// M4: simple indented list of buttons (one per element). The s&box <c>TreeView</c>
/// has a richer API that we'll adopt in M5/M7 when document mutations and rename
/// flow are wired through the controller.
/// </summary>
public class SuiHierarchyWidget : Widget
{
	private SuiDocument _document;
	private SuiElement _selected;

	private Widget _scrollHost;
	private Layout _listLayout;

	/// <summary>Raised when the user clicks an element in the list.</summary>
	public event Action<SuiElement> ElementSelected;

	public SuiHierarchyWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Hierarchy";
		Name = "SuiHierarchy";
		MinimumSize = new Vector2( 200, 200 );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		var header = new Label( "Hierarchy", this );
		header.SetStyles( "padding: 6px; font-weight: bold; color: #e5e7eb;" );
		Layout.Add( header );

		_scrollHost = new Widget( this );
		_scrollHost.Layout = Layout.Column();
		_scrollHost.Layout.Margin = new Sandbox.UI.Margin( 4, 4, 4, 4 );
		_scrollHost.Layout.Spacing = 1;
		_listLayout = _scrollHost.Layout;
		Layout.Add( _scrollHost, 1 );

		Refresh();
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		_selected = null;
		Refresh();
	}

	public void SetSelected( SuiElement element )
	{
		_selected = element;
		Refresh();
	}

	public void Refresh()
	{
		_listLayout.Clear( true );

		if ( _document == null )
		{
			var msg = new Label( "(no document loaded)", _scrollHost );
			msg.SetStyles( "color: #6b7280; font-size: 11px; padding: 8px;" );
			_listLayout.Add( msg );
			return;
		}

		var root = _document.GetRoot();
		if ( root == null )
		{
			var msg = new Label( "(empty document — no root)", _scrollHost );
			msg.SetStyles( "color: #6b7280; font-size: 11px; padding: 8px;" );
			_listLayout.Add( msg );
			return;
		}

		var byId = new Dictionary<string, SuiElement>();
		foreach ( var el in _document.Elements )
		{
			if ( !string.IsNullOrEmpty( el.Id ) ) byId[el.Id] = el;
		}

		AddElementRow( root, byId, depth: 0 );
	}

	private void AddElementRow( SuiElement element, Dictionary<string, SuiElement> byId, int depth )
	{
		var isSelected = _selected != null && _selected.Id == element.Id;

		// Row = spacer (depth-based) + element button. The Button's text is
		// center-aligned by default and CSS text-align does not always override
		// it, so we get visual nesting by physically shifting the button right
		// with a fixed-width spacer rather than relying on padding.
		var row = new Widget( _scrollHost );
		row.Layout = Layout.Row();
		row.Layout.Margin = 0;
		row.Layout.Spacing = 0;

		if ( depth > 0 )
		{
			var spacer = new Widget( row );
			spacer.FixedWidth = depth * 14;
			row.Layout.Add( spacer );
		}

		var btn = new Button( $"{element.Name}  ·  {element.Type}", IconForType( element.Type ), row );
		btn.ToolTip = element.Id;
		if ( isSelected )
			btn.SetStyles( "background-color: #1f5cb8;" );

		var captured = element;
		btn.Clicked += () =>
		{
			_selected = captured;
			ElementSelected?.Invoke( captured );
			Refresh();
		};
		row.Layout.Add( btn, 1 );

		_listLayout.Add( row );

		foreach ( var childId in element.Children )
		{
			if ( byId.TryGetValue( childId, out var child ) )
				AddElementRow( child, byId, depth + 1 );
		}
	}

	private static string IconForType( SuiElementType type ) => type switch
	{
		SuiElementType.Canvas => "crop_free",
		SuiElementType.Panel => "crop_square",
		SuiElementType.Overlay => "layers",
		SuiElementType.Text => "title",
		SuiElementType.Image => "image",
		SuiElementType.Button => "smart_button",
		SuiElementType.HorizontalBox => "view_week",
		SuiElementType.VerticalBox => "view_agenda",
		SuiElementType.Grid => "grid_on",
		SuiElementType.ScrollPanel => "swap_vert",
		SuiElementType.ProgressBar => "linear_scale",
		SuiElementType.InventoryGrid => "grid_view",
		SuiElementType.InventorySlot => "check_box_outline_blank",
		SuiElementType.ItemIcon => "category",
		SuiElementType.Tooltip => "info",
		SuiElementType.Hotbar => "view_carousel",
		_ => "extension",
	};
}
