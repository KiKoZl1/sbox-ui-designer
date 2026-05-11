using System;
using Editor;
using Sandbox;
using SboxUiDesigner.EditorUi.Canvas;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Center area of the editor — 100% custom, no DockManager.
///
/// Layout (Column):
///   1. <see cref="SuiTabStrip"/>           Designer / Preview / Code
///   2. <see cref="SuiCanvasMiniToolbar"/>  Screen / Zoom / Snap (canvas-scoped)
///   3. Content switcher                    Canvas | Preview | Code (Visible toggle)
///
/// Bound to a controller for canvas interactions; preview + code refresh on
/// tab activation only.
/// </summary>
public sealed class SuiCenterTabsWidget : Widget
{
	private SuiTabStrip _tabs;
	private SuiCanvasMiniToolbar _miniToolbar;

	private SuiCanvasWidget _canvas;
	private SuiPreviewTabWidget _preview;
	private SuiCodeTabWidget _code;

	private SuiTopBarDropdown _screenDd;
	private SuiTopBarDropdown _zoomDd;
	private SuiTopBarDropdown _snapDd;
	private SuiTopBarButton _rulersBtn;
	private SuiTopBarButton _respDebugBtn;
	private SuiTopBarButton _anchorsBtn;
	private SuiTopBarButton _boundsBtn;
	private SuiTopBarButton _fitBtn;

	private SuiDocument _document;

	public SuiCanvasWidget Canvas => _canvas;
	public SuiTopBarDropdown ScreenDropdown => _screenDd;
	public SuiTopBarDropdown ZoomDropdown => _zoomDd;
	public SuiTopBarDropdown SnapDropdown => _snapDd;
	public SuiTopBarButton RulersButton => _rulersBtn;
	public SuiTopBarButton ResponsiveDebugButton => _respDebugBtn;
	public SuiTopBarButton AnchorsButton => _anchorsBtn;
	public SuiTopBarButton BoundsButton => _boundsBtn;

	/// <summary>Fired when the Preview tab becomes active. Host uses this to
	/// regenerate the preview cache lazily (avoids a hotload on every doc edit).</summary>
	public event System.Action PreviewTabActivated;

	public SuiCenterTabsWidget( Widget parent = null ) : base( parent )
	{
		Name = "SuiCenter";
		MinimumSize = new Vector2( 400, 300 );
		SetStyles( "background-color: rgb(20,20,20);" );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		// 1. Tab strip — small, left-aligned (mockup spec).
		// Designer | Preview (embedded SceneRenderingWidget, simulated lifecycle) | Code.
		// "Test in Play" on the top toolbar is the separate real-Play workflow.
		_tabs = new SuiTabStrip( this );
		_tabs.AddTab( "Designer" );
		_tabs.AddTab( "Preview" );
		_tabs.AddTab( "Code" );
		_tabs.FinishLeftAligned();
		_tabs.ActiveTabChanged += OnTabChanged;
		Layout.Add( _tabs );

		// 2. Mini-toolbar — only visible on Designer tab.
		_miniToolbar = new SuiCanvasMiniToolbar( this );
		Layout.Add( _miniToolbar );

		// 3. Content stack — three pages.
		_canvas = new SuiCanvasWidget( this );
		_preview = new SuiPreviewTabWidget( this );
		_code = new SuiCodeTabWidget( this );

		Layout.Add( _canvas, 1 );
		Layout.Add( _preview, 1 );
		Layout.Add( _code, 1 );

		ApplyVisibility();
	}

