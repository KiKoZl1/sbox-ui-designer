using System;
using System.Collections.Generic;
using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Custom tab strip — pure widget, paint-only. Designer/Preview/Code style:
/// active tab has a solid blue fill (#0F3F79 — user spec), inactive tabs
/// are flat with grey text. Subtle vertical separators between tabs match
/// the top-bar look.
///
/// Background: #141414 (canvas dark area).
/// </summary>
public sealed class SuiTabStrip : Widget
{
	private readonly List<SuiTabStripItem> _tabs = new();
	private int _active = 0;

	public event Action<int> ActiveTabChanged;

	public int ActiveIndex
	{
		get => _active;
		set
		{
			if ( _active == value ) return;
			_active = value;
			foreach ( var t in _tabs ) t.Update();
			ActiveTabChanged?.Invoke( _active );
		}
	}

	public SuiTabStrip( Widget parent = null ) : base( parent )
	{
		FixedHeight = 36;

		Layout = Layout.Row();
		Layout.Margin = new Sandbox.UI.Margin( 0, 0, 0, 0 );
		Layout.Spacing = 0;
	}

	public void AddTab( string label, string icon = null )
	{
		var idx = _tabs.Count;

		// Insert vertical separator between tabs (not before first).
		if ( _tabs.Count > 0 )
		{
			var sep = new SuiTabStripSeparator();
			Layout.Add( sep );
		}

		var tab = new SuiTabStripItem( this, idx, label, icon );
		_tabs.Add( tab );
		Layout.Add( tab );
	}

	public void AddStretch() => Layout.AddStretchCell();

	/// <summary>Push remaining items to the right by inserting a stretch
	/// cell after the last tab. Called automatically by parent so tabs
	/// stay anchored on the left and don't expand to fill the row.</summary>
	public void FinishLeftAligned() => Layout.AddStretchCell();

	internal bool IsActive( int idx ) => idx == _active;

	internal void OnTabClicked( int idx )
	{
		ActiveIndex = idx;
	}

	protected override void OnPaint()
	{
		// Background: #141414 — canvas dark area.
		Paint.SetBrushAndPen( new Color( 20 / 255f, 20 / 255f, 20 / 255f ) );
		Paint.DrawRect( LocalRect );
	}
}

internal sealed class SuiTabStripItem : Widget
{
	private readonly SuiTabStrip _strip;
	private readonly int _index;
	public string Label;
	public string Icon;

	private bool _hover;
	private bool _sized;

	private const int FontSize = 11;
	private const int PadH = 14;
	private const int IconSize = 12;
	private const int IconGap = 6;

	public SuiTabStripItem( SuiTabStrip strip, int index, string label, string icon ) : base( strip )
	{
		_strip = strip;
		_index = index;
		Label = label ?? "";
		Icon = icon;
		Cursor = CursorShape.Finger;
		FixedHeight = 36;

		// Conservative initial width.
		var iconBlock = string.IsNullOrEmpty( icon ) ? 0 : IconSize + IconGap;
		FixedWidth = PadH + iconBlock + (Label.Length * 10) + PadH;
	}

	protected override void OnPaint()
	{
		Paint.SetDefaultFont( FontSize );

		if ( !_sized )
		{
			var labelW = string.IsNullOrEmpty( Label ) ? 0 : Paint.MeasureText( Label ).x;
			var iconBlock = string.IsNullOrEmpty( Icon ) ? 0 : IconSize + IconGap;
			int newW = (int)(PadH + iconBlock + labelW + 2 + PadH);
			_sized = true;
			if ( newW != FixedWidth )
			{
				FixedWidth = newW;
				return;
			}
		}

		var rect = LocalRect;
		bool active = _strip.IsActive( _index );

		// Active tab: solid blue #0F3F79.
		if ( active )
		{
			Paint.SetBrushAndPen( new Color( 15 / 255f, 63 / 255f, 121 / 255f ) );
			Paint.DrawRect( rect );
		}
		else if ( _hover )
		{
			Paint.SetBrushAndPen( Color.White.WithAlpha( 0.04f ) );
			Paint.DrawRect( rect );
		}

		// Text + icon.
		var textColor = active
			? new Color( 240 / 255f, 244 / 255f, 250 / 255f )
			: new Color( 165 / 255f, 170 / 255f, 178 / 255f );
		Paint.SetPen( textColor );

		float x = PadH;
		if ( !string.IsNullOrEmpty( Icon ) )
		{
			var iconRect = new Rect( x, (Height - IconSize) / 2f, IconSize, IconSize );
			Paint.DrawIcon( iconRect, Icon, IconSize );
			x += IconSize + IconGap;
		}

		if ( !string.IsNullOrEmpty( Label ) )
		{
			var labelW = Paint.MeasureText( Label ).x;
			var labelRect = new Rect( x, 0, labelW + 2, Height );
			Paint.DrawText( labelRect, Label, TextFlag.LeftCenter );
		}
	}

	protected override void OnMouseEnter() { _hover = true; Update(); }
	protected override void OnMouseLeave() { _hover = false; Update(); }
	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton ) _strip.OnTabClicked( _index );
	}
}

/// <summary>
/// Subtle 1px vertical line between tab items — same look as the top-bar
/// group separators (rgba(255,255,255,0.08), 55% of widget height, centered).
/// </summary>
internal sealed class SuiTabStripSeparator : Widget
{
	public SuiTabStripSeparator( Widget parent = null ) : base( parent )
	{
		FixedWidth = 11;
		FixedHeight = 36;
	}

	protected override void OnPaint()
	{
		float lineHeight = Height * 0.55f;
		float yTop = (Height - lineHeight) / 2f;
		float xCenter = Width / 2f;

		Paint.SetPen( Color.White.WithAlpha( 0.08f ) );
		Paint.DrawLine( new Vector2( xCenter, yTop ), new Vector2( xCenter, yTop + lineHeight ) );
	}
}
