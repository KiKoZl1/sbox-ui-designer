using System;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Bindings panel — a document-wide table of every property binding (PRD 18 § 4.8).
/// V1.5 model uses <see cref="SuiBinding"/> on each element. The legacy V1.0
/// <c>SuiPropertyBinding</c> table on the document was removed pre-M3 (it was
/// stub code that never wired into codegen). Each row: Element · Property ←
/// Source Variable · Mode, with edit / delete buttons.
///
/// The "+ Add Binding" button and the per-row edit/delete fire request events;
/// <see cref="SuiDesignerWindow"/> turns them into undoable commands via the
/// <see cref="SuiBindPopup"/>.
/// </summary>
public sealed class SuiBindingsWidget : Widget
{
	private SuiDocument _document;
	private Widget _listHost;
	private LineEdit _search;
	private string _filter = "";

	public event Action AddBindingRequested;
	public event Action<string, SuiBinding> EditBindingRequested;
	public event Action<string, SuiBinding> DeleteBindingRequested;

	public SuiBindingsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Bindings";
		Name = "SuiBindings";

		Layout = Layout.Column();
		Layout.Margin = 8;
		Layout.Spacing = 6;

		var top = new Widget( this );
		top.Layout = Layout.Row();
		top.Layout.Spacing = 6;
		top.FixedHeight = 28;

		var addBtn = new Button( "+ Add Binding", "add", top );
		addBtn.Clicked = () => AddBindingRequested?.Invoke();
		top.Layout.Add( addBtn );

