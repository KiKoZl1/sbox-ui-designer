using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// V1.5 M3 (PRD 20 § 4.3) — project-wide Events table. Mirrors the shape of
/// <see cref="SuiBindingsWidget"/>: one row per (Element, Event) pair, with
/// a search box up top. Clicking a row jumps the controller selection to
/// that element so the Details panel scrolls into context.
/// </summary>
public sealed class SuiEventsWidget : Widget
{
	private SuiDocument _document;
	private Widget _listHost;
	private LineEdit _search;
	private string _filter = "";

	/// <summary>Raised when a row's "go to element" affordance fires.</summary>
	public event Action<string> JumpToElementRequested;

	/// <summary>+ Add Event button — Designer opens <see cref="SuiEventPopup"/> in create mode.</summary>
	public event Action AddEventRequested;

	/// <summary>Per-row edit affordance. (elementId, eventName, existingBinding).</summary>
	public event Action<string, string, SuiEventBinding> EditEventRequested;

	/// <summary>Per-row delete affordance. (elementId, eventName).</summary>
	public event Action<string, string> DeleteEventRequested;

	public SuiEventsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Events";
		Name = "SuiEvents";

		Layout = Layout.Column();
		Layout.Margin = 8;
		Layout.Spacing = 6;

		var top = new Widget( this );
		top.Layout = Layout.Row();
		top.Layout.Spacing = 6;
		top.FixedHeight = 28;

		var addBtn = new Button( "+ Add Event", "add", top );
		addBtn.Clicked = () => AddEventRequested?.Invoke();
		top.Layout.Add( addBtn );

		_search = new LineEdit( top );
		_search.PlaceholderText = "Search events…";
		_search.SetStyles(
			"background-color: rgb(20,20,19);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 3px;" );
		_search.TextEdited += s => { _filter = (s ?? "").ToLowerInvariant(); Refresh(); };
		top.Layout.Add( _search, 1 );

		Layout.Add( top );

		var scroll = new ScrollArea( this );
		_listHost = new Widget( scroll );
		_listHost.Layout = Layout.Column();
		_listHost.Layout.Spacing = 2;
		scroll.Canvas = _listHost;
		Layout.Add( scroll, 1 );

		Refresh();
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		Refresh();
	}

	public void Refresh()
	{
		if ( _listHost?.Layout == null ) return;
		_listHost.Layout.Clear( true );

		if ( _document?.Elements == null )
		{
			AddEmpty( "(no document)" );
			return;
		}

		int shown = 0;
		foreach ( var el in _document.Elements )
		{
			if ( el?.Events == null || el.Events.Count == 0 ) continue;
			foreach ( var kv in el.Events )
			{
				if ( kv.Value == null ) continue;
				if ( !Matches( el, kv.Key, kv.Value ) ) continue;
				_listHost.Layout.Add( BuildRow( el, kv.Key, kv.Value ) );
				shown++;
			}
		}

		if ( shown == 0 )
		{
			AddEmpty( string.IsNullOrEmpty( _filter )
				? "No events bound — select an element with events in the canvas to wire one."
				: "No events match your search." );
			return;
		}
		_listHost.Layout.AddStretchCell();
	}

	private bool Matches( SuiElement el, string eventName, SuiEventBinding b )
	{
		if ( string.IsNullOrEmpty( _filter ) ) return true;
		var handler = b.Mode == SuiEventMode.Code ? b.Handler : b.DooPropertyName;
		return (el.Name ?? "").ToLowerInvariant().Contains( _filter )
			|| (eventName ?? "").ToLowerInvariant().Contains( _filter )
			|| (handler ?? "").ToLowerInvariant().Contains( _filter );
	}

	private Widget BuildRow( SuiElement el, string eventName, SuiEventBinding b )
	{
		var row = new Widget( _listHost );
		row.SetStyles(
			"background-color: rgb(28,28,27);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 4px;" );
		row.Layout = Layout.Row();
		row.Layout.Margin = new Sandbox.UI.Margin( 8, 4, 4, 4 );
		row.Layout.Spacing = 6;
		row.FixedHeight = 30;

		var elLbl = new Label( el.Name ?? el.Id, row ) { FixedWidth = 110 };
		elLbl.SetStyles( "color: #e5e7eb; font-size: 11px; font-weight: 600;" );
		row.Layout.Add( elLbl );

		var evLbl = new Label( eventName ?? "?", row ) { FixedWidth = 96 };
		evLbl.SetStyles( "color: #93c5fd; font-size: 11px;" );
		row.Layout.Add( evLbl );

		var arrow = new Label( "→", row );
		arrow.SetStyles( "color: #6b7280; font-size: 11px;" );
		row.Layout.Add( arrow );

		var handlerText = b.Mode == SuiEventMode.Code ? b.Handler
			: b.Mode == SuiEventMode.Doo ? b.DooPropertyName
			: "(not bound)";
		var handlerLbl = new Label( handlerText ?? "(empty)", row );
		handlerLbl.SetStyles( "color: #c4b5fd; font-size: 11px;" );
		row.Layout.Add( handlerLbl, 1 );

		var modeLbl = new Label( b.Mode.ToString(), row ) { FixedWidth = 56 };
		modeLbl.SetStyles( "color: #6b7280; font-size: 10px;" );
		row.Layout.Add( modeLbl );

		// Edit / delete — same pattern + iconography as Bindings tab.
		var editBtn = new Button( "", "edit", row );
		editBtn.FixedWidth = 24;
		editBtn.FixedHeight = 24;
		editBtn.SetStyles( "background-color: transparent; border: none; color: #9ca3af;" );
		editBtn.Clicked = () => EditEventRequested?.Invoke( el.Id, eventName, b );
		row.Layout.Add( editBtn );

		var delBtn = new Button( "", "delete", row );
		delBtn.FixedWidth = 24;
		delBtn.FixedHeight = 24;
		delBtn.SetStyles( "background-color: transparent; border: none; color: #9ca3af;" );
		delBtn.Clicked = () => DeleteEventRequested?.Invoke( el.Id, eventName );
		row.Layout.Add( delBtn );

		// Element name label click jumps selection to the element so the
		// canvas + Details scroll into context. Other clicks (edit, delete)
		// are consumed by their own buttons before they reach the row.
		elLbl.Cursor = CursorShape.Finger;
		elLbl.MouseLeftPress += () => JumpToElementRequested?.Invoke( el.Id );
		return row;
	}

	private void AddEmpty( string text )
	{
		var lbl = new Label( text, _listHost );
		lbl.SetStyles( "color: #71717a; font-size: 11px; padding: 12px;" );
		_listHost.Layout.Add( lbl );
	}
}
