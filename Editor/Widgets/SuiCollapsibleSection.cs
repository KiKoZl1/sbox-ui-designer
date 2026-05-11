using System;
using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// UMG-style collapsible section: header button with a chevron + a content
/// pane below it. Click the header to toggle the body's visibility.
///
/// Rows live inside <see cref="Body"/> — caller does
/// <c>section.Body.Layout.Add( ... )</c> or uses <see cref="Body"/> as the
/// parent for child Widgets.
///
/// Editor.Qt doesn't ship a Foldable / GroupBox widget at the public API
/// surface, so this is a small composite — Button (chevron + title) +
/// content Widget — that hides/shows the content widget on click.
/// </summary>
public class SuiCollapsibleSection : Widget
{
	private bool _expanded;
	private readonly Button _header;
	private readonly Widget _body;

	/// <summary>Container Widget that should host rows / fields. Public so
	/// callers can pass it as the parent of new child Widgets they create.</summary>
	public Widget Body => _body;

	/// <summary>Whether the section is currently expanded.</summary>
	public bool Expanded
	{
		get => _expanded;
		set => SetExpanded( value );
	}

	public string Title { get; }

	public SuiCollapsibleSection( string title, Widget parent, bool defaultExpanded = true ) : base( parent )
	{
		Title = title;

		Layout = Layout.Column();
		Layout.Margin = new Sandbox.UI.Margin( 0, 4, 0, 0 );
		Layout.Spacing = 0;

		// M14 visual: lighter section header — chevron + title with a thin
		// underline accent. No filled background, less padding, smaller text.
		_header = new Button( title.ToUpperInvariant(), "expand_more", this );
		_header.SetStyles(
			"text-align: left; font-weight: 600; font-size: 10px; " +
			"color: #94a3b8; padding: 5px 4px 4px 4px; border: none; " +
			"background-color: transparent; letter-spacing: 1.2px; " +
			"border-bottom: 1px solid rgba(255,255,255,0.06);" );
		_header.Clicked += () => SetExpanded( !_expanded );
		Layout.Add( _header );

		_body = new Widget( this );
		_body.Layout = Layout.Column();
		_body.Layout.Margin = new Sandbox.UI.Margin( 2, 4, 2, 4 );
		_body.Layout.Spacing = 1;
		Layout.Add( _body );

		SetExpanded( defaultExpanded );
	}

	public void SetExpanded( bool expanded )
	{
		_expanded = expanded;
		_body.Visible = expanded;
		// Material icons: expand_more (▾ down) when open; chevron_right (▸) when closed.
		_header.Icon = expanded ? "expand_more" : "chevron_right";
	}
}
