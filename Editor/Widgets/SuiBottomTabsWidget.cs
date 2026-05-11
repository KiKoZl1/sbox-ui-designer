using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Generation;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Bottom panel with 4 tabs (Animations / Bindings / Compile Results / Logs).
/// 100% custom paint — left-aligned tabs above a content area, no Editor.TabWidget.
///
/// Per user spec:
/// - panel background:        #141413 (rgba 20, 20, 19)
/// - inactive tab fill:       same as panel bg (no fill)
/// - active tab fill:         #212120 (rgba 33, 33, 32)
/// - active tab top stripe:   #103354 (rgba 16, 51, 84) — 3px line at top
///
/// Lives inside the center column (between the canvas tabs and the right edge
/// of the canvas) — sidebars on the left/right go full height down to the
/// window bottom.
/// </summary>
public sealed class SuiBottomTabsWidget : Widget
{
	private SuiBottomTabStrip _tabs;
	private Widget _stack;

	private SuiAnimationsWidget _animations;
	private SuiBindingsWidget _bindings;
	private SuiCompileResultsWidget _compileResults;
	private SuiLogsWidget _logs;

	public SuiAnimationsWidget Animations => _animations;
	public SuiBindingsWidget Bindings => _bindings;
	public SuiCompileResultsWidget CompileResults => _compileResults;
	public SuiLogsWidget Logs => _logs;

	public SuiBottomTabsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Bottom Panel";
		Name = "SuiBottomTabs";
		MinimumSize = new Vector2( 400, 160 );

		// Panel background — #141413 per user spec.
		SetStyles( "background-color: rgb(20,20,19); border: none;" );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		_tabs = new SuiBottomTabStrip( this );
		_tabs.AddTab( "Animations" );
		_tabs.AddTab( "Bindings" );
		_tabs.AddTab( "Compile Results" );
		_tabs.AddTab( "Logs" );
		_tabs.FinishLeftAligned();
		_tabs.ActiveTabChanged += OnTabChanged;
		Layout.Add( _tabs );

		// Content stack — single page visible at a time.
		_stack = new Widget( this );
		_stack.SetStyles( "background-color: rgb(33,33,32); border: none;" );
		_stack.Layout = Layout.Row();
		_stack.Layout.Margin = 0;
		Layout.Add( _stack, 1 );

		_animations = new SuiAnimationsWidget( _stack );
		_bindings = new SuiBindingsWidget( _stack );
		_compileResults = new SuiCompileResultsWidget( _stack );
		_logs = new SuiLogsWidget( _stack );

		// Make all four children fill the stack so the panel height stays
		// constant when switching tabs. Without these the stack collapses
		// to the natural size of whichever child is visible.
		foreach ( var w in new Widget[] { _animations, _bindings, _compileResults, _logs } )
		{
			w.SetSizeMode( SizeMode.CanGrow, SizeMode.CanGrow );
		}

		_stack.Layout.Add( _animations, 1 );
		_stack.Layout.Add( _bindings, 1 );
		_stack.Layout.Add( _compileResults, 1 );
		_stack.Layout.Add( _logs, 1 );

		ApplyVisibility();
	}

	private void OnTabChanged( int index ) => ApplyVisibility();

	private void ApplyVisibility()
	{
		var idx = _tabs.ActiveIndex;
		if ( _animations.IsValid() )     _animations.Visible     = idx == 0;
		if ( _bindings.IsValid() )       _bindings.Visible       = idx == 1;
		if ( _compileResults.IsValid() ) _compileResults.Visible = idx == 2;
		if ( _logs.IsValid() )           _logs.Visible           = idx == 3;
	}

	public void DisplayCompileResult( SuiGenerationResult generation, SboxUiDesigner.EditorUi.SuiCompileResult compile )
	{
		_compileResults?.DisplayCompileResult( generation, compile );
		// Auto-switch to Compile Results tab when a compile finishes.
		_tabs.ActiveIndex = 2;
	}

	public void SetDocument( SboxUiDesigner.Runtime.SuiDocument doc )
	{
		_bindings?.SetDocument( doc );
	}
}

