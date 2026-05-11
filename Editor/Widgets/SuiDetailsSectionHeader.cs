using System;
using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Collapsible section header in the Details panel. Chevron on the left,
/// uppercase label centered, subtle dark fill so it reads as a band but
/// doesn't compete with the panel chrome.
/// </summary>
public sealed class SuiDetailsSectionHeader : Widget
{
	public string Title { get; }
	public bool IsExpanded = true;
	public event Action ToggledExpanded;

	private bool _hover;

	public SuiDetailsSectionHeader( string title, Widget parent = null ) : base( parent )
	{
		Title = title ?? "";
		FixedHeight = 24;
		Cursor = CursorShape.Finger;
		SetStyles( "background-color: transparent; border: none;" );
	}

	protected override void OnPaint()
	{
		var rect = LocalRect;

		// Subtle band background (#272726 — slightly lighter than the dock body
		// #212120 to read as a header). Hover bumps it a touch.
		var bg = _hover
			? new Color( 50 / 255f, 50 / 255f, 49 / 255f )
			: new Color( 39 / 255f, 39 / 255f, 38 / 255f );
		Paint.SetBrushAndPen( bg );
		Paint.DrawRect( rect, 3f );

		// Chevron at left — same shape used in Palette / Hierarchy headers.
		Paint.SetPen( new Color( 165 / 255f, 172 / 255f, 182 / 255f ), 2f );
		float cy = Height / 2f;
		if ( IsExpanded )
		{
			// ▾ down
			Paint.DrawLine( new Vector2( 6, cy - 2 ), new Vector2( 10, cy + 2 ) );
			Paint.DrawLine( new Vector2( 10, cy + 2 ), new Vector2( 14, cy - 2 ) );
		}
		else
		{
			// ▸ right
			Paint.DrawLine( new Vector2( 7, cy - 4 ), new Vector2( 11, cy ) );
			Paint.DrawLine( new Vector2( 11, cy ), new Vector2( 7, cy + 4 ) );
		}

		// Title — left-aligned just past the chevron, small caps + bold.
		Paint.SetFont( null, 9, 700 );
		Paint.SetPen( new Color( 200 / 255f, 204 / 255f, 210 / 255f ) );
		Paint.DrawText( new Rect( 22, 0, Width - 24, Height ), Title, TextFlag.LeftCenter );
	}

	protected override void OnMouseEnter() { _hover = true; Update(); }
	protected override void OnMouseLeave() { _hover = false; Update(); }
	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton ) ToggledExpanded?.Invoke();
	}
}

/// <summary>
/// Sub-header inside an open section (e.g. "Notes" or a per-prop subgroup).
/// Visually quieter than a full section header — small uppercase tracking,
/// no background, just a row of text.
/// </summary>
public sealed class SuiDetailsSubHeader : Widget
{
	public string Title { get; }

	public SuiDetailsSubHeader( string title, Widget parent = null ) : base( parent )
	{
		Title = title?.ToUpperInvariant() ?? "";
		FixedHeight = 22;
		SetStyles( "background-color: transparent; border: none;" );
	}

	protected override void OnPaint()
	{
		Paint.SetDefaultFont( 10 );
		Paint.SetPen( new Color( 140 / 255f, 145 / 255f, 153 / 255f ) );
		// Small horizontal padding so it lines up with section header text.
		Paint.DrawText( new Rect( 4, 6, Width - 8, Height - 6 ), Title, TextFlag.LeftCenter );
	}
}

/// <summary>
/// A complete collapsible section: header + a content container that toggles
/// visibility when the header is clicked. Add rows to <see cref="Body"/>.
/// </summary>
public sealed class SuiDetailsSection : Widget
{
	public SuiDetailsSectionHeader Header { get; }
	public Widget Body { get; }

	public SuiDetailsSection( string title, Widget parent = null, bool expanded = true ) : base( parent )
	{
		SetStyles( "background-color: transparent; border: none;" );
		Layout = Layout.Column();
		Layout.Margin = new Sandbox.UI.Margin( 0, 2, 0, 2 );
		Layout.Spacing = 0;

		Header = new SuiDetailsSectionHeader( title, this );
		Header.IsExpanded = expanded;
		Header.ToggledExpanded += () =>
		{
			Header.IsExpanded = !Header.IsExpanded;
			Header.Update();
			if ( Body.IsValid() ) Body.Visible = Header.IsExpanded;
		};
		Layout.Add( Header );

		Body = new Widget( this );
		Body.SetStyles( "background-color: transparent; border: none;" );
		Body.Layout = Layout.Column();
		// Pure Layout.Margin path — eliminate ContentMargins (was conflicting).
		// Right=16 = gap from scrollbar; vertical=4 = breathing room above/below
		// the rows.
		Body.Layout.Margin = new Sandbox.UI.Margin( 0, 4, 16, 4 );
		Body.Layout.Spacing = 0;
		Body.Visible = expanded;
		Layout.Add( Body );
	}
}