		_search = new LineEdit( top );
		_search.PlaceholderText = "Search bindings…";
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
			if ( el?.Bindings == null ) continue;
			foreach ( var b in el.Bindings )
			{
				if ( b == null || !Matches( el, b ) ) continue;
				_listHost.Layout.Add( BuildRow( el, b ) );
				shown++;
			}
		}

		if ( shown == 0 )
		{
			AddEmpty( string.IsNullOrEmpty( _filter )
				? "No bindings yet — click \"+ Add Binding\"."
				: "No bindings match your search." );
			return;
		}
		_listHost.Layout.AddStretchCell();
	}

	private bool Matches( SuiElement el, SuiBinding b )
	{
		if ( string.IsNullOrEmpty( _filter ) ) return true;
		return (el.Name ?? "").ToLowerInvariant().Contains( _filter )
			|| (b.Property ?? "").ToLowerInvariant().Contains( _filter )
			|| SourceName( b ).ToLowerInvariant().Contains( _filter );
	}

	private string SourceName( SuiBinding b )
	{
		var id = b?.Source?.VariableId;
		if ( string.IsNullOrEmpty( id ) || _document?.Variables == null ) return "(unset)";
		foreach ( var v in _document.Variables )
			if ( v?.Id == id ) return v.Name ?? "(unnamed)";
		return "(missing)";
	}

	private Widget BuildRow( SuiElement el, SuiBinding b )
	{
		// Broken-binding detection (#9). A row is broken when the source
		// Variable id no longer resolves OR a converter step references a
		// converter that doesn't exist in the catalog.
		var brokenReason = DetectBroken( b );
		var isBroken = !string.IsNullOrEmpty( brokenReason );

		var row = new Widget( _listHost );
		row.SetStyles( isBroken
			? "background-color: rgba(239,68,68,0.08); border: 1px solid rgba(239,68,68,0.55); border-radius: 4px;"
			: "background-color: rgb(28,28,27); border: 1px solid rgba(255,255,255,0.06); border-radius: 4px;" );
		row.Layout = Layout.Row();
		row.Layout.Margin = new Sandbox.UI.Margin( 8, 4, 4, 4 );
		row.Layout.Spacing = 6;
		row.FixedHeight = 30;

		if ( isBroken )
		{
			// Leading warning icon — colored red so a row stands out at a glance.
			var warn = new Label( "⚠", row ) { FixedWidth = 16 };
			warn.SetStyles( "color: #ef4444; font-size: 14px; font-weight: 800;" );
			warn.ToolTip = brokenReason;
			row.Layout.Add( warn );
		}

		var elLbl = new Label( el.Name ?? el.Id, row ) { FixedWidth = isBroken ? 76 : 92 };
		elLbl.SetStyles( "color: #e5e7eb; font-size: 11px; font-weight: 600;" );
		if ( isBroken ) elLbl.ToolTip = brokenReason;
		row.Layout.Add( elLbl );

		var propLbl = new Label( b.Property ?? "?", row ) { FixedWidth = 84 };
		propLbl.SetStyles( "color: #93c5fd; font-size: 11px;" );
		row.Layout.Add( propLbl );

		var arrow = new Label( "<-", row );
		arrow.SetStyles( "color: #6b7280; font-size: 11px;" );
		row.Layout.Add( arrow );

		var srcLbl = new Label( SourceName( b ), row );
		srcLbl.SetStyles( isBroken
			? "color: #ef4444; font-size: 11px; font-weight: 600;"
			: "color: #c4b5fd; font-size: 11px;" );
		if ( isBroken ) srcLbl.ToolTip = brokenReason;
		row.Layout.Add( srcLbl, 1 );

		var modeLbl = new Label( b.Mode.ToString(), row ) { FixedWidth = 64 };
		modeLbl.SetStyles( "color: #6b7280; font-size: 10px;" );
		row.Layout.Add( modeLbl );

		var editBtn = new Button( "", "edit", row );
		editBtn.FixedWidth = 24;
		editBtn.FixedHeight = 24;
		editBtn.SetStyles( "background-color: transparent; border: none; color: #9ca3af;" );
		editBtn.Clicked = () => EditBindingRequested?.Invoke( el.Id, b );
		row.Layout.Add( editBtn );

		var delBtn = new Button( "", "delete", row );
		delBtn.FixedWidth = 24;
		delBtn.FixedHeight = 24;
		delBtn.SetStyles( "background-color: transparent; border: none; color: #9ca3af;" );
		delBtn.Clicked = () => DeleteBindingRequested?.Invoke( el.Id, b );
		row.Layout.Add( delBtn );

		return row;
	}

	/// <summary>
	/// Return a human-readable reason if the binding references something that
	/// no longer exists (Variable deleted, converter ref missing). Empty string
	/// means the binding is healthy.
	/// </summary>
	private string DetectBroken( SuiBinding b )
	{
		if ( b == null ) return "binding is null";

		// Source variable id must still resolve.
		var sourceId = b.Source?.VariableId;
		if ( string.IsNullOrEmpty( sourceId ) )
			return "Binding has no source Variable.";

		bool found = false;
		if ( _document?.Variables != null )
		{
			foreach ( var v in _document.Variables )
				if ( v?.Id == sourceId ) { found = true; break; }
		}
		if ( !found )
			return $"Source Variable '{sourceId}' was deleted or renamed (id missing from this document).";

		// Every converter ref in the chain must resolve to a real converter.
		if ( b.Converters != null )
		{
			for ( int i = 0; i < b.Converters.Count; i++ )
			{
				var step = b.Converters[i];
				if ( step == null || string.IsNullOrEmpty( step.ConverterRef ) )
					return $"Converter step #{i + 1} has no ConverterRef.";
				if ( SuiConverterCatalog.Find( step.ConverterRef ) == null )
					return $"Converter step #{i + 1} references unknown converter '{step.ConverterRef}'.";
			}
		}

		return null;
	}

	private void AddEmpty( string msg )
	{
		var none = new Label( msg, _listHost );
		none.SetStyles( "color: #6b7280; font-size: 11px; padding: 8px;" );
		_listHost.Layout.Add( none );
		_listHost.Layout.AddStretchCell();
	}
}
