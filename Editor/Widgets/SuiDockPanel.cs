using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Visual imitation of a docked panel with a real Qt-style tab title.
///
/// Shape:
///   ┌─ 1px top gap (root bg shows here) ──────────────────────────┐
///   │ ╔════════════╗                                               │
///   │ ║ ⛏ Palette × ║   (rest of tab-row is transparent: root bg)  │
///   │ ╚════════════╝────────────────────────────────────────────── │
///   │ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │
///   │ ▓ Body fill #212120, content goes here, bottom corners      │
///   │ ▓ rounded, top-left corner is square (covered by tab).      │
///   │ ▓                                                            │
///   └─────────────────────────────────────────────────────────────┘
///
/// The tab and body share the same fill color; only the bottom border of the
/// tab is a 2px blue line (#0F3F79) — same accent used on the active editor
/// tab (Designer / Preview / Code).
/// </summary>
public sealed class SuiDockPanel : Widget
{
	public string Title { get; private set; }
	public string Icon { get; private set; }

	private Widget _tabRow;
	private SuiDockTab _tab;
	private Widget _body;
	private Widget _content;

	public SuiDockPanel( string title, string icon, Widget content, Widget parent = null ) : base( parent )
	{
		Title = title;
		Icon = icon;

		// Outer container is transparent — root bg shows through gaps.
		// 1px top margin = the gap above the tab the user asked for.
		SetStyles( "background-color: transparent; border: none;" );
		Layout = Layout.Column();
		Layout.Margin = new Sandbox.UI.Margin( 0, 1, 0, 0 );
		Layout.Spacing = 0;

		// Tab row — transparent, holds the tab on the left and a stretch
		// cell that lets the root bg show on the right.
		_tabRow = new Widget( this );
		_tabRow.SetStyles( "background-color: transparent; border: none;" );
		_tabRow.Layout = Layout.Row();
		_tabRow.Layout.Margin = 0;
		_tabRow.Layout.Spacing = 0;
		_tabRow.FixedHeight = 35;

		_tab = new SuiDockTab( title, icon );
		_tab.Parent = _tabRow;
		_tabRow.Layout.Add( _tab );
		_tabRow.Layout.AddStretchCell();

		Layout.Add( _tabRow );

		// Body — same fill as the tab so they read as one continuous surface.
		// Top-left corner is square (covered by tab); the other three corners
		// are rounded.
		_body = new Widget( this );
		_body.SetStyles(
			"background-color: rgb(33,33,32);" +
			"border: none;" +
			"border-top-left-radius: 0;" +
			"border-top-right-radius: 4px;" +
			"border-bottom-left-radius: 4px;" +
			"border-bottom-right-radius: 4px;" );
		_body.Layout = Layout.Column();
		_body.Layout.Margin = 0;
		_body.Layout.Spacing = 0;

		if ( content != null )
		{
			_content = content;
			content.Parent = _body;
			_body.Layout.Add( content, 1 );
		}

		Layout.Add( _body, 1 );
	}

	public Widget Content => _content;
}

/// <summary>
/// The tab pill itself — fill matches the body, rounded top corners, square
/// bottom corners, and a 2px blue underline at the bottom (active accent).
/// </summary>
internal sealed class SuiDockTab : Widget
{
	public string Title;
	public string Icon;
	private bool _sized;

	public SuiDockTab( string title, string icon, Widget parent = null ) : base( parent )
	{
		Title = title ?? "";
		Icon = icon;
		FixedHeight = 35;
		// Initial conservative width — recalibrated on first paint via MeasureText.
		var iconW = string.IsNullOrEmpty( icon ) ? 0 : 20;
		FixedWidth = 12 + iconW + (Title.Length * 8) + 8 + 22;

		SetStyles(
			"background-color: rgb(33,33,32);" +
			"border: none;" +
			"border-top-left-radius: 4px;" +
			"border-top-right-radius: 4px;" +
			"border-bottom-left-radius: 0;" +
			"border-bottom-right-radius: 0;" );
	}

	protected override void OnPaint()
	{
		Paint.SetDefaultFont( 11 );

		// Calibrate width to actual text on first paint.
		if ( !_sized )
		{
			var titleW0 = string.IsNullOrEmpty( Title ) ? 0 : Paint.MeasureText( Title ).x;
			var iconW = string.IsNullOrEmpty( Icon ) ? 0 : 20;
			int newW = (int)(12 + iconW + titleW0 + 8 + 22);
			_sized = true;
			if ( newW != FixedWidth )
			{
				FixedWidth = newW;
				return;
			}
		}

		// Active accent — 2px blue line at the bottom of the tab.
		Paint.SetBrushAndPen( new Color( 15 / 255f, 63 / 255f, 121 / 255f ) );
		Paint.DrawRect( new Rect( 0, Height - 2, Width, 2 ) );

		// Icon + title + × close.
		Paint.SetPen( new Color( 220 / 255f, 224 / 255f, 230 / 255f ) );

		float x = 12f;
		if ( !string.IsNullOrEmpty( Icon ) )
		{
			Paint.DrawIcon( new Rect( x, (Height - 14) / 2f, 14, 14 ), Icon, 14 );
			x += 20f;
		}

		float titleW = 0;
		if ( !string.IsNullOrEmpty( Title ) )
		{
			titleW = Paint.MeasureText( Title ).x;
			Paint.DrawText( new Rect( x, 0, titleW + 2, Height ), Title, TextFlag.LeftCenter );
			x += titleW + 8;
		}

		// × close icon — visual only for now (no click handler wired).
		Paint.SetPen( new Color( 150 / 255f, 156 / 255f, 165 / 255f ) );
		Paint.DrawIcon( new Rect( x, (Height - 14) / 2f, 14, 14 ), "close", 14 );
	}
}
