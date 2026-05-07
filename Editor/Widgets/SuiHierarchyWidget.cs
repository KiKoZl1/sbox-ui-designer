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
		var indentPx = 6 + depth * 12;
		var isSelected = _selected != null && _selected.Id == element.Id;

		var btn = new Button( $"{element.Name}  ·  {element.Type}", "", _scrollHost );
		btn.ToolTip = element.Id;
		btn.SetStyles( $"text-align: left; padding-left: {indentPx}px; {(isSelected ? "background-color: #1f5cb8;" : "")}" );

		var captured = element;
		btn.Clicked += () =>
		{
			_selected = captured;
			ElementSelected?.Invoke( captured );
			Refresh();
		};
		_listLayout.Add( btn );

		foreach ( var childId in element.Children )
		{
			if ( byId.TryGetValue( childId, out var child ) )
				AddElementRow( child, byId, depth + 1 );
		}
	}
}
