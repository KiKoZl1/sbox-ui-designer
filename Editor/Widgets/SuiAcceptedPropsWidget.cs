using System;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// AcceptedProps panel — sibling of <see cref="SuiVariablesWidget"/>. Shows the
/// document's external contract (PRD 19 § 4.3): typed parameters that parents
/// embedding this <c>.sui</c> via SuiReference can pass in. The generator emits
/// one <c>[Property]</c> per row on the partial class.
///
/// <para>Visually distinguished from Variables with a slot-icon chip (vs the
/// type-icon chip Variables use) and a "prop slot" tag — same row shape so the
/// user gets the parallel intuition. Eventos disparam para o
/// <see cref="SuiDesignerWindow"/> que envelopa em commands undoable.</para>
/// </summary>
public class SuiAcceptedPropsWidget : Widget
{
	private SuiDocument _document;

	private LineEdit _search;
	private string _filter = "";
	private ScrollArea _scroll;
	private Widget _list;

	public event Action AddPropRequested;
	public event Action<SuiAcceptedProp> EditPropRequested;
	public event Action<SuiAcceptedProp> DeletePropRequested;
	public event Action<SuiAcceptedProp> DuplicatePropRequested;

	public SuiAcceptedPropsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "AcceptedProps";
		Name = "SuiAcceptedProps";
		MinimumSize = new Vector2( 200, 140 );
		SetStyles( "background-color: transparent; border: none;" );

		Layout = Layout.Column();
		Layout.Margin = new Sandbox.UI.Margin( 8, 8, 8, 8 );
		Layout.Spacing = 6;

		var addBtn = new Button( "+ Add Prop", this );
		addBtn.FixedHeight = 28;
		addBtn.Clicked = () => AddPropRequested?.Invoke();
		Layout.Add( addBtn );

		_search = new LineEdit( this );
		_search.PlaceholderText = "Search Props";
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

		var props = _document?.AcceptedProps;
		if ( props == null || props.Count == 0 )
		{
			var empty = new Label( "No accepted props yet. Click \"+ Add Prop\".\nProps are the external contract parents pass in via SuiReference.", _list );
			empty.SetStyles( "color: #6b7280; font-size: 11px;" );
			empty.WordWrap = true;
			_list.Layout.Add( empty );
			_list.Layout.AddStretchCell();
			return;
		}

		var filter = _filter?.Trim().ToLowerInvariant() ?? "";
		int shown = 0;
		foreach ( var p in props )
		{
			if ( p == null ) continue;
			if ( filter.Length > 0
				&& !(p.Name ?? "").ToLowerInvariant().Contains( filter )
				&& !(p.Type ?? "").ToLowerInvariant().Contains( filter ) )
				continue;
			_list.Layout.Add( BuildRow( p ) );
			shown++;
		}

		if ( shown == 0 )
		{
			var none = new Label( "No props match the search.", _list );
			none.SetStyles( "color: #6b7280; font-size: 11px;" );
			_list.Layout.Add( none );
		}

		_list.Layout.AddStretchCell();
	}

	private Widget BuildRow( SuiAcceptedProp p )
	{
		var meta = SuiTypeRegistry.Get( p.Type );

		var row = new Widget( _list );
		row.SetStyles(
			"background-color: rgb(28,28,27);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 4px;" );
		row.Layout = Layout.Row();
		row.Layout.Margin = new Sandbox.UI.Margin( 6, 6, 4, 6 );
		row.Layout.Spacing = 8;
		row.FixedHeight = 46;

		// Slot-icon chip — same colour palette as the matching Variable type,
		// but with the "input" icon to visually distinguish from Variables.
		var chip = new Button( "", "input", row );
		chip.FixedWidth = 30;
		chip.FixedHeight = 30;
		chip.SetStyles(
			$"background-color: {meta.Color}1a;" +
			$"border: 1px solid {meta.Color}55;" +
			$"color: {meta.Color};" +
			"border-radius: 4px;" );
		row.Layout.Add( chip );

		var info = new Widget( row );
		info.Layout = Layout.Column();
		info.Layout.Spacing = 2;

		var nameLbl = new Label( p.Name ?? "(unnamed)", info );
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

		var slotLbl = new Label( p.Required ? "required" : "prop slot", tagRow );
		slotLbl.SetStyles( p.Required
			? "color: #fca5a5; font-size: 9px; font-weight: 600;"
			: "color: #6b7280; font-size: 9px;" );
		tagRow.Layout.Add( slotLbl );
		tagRow.Layout.AddStretchCell();

		info.Layout.Add( tagRow );
		row.Layout.Add( info, 1 );

		var menuBtn = new Button( "", "more_vert", row );
		menuBtn.FixedWidth = 28;
		menuBtn.FixedHeight = 28;
		menuBtn.SetStyles( "background-color: transparent; border: none; color: #6b7280;" );
		menuBtn.Clicked = () => OpenRowMenu( p );
		row.Layout.Add( menuBtn );

		return row;
	}

	private void OpenRowMenu( SuiAcceptedProp p )
	{
		var menu = new Menu();
		menu.AddOption( "Edit…", "edit", () => EditPropRequested?.Invoke( p ) );
		menu.AddOption( "Duplicate", "content_copy", () => DuplicatePropRequested?.Invoke( p ) );
		menu.AddSeparator();
		menu.AddOption( "Delete", "delete", () => DeletePropRequested?.Invoke( p ) );
		menu.OpenAtCursor( false );
	}
}
