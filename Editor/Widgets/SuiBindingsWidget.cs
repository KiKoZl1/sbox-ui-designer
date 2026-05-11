using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Bindings tab — minimal table of property bindings for the current document
/// (mockup Image 1). Each row: Target | Property | Source | Path | Mode + delete.
/// Add Binding button at the top.
///
/// V1 persists bindings in the .sui (<c>SuiDocument.Bindings</c>) but does
/// NOT emit them to the generated Razor yet. V2 will translate to
/// <c>@bind-X</c> + <c>[Property]</c> code.
/// </summary>
public sealed class SuiBindingsWidget : Widget
{
	private SuiDocument _document;
	private Widget _listHost;
	private LineEdit _searchInput;
	private string _filter = "";

	public SuiBindingsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Bindings";
		Name = "SuiBindings";

		Layout = Layout.Column();
		Layout.Margin = 8;
		Layout.Spacing = 6;

		// Top toolbar — Add Binding + Search.
		var top = new Widget( this );
		top.Layout = Layout.Row();
		top.Layout.Spacing = 6;
		top.FixedHeight = 28;

		var addBtn = new Button( "Add Binding", "add", top );
		addBtn.Clicked += AddBinding;
		top.Layout.Add( addBtn );

		_searchInput = new LineEdit( top );
		_searchInput.PlaceholderText = "Search bindings…";
		_searchInput.TextEdited += s => { _filter = (s ?? "").ToLowerInvariant(); RefreshList(); };
		top.Layout.Add( _searchInput, 1 );

		Layout.Add( top );

		// Header row.
		var header = new Widget( this );
		header.Layout = Layout.Row();
		header.Layout.Spacing = 6;
		header.FixedHeight = 22;
		AddHeaderCell( header, "Target", 1 );
		AddHeaderCell( header, "Property", 1 );
		AddHeaderCell( header, "Binding Source", 1 );
		AddHeaderCell( header, "Path", 1 );
		AddHeaderCell( header, "Mode", 0, 70 );
		var delHdr = new Label( "", header );
		delHdr.FixedWidth = 24;
		header.Layout.Add( delHdr );
		Layout.Add( header );

		// Scrollable list of binding rows.
		var scroll = new ScrollArea( this );
		scroll.Canvas = new Widget( null );
		scroll.Canvas.Layout = Layout.Column();
		scroll.Canvas.Layout.Margin = 0;
		scroll.Canvas.Layout.Spacing = 2;
		_listHost = scroll.Canvas;
		Layout.Add( scroll, 1 );
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		RefreshList();
	}

	private void AddBinding()
	{
		if ( _document == null ) return;
		_document.Bindings.Add( new SuiPropertyBinding
		{
			TargetElementId = _document.GetRoot()?.Id,
			Property = "Value",
			Source = "Player",
			Path = "Health",
			Mode = "OneWay",
		} );
		RefreshList();
	}

	private void RefreshList()
	{
		if ( _listHost?.Layout == null ) return;
		_listHost.Layout.Clear( true );
		if ( _document == null )
		{
			var none = new Label( "(no document)", _listHost );
			none.SetStyles( "color: #6b7280; font-size: 11px;" );
			_listHost.Layout.Add( none );
			return;
		}

		var any = false;
		foreach ( var b in _document.Bindings )
		{
			if ( !MatchesFilter( b ) ) continue;
			any = true;
			BuildRow( b );
		}
		if ( !any )
		{
			var none = new Label( string.IsNullOrEmpty( _filter )
				? "No bindings yet — click 'Add Binding' to create one."
				: "No bindings match your search.", _listHost );
			none.SetStyles( "color: #6b7280; font-size: 11px; padding: 8px;" );
			_listHost.Layout.Add( none );
		}
		_listHost.Layout.AddStretchCell();
	}

	private bool MatchesFilter( SuiPropertyBinding b )
	{
		if ( string.IsNullOrEmpty( _filter ) ) return true;
		var elName = ResolveTargetName( b.TargetElementId )?.ToLowerInvariant() ?? "";
		return elName.Contains( _filter )
			|| (b.Property?.ToLowerInvariant() ?? "").Contains( _filter )
			|| (b.Source?.ToLowerInvariant() ?? "").Contains( _filter )
			|| (b.Path?.ToLowerInvariant() ?? "").Contains( _filter );
	}

	private string ResolveTargetName( string elementId )
	{
		if ( _document == null || string.IsNullOrEmpty( elementId ) ) return null;
		var el = _document.GetElement( elementId );
		return el?.Name;
	}

	private void BuildRow( SuiPropertyBinding b )
	{
		var row = new Widget( _listHost );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 6;
		row.FixedHeight = 26;

		// Target — dropdown of elements in the document.
		var targetBtn = new Button( ResolveTargetName( b.TargetElementId ) ?? "(unset)", "category", row );
		targetBtn.Clicked += () =>
		{
			var menu = new Menu( targetBtn );
			foreach ( var el in _document.Elements )
			{
				if ( string.IsNullOrEmpty( el.ParentId ) ) continue;
				var captured = el;
				menu.AddOption( captured.Name, "category", () => { b.TargetElementId = captured.Id; RefreshList(); } );
			}
			menu.OpenAtCursor( true );
		};
		row.Layout.Add( targetBtn, 1 );

		// Property — text input.
		var propEdit = new LineEdit( row );
		propEdit.Text = b.Property ?? "";
		propEdit.PlaceholderText = "Value";
		propEdit.EditingFinished += () => b.Property = propEdit.Text;
		row.Layout.Add( propEdit, 1 );

		// Source — text input.
		var srcEdit = new LineEdit( row );
		srcEdit.Text = b.Source ?? "";
		srcEdit.PlaceholderText = "Player";
		srcEdit.EditingFinished += () => b.Source = srcEdit.Text;
		row.Layout.Add( srcEdit, 1 );

		// Path — text input.
		var pathEdit = new LineEdit( row );
		pathEdit.Text = b.Path ?? "";
		pathEdit.PlaceholderText = "Health";
		pathEdit.EditingFinished += () => b.Path = pathEdit.Text;
		row.Layout.Add( pathEdit, 1 );

		// Mode dropdown.
		var modeBtn = new Button( b.Mode ?? "OneWay", "swap_horiz", row );
		modeBtn.FixedWidth = 70;
		modeBtn.Clicked += () =>
		{
			var menu = new Menu( modeBtn );
			void Add( string m ) => menu.AddOption( m, "swap_horiz", () => { b.Mode = m; modeBtn.Text = m; } );
			Add( "OneWay" );
			Add( "TwoWay" );
			Add( "OneTime" );
			menu.OpenAtCursor( true );
		};
		row.Layout.Add( modeBtn );

		// Delete.
		var delBtn = new Button( "", "delete", row );
		delBtn.FixedWidth = 24;
		delBtn.ToolTip = "Delete binding";
		delBtn.Clicked += () => { _document.Bindings.Remove( b ); RefreshList(); };
		row.Layout.Add( delBtn );

		_listHost.Layout.Add( row );
	}

	private static void AddHeaderCell( Widget row, string text, int stretch, int fixedWidth = 0 )
	{
		var lbl = new Label( text, row );
		lbl.SetStyles( "color: #9ca3af; font-size: 10px; font-weight: 600; letter-spacing: 0.5px;" );
		if ( fixedWidth > 0 ) lbl.FixedWidth = fixedWidth;
		row.Layout.Add( lbl, stretch );
	}
}
