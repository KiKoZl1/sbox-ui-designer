using System;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Variables panel — left sidebar, below the Hierarchy (PRD 18 § 3.7, § 11 #1).
/// Lists the document's typed UI-local state. The "+ Add Variable" button and the
/// per-row "⋮" menu fire request events; <see cref="SuiDesignerWindow"/> turns
/// those into undoable commands.
///
/// M1-B ships a flat searchable list. Collapsible Group sections (the PRD 18 § 3.7
/// mockup) are a later refinement.
/// </summary>
public class SuiVariablesWidget : Widget
{
	private SuiDocument _document;

	private LineEdit _search;
	private string _filter = "";
	private ScrollArea _scroll;
	private Widget _list;

	public event Action AddVariableRequested;
	public event Action<SuiVariable> EditVariableRequested;
	public event Action<SuiVariable> DeleteVariableRequested;
	public event Action<SuiVariable> DuplicateVariableRequested;

	public SuiVariablesWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Variables";
		Name = "SuiVariables";
		MinimumSize = new Vector2( 200, 140 );
		SetStyles( "background-color: transparent; border: none;" );

		Layout = Layout.Column();
		Layout.Margin = new Sandbox.UI.Margin( 8, 8, 8, 8 );
		Layout.Spacing = 6;

		var addBtn = new Button( "+ Add Variable", this );
		addBtn.FixedHeight = 28;
		addBtn.Clicked = () => AddVariableRequested?.Invoke();
		Layout.Add( addBtn );

		_search = new LineEdit( this );
		_search.PlaceholderText = "Search Variables";
		_search.FixedHeight = 26;
		_search.SetStyles(
			"background-color: rgb(20,20,19);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 3px;" );
		_search.TextEdited += t => { _filter = t ?? ""; Refresh(); };
		Layout.Add( _search );

		_scroll = new ScrollArea( this );
		_list = new Widget( _scroll );
		_list.Layout = Layout.Column();
		_list.Layout.Spacing = 2;
		_scroll.Canvas = _list;
		Layout.Add( _scroll, 1 );

		Refresh();
	}

	public void SetDocument( SuiDocument doc )
	{
		_document = doc;
		Refresh();
	}

	public void Refresh()
	{
		if ( _list == null ) return;
		_list.Layout.Clear( true );

		var vars = _document?.Variables;
		if ( vars == null || vars.Count == 0 )
		{
			var empty = new Label( "No variables yet. Click \"+ Add Variable\".", _list );
			empty.SetStyles( "color: #6b7280; font-size: 11px;" );
			empty.WordWrap = true;
			_list.Layout.Add( empty );
			_list.Layout.AddStretchCell();
			return;
		}

		var filter = _filter?.Trim().ToLowerInvariant() ?? "";
		int shown = 0;
		foreach ( var v in vars )
		{
			if ( v == null ) continue;
			if ( filter.Length > 0
				&& !(v.Name ?? "").ToLowerInvariant().Contains( filter )
				&& !(v.Type ?? "").ToLowerInvariant().Contains( filter ) )
				continue;
			_list.Layout.Add( BuildRow( v ) );
			shown++;
		}

		if ( shown == 0 )
		{
			var none = new Label( "No variables match the search.", _list );
			none.SetStyles( "color: #6b7280; font-size: 11px;" );
			_list.Layout.Add( none );
		}

		_list.Layout.AddStretchCell();
	}

	private Widget BuildRow( SuiVariable v )
	{
		var meta = SuiTypeRegistry.Get( v.Type );

		var row = new Widget( _list );
		row.SetStyles(
			"background-color: rgb(28,28,27);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 4px;" );
		row.Layout = Layout.Row();
		row.Layout.Margin = new Sandbox.UI.Margin( 6, 6, 4, 6 );
		row.Layout.Spacing = 8;
		row.FixedHeight = 46;

		// Type chip — a coloured square with the type's Material icon. Inspired
		// by UMG's pin-cap leader, but our own: icon SHAPE carries the type
		// identity for colour-blind users; colour reinforces it for everyone else.
		var chip = new Button( "", meta.Icon, row );
		chip.FixedWidth = 30;
		chip.FixedHeight = 30;
		chip.SetStyles(
			$"background-color: {meta.Color}1a;" +
			$"border: 1px solid {meta.Color}55;" +
			$"color: {meta.Color};" +
			"border-radius: 4px;" );
		// Non-interactive — just a visual chip. Clicked stays null.
		row.Layout.Add( chip );

		// Info column — name on top, type pill + source below.
		var info = new Widget( row );
		info.Layout = Layout.Column();
		info.Layout.Spacing = 2;

		var nameLbl = new Label( v.Name ?? "(unnamed)", info );
		nameLbl.SetStyles( "color: #f3f4f6; font-size: 12px; font-weight: 600;" );
		info.Layout.Add( nameLbl );

		var tagRow = new Widget( info );
		tagRow.Layout = Layout.Row();
		tagRow.Layout.Spacing = 6;

		var typePill = new Label( meta.DisplayName, tagRow );
		typePill.SetStyles(
			$"background-color: {meta.Color}26;" +
			$"color: {meta.Color};" +
			"font-size: 9px; font-weight: 700;" +
			"border-radius: 3px; padding: 1px 6px;" );
		tagRow.Layout.Add( typePill );

		var srcText = v.Source?.Kind switch
		{
			SuiVariableSourceKind.FromComponent   => "from Component",
			SuiVariableSourceKind.FromActionGraph => "from ActionGraph",
			_                                     => "Manual",
		};
		var srcLbl = new Label( srcText, tagRow );
		srcLbl.SetStyles( "color: #6b7280; font-size: 9px;" );
		tagRow.Layout.Add( srcLbl );
		tagRow.Layout.AddStretchCell();

		info.Layout.Add( tagRow );
		row.Layout.Add( info, 1 );

		var menuBtn = new Button( "", "more_vert", row );
		menuBtn.FixedWidth = 28;
		menuBtn.FixedHeight = 28;
		menuBtn.SetStyles( "background-color: transparent; border: none; color: #6b7280;" );
		menuBtn.Clicked = () => OpenRowMenu( v );
		row.Layout.Add( menuBtn );

		return row;
	}

	private void OpenRowMenu( SuiVariable v )
	{
		var menu = new Menu();
		menu.AddOption( "Edit…", "edit", () => EditVariableRequested?.Invoke( v ) );
		menu.AddOption( "Duplicate", "content_copy", () => DuplicateVariableRequested?.Invoke( v ) );
		menu.AddSeparator();
		menu.AddOption( "Delete", "delete", () => DeleteVariableRequested?.Invoke( v ) );
		menu.OpenAtCursor( false );
	}
}
