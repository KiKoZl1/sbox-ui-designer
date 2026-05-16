using System;
using System.Linq;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Modal "Pick the .sui to reference" — opens when the user drops a Sub-UI
/// element onto the canvas via the palette (PRD 19 § 4.1 Path A). Lists every
/// .sui in the project (from the Asset Registry) and returns the picked
/// document's GUID through <see cref="OnAccept"/>.
///
/// <para>The dragged-from-Asset-Browser path (Path B) is deferred — it needs
/// an engine drag-target hookup that's an M3 polish.</para>
/// </summary>
public sealed class SuiReferencePicker : Window
{
	/// <summary>Invoked with (sourceGuid, displayName) when the user picks a doc. Cancel = not invoked.</summary>
	public Action<string, string> OnAccept;

	/// <summary>Optional document to exclude from the list (the host doc itself — embedding self would cycle instantly).</summary>
	public string ExcludeDocumentId;

	private LineEdit _search;
	private string _filter = "";
	private ScrollArea _scroll;
	private Widget _list;

	public SuiReferencePicker()
	{
		Title = "Pick .sui to reference";
		WindowTitle = Title;
		Size = new Vector2( 520, 480 );
		MinimumSize = new Vector2( 520, 480 );
		SetWindowIcon( "schema" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 12;
		Canvas.Layout.Spacing = 8;

		_search = new LineEdit( Canvas );
		_search.PlaceholderText = "Search .sui";
		_search.FixedHeight = 28;
		_search.SetStyles(
			"background-color: rgb(20,20,19);" +
			"border: 1px solid rgba(255,255,255,0.12);" +
			"border-radius: 3px;" +
			"padding: 0 8px;" +
			"color: #e5e7eb;" );
		_search.TextEdited += s => { _filter = (s ?? "").Trim().ToLowerInvariant(); Rebuild(); };
		Canvas.Layout.Add( _search );

		_scroll = new ScrollArea( Canvas );
		SuiScrollStyle.ApplyTo( _scroll );
		_list = new Widget( _scroll );
		_list.Layout = Layout.Column();
		_list.Layout.Spacing = 2;
		_scroll.Canvas = _list;
		Canvas.Layout.Add( _scroll, 1 );

		var footer = new Widget( Canvas );
		footer.Layout = Layout.Row();
		footer.Layout.Spacing = 8;
		footer.Layout.AddStretchCell();
		var cancelBtn = new Button( "Cancel", footer );
		cancelBtn.Clicked = Close;
		footer.Layout.Add( cancelBtn );
		Canvas.Layout.Add( footer );

		// Ensure the registry knows about every .sui on disk before listing.
		SuiAssetRegistryService.Instance.EnsureInitialized();

		Rebuild();
		Show();
	}

	private void Rebuild()
	{
		if ( _list?.Layout == null ) return;
		_list.Layout.Clear( true );

		var entries = SuiAssetRegistryService.Instance.Registry.Entries
			.Where( kv => string.IsNullOrEmpty( ExcludeDocumentId ) || kv.Key != ExcludeDocumentId )
			.OrderBy( kv => kv.Value.Name ?? kv.Value.Path )
			.ToList();

		if ( entries.Count == 0 )
		{
			var empty = new Label( "No .sui documents found in this project.", _list );
			empty.SetStyles( "color: #6b7280; font-size: 11px;" );
			_list.Layout.Add( empty );
			return;
		}

		int shown = 0;
		foreach ( var kv in entries )
		{
			var name = kv.Value.Name ?? "(unnamed)";
			var path = kv.Value.Path ?? "";
			if ( _filter.Length > 0
				&& !name.ToLowerInvariant().Contains( _filter )
				&& !path.ToLowerInvariant().Contains( _filter ) )
				continue;

			_list.Layout.Add( BuildRow( kv.Key, name, path ) );
			shown++;
		}

		if ( shown == 0 )
		{
			var none = new Label( "No documents match the search.", _list );
			none.SetStyles( "color: #6b7280; font-size: 11px;" );
			_list.Layout.Add( none );
		}

		_list.Layout.AddStretchCell();
	}

	private Widget BuildRow( string guid, string name, string path )
	{
		var row = new Button( "", "", _list );
		row.SetStyles(
			"background-color: rgb(28,28,27);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 4px;" +
			"text-align: left; padding: 8px 10px;" +
			"color: #e5e7eb;" );
		row.FixedHeight = 48;
		row.Text = $"{name}\n{path}";
		row.Icon = "schema";
		row.Clicked = () =>
		{
			OnAccept?.Invoke( guid, name );
			Close();
		};
		return row;
	}
}