/// <summary>
/// Tab strip for the bottom panel. Same shape as SuiTabStrip but uses the
/// "active = filled with body color + blue top stripe" look the user specified.
/// </summary>
internal sealed class SuiBottomTabStrip : Widget
{
	private readonly List<SuiBottomTabStripItem> _tabs = new();
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

	public SuiBottomTabStrip( Widget parent = null ) : base( parent )
	{
		FixedHeight = 30;
		SetStyles( "background-color: rgb(20,20,19); border: none;" );
		Layout = Layout.Row();
		Layout.Margin = 0;
		Layout.Spacing = 0;
	}

	public void AddTab( string label )
	{
		var idx = _tabs.Count;
		// Insert a subtle vertical 1px line between consecutive tabs (same
		// look as the top-bar group separators).
		if ( _tabs.Count > 0 )
		{
			Layout.Add( new SuiBottomTabSeparator() );
		}
		var tab = new SuiBottomTabStripItem( this, idx, label );
		_tabs.Add( tab );
		Layout.Add( tab );
	}

	public void FinishLeftAligned() => Layout.AddStretchCell();

	internal bool IsActive( int idx ) => idx == _active;

	internal void OnTabClicked( int idx ) => ActiveIndex = idx;
}

internal sealed class SuiBottomTabStripItem : Widget
{
	private readonly SuiBottomTabStrip _strip;
	private readonly int _index;
	public string Label;

	private bool _hover;
	private bool _sized;

	private const int FontSize = 11;
	private const int PadH = 16;

	public SuiBottomTabStripItem( SuiBottomTabStrip strip, int index, string label ) : base( strip )
	{
		_strip = strip;
		_index = index;
		Label = label ?? "";
		Cursor = CursorShape.Finger;
		FixedHeight = 30;
		SetStyles( "background-color: transparent; border: none;" );

		FixedWidth = PadH + (Label.Length * 8) + PadH;
	}

	protected override void OnPaint()
	{
		Paint.SetDefaultFont( FontSize );

		if ( !_sized )
		{
			var labelW = string.IsNullOrEmpty( Label ) ? 0 : Paint.MeasureText( Label ).x;
			int newW = (int)(PadH + labelW + 2 + PadH);
			_sized = true;
			if ( newW != FixedWidth )
			{
				FixedWidth = newW;
				return;
			}
		}

		var rect = LocalRect;
		bool active = _strip.IsActive( _index );

		if ( active )
		{
			// Active tab fill — #212120 (same as content body, so the tab
			// reads as a flap of the body sticking up).
			Paint.SetBrushAndPen( new Color( 33 / 255f, 33 / 255f, 32 / 255f ) );
			Paint.DrawRect( rect );

			// Top stripe — #103354 (rgba 16, 51, 84), 3px high.
			Paint.SetBrushAndPen( new Color( 16 / 255f, 51 / 255f, 84 / 255f ) );
			Paint.DrawRect( new Rect( 0, 0, Width, 3 ) );
		}
		else if ( _hover )
		{
			Paint.SetBrushAndPen( Color.White.WithAlpha( 0.04f ) );
			Paint.DrawRect( rect );
		}

		// Label.
		var textColor = active
			? new Color( 240 / 255f, 244 / 255f, 250 / 255f )
			: new Color( 165 / 255f, 170 / 255f, 178 / 255f );
		Paint.SetPen( textColor );

		var labelRect = new Rect( PadH, 0, Width - PadH * 2, Height );
		Paint.DrawText( labelRect, Label, TextFlag.LeftCenter );
	}

	protected override void OnMouseEnter() { _hover = true; Update(); }
	protected override void OnMouseLeave() { _hover = false; Update(); }
	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton ) _strip.OnTabClicked( _index );
	}
}

/// <summary>
/// Subtle 1px vertical line between consecutive bottom-panel tabs. Same look
/// as the top-bar group separators (rgba(255,255,255,0.08), centered, 55%
/// of strip height).
/// </summary>
internal sealed class SuiBottomTabSeparator : Widget
{
	public SuiBottomTabSeparator( Widget parent = null ) : base( parent )
	{
		FixedWidth = 11;
		FixedHeight = 30;
		SetStyles( "background-color: transparent; border: none;" );
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