	public void WireMiniToolbar(
		string screenValue, Action<Widget> openScreen,
		string zoomValue, Action<Widget> openZoom,
		string snapValue, Action<Widget> openSnap,
		Action onToggleRulers,
		Action onToggleResponsiveDebug,
		Action onToggleAnchors,
		Action onToggleBounds,
		Action<Widget> openAlignMenu,
		Action<Widget> openDistributeMenu,
		Action onFit )
	{
		// Group 1: dropdowns (Screen / Zoom / Snap)
		_screenDd = _miniToolbar.AddDropdown( "crop_landscape", "Screen", screenValue, openScreen, "Preview screen size" );
		_miniToolbar.AddGap( 8 );
		_zoomDd = _miniToolbar.AddDropdown( "search", "Zoom", zoomValue, openZoom, "Canvas zoom" );
		_miniToolbar.AddGap( 8 );
		_snapDd = _miniToolbar.AddDropdown( "add_box", "Snap", snapValue, openSnap, "Snap to grid" );

		_miniToolbar.AddSeparator();

		// Group 2: view toggles (Rulers / Responsive Debug) — icon-only, label in tooltip.
		_rulersBtn = _miniToolbar.AddButton( "", "straighten", onToggleRulers, "Toggle pixel rulers" );
		_respDebugBtn = _miniToolbar.AddButton( "", "bug_report", onToggleResponsiveDebug, "Show responsive issues + safe area + bounds" );
		_anchorsBtn = _miniToolbar.AddButton( "", "my_location", onToggleAnchors, "Show anchor markers for the selected element" );
		_boundsBtn = _miniToolbar.AddButton( "", "select_all", onToggleBounds, "Show layout bounds (dashed outlines around every element)" );

		_miniToolbar.AddSeparator();

		// Group 3: alignment ops on multi-selection — two dropdowns to keep
		// the toolbar compact while exposing all 8 operations.
		_miniToolbar.AddMenuButton( "Align",      "align_horizontal_center", openAlignMenu,      "Align selected elements (≥2 with same parent)" );
		_miniToolbar.AddMenuButton( "Distribute", "horizontal_distribute",   openDistributeMenu, "Distribute selected elements evenly (≥3 with same parent)" );

		_miniToolbar.AddStretch();

		// Group 3: fit-to-screen on the right
		_fitBtn = _miniToolbar.AddButton( "Fit", "fit_screen", onFit, "Fit canvas to viewport (Ctrl+0)" );
	}

	public void RefreshToggleStates()
	{
		if ( _document?.Settings == null ) return;
		if ( _rulersBtn != null )    { _rulersBtn.IsActive    = _document.Settings.ShowRulers;       _rulersBtn.Update(); }
		if ( _respDebugBtn != null ) { _respDebugBtn.IsActive = _document.Settings.ResponsiveDebug;  _respDebugBtn.Update(); }
		if ( _anchorsBtn != null )   { _anchorsBtn.IsActive   = _document.Settings.ShowAnchors;      _anchorsBtn.Update(); }
		if ( _boundsBtn != null )    { _boundsBtn.IsActive    = _document.Settings.ShowLayoutBounds; _boundsBtn.Update(); }
	}

	private void OnTabChanged( int index )
	{
		ApplyVisibility();
		// Preview tab → regenerate cache + remount type. Host uses this for
		// lazy regen (avoids a hotload churn on every property edit).
		if ( index == 1 )
		{
			PreviewTabActivated?.Invoke();
			_preview?.Reload();
		}
		// Code tab → regenerate the .razor/.scss text views.
		else if ( index == 2 )
		{
			_code?.Reload();
		}
	}

	private void ApplyVisibility()
	{
		var idx = _tabs.ActiveIndex;
		// Mini-toolbar only appears under Designer.
		if ( _miniToolbar.IsValid() ) _miniToolbar.Visible = idx == 0;
		if ( _canvas.IsValid() )      _canvas.Visible      = idx == 0;
		if ( _preview.IsValid() )     _preview.Visible     = idx == 1;
		if ( _code.IsValid() )        _code.Visible        = idx == 2;
	}

	public void SetController( SuiDesignerController controller )
	{
		_canvas?.SetController( controller );
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		_canvas?.SetDocument( document );
		_preview?.SetDocument( document );
		_code?.SetDocument( document );
	}

	public SuiCanvasViewport GetViewport() => _canvas?.GetViewport();
}
