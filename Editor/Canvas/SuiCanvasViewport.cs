using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Canvas;

/// <summary>
/// The 2D paint-based design viewport. Replaces the SceneRenderingWidget-based
/// preview that was the canvas during M10. This widget is responsible for:
///
/// 1. Painting the document via <see cref="SuiCanvasRenderer"/> — fast, zero
///    runtime hot-load churn.
/// 2. Applying pan + zoom (logical→widget transform) so designers can navigate.
/// 3. Drawing selection chrome / hover outline / marquee / alignment guides
///    on top of the document, in widget pixels (after resetting the transform).
/// 4. Routing mouse events to the parent SuiCanvasWidget for
///    hit-test + drag/resize logic (Phase 2).
///
/// Coordinate systems (see PRD doc 15 §4.3):
/// - <b>Logical</b>: pixels in the document's drawable area (0..PanelSize).
/// - <b>Widget</b>: pixels of this widget's local rect (Paint API native space).
/// - Conversion: <see cref="LogicalToWidget"/> / <see cref="WidgetToLogical"/>.
/// </summary>
public sealed class SuiCanvasViewport : Widget
{
	private SuiDocument _document;
	private SuiDesignerController _controller;
	private readonly SuiLayoutSolver _solver = new( new Vector2( 1920, 1080 ) );
	private SuiCanvasRenderer _renderer;
	private string _projectAssetsRoot;

	// View state (zoom/pan) — persisted on the document via Settings.CanvasZoom/Pan.
	private float _zoom = 1.0f;
	private Vector2 _pan = Vector2.Zero;

	// Hover + selection state — Phase 2 will populate these via mouse events.
	internal SuiElement HoverElement;
	internal Rect? MarqueeRect; // in widget-pixel space

	/// <summary>
	/// Optional error banner shown over the canvas. Populated by the Window
	/// when Compile fails or schema validation surfaces errors. Click X to dismiss.
	/// </summary>
	public SuiCanvasErrorBanner ErrorBanner { get; set; }

	// Public API for the parent SuiCanvasWidget to query and update.
	public Vector2 PanelSize => _solver.PanelSize;
	public SuiLayoutSolver Solver => _solver;
	public float Zoom => _zoom;
	public Vector2 Pan => _pan;

	public event Action OnRepaintRequested;

	/// <summary>
	/// External handler for mouse events. Returns true if the event was
	/// consumed (Phase 2 wires this up; Phase 1 leaves it null = ignore).
	/// </summary>
	public Func<MouseEvent, bool> MousePressHandler;
	public Action<MouseEvent> MouseMoveHandler;
	public Action<MouseEvent> MouseReleaseHandler;

	public SuiCanvasViewport( Widget parent ) : base( parent )
	{
		MouseTracking = true;
		FocusMode = FocusMode.Click;
		AcceptDrops = true;

		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		_projectAssetsRoot = string.IsNullOrEmpty( projectRoot ) ? null : System.IO.Path.Combine( projectRoot, "Assets" );

		_renderer = new SuiCanvasRenderer( _solver, _projectAssetsRoot );
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		// Restore view state if persisted on the document.
		if ( document?.Settings != null )
		{
			_zoom = document.Settings.CanvasZoom > 0.01f ? document.Settings.CanvasZoom : 1.0f;
			_pan = new Vector2( document.Settings.CanvasPanX, document.Settings.CanvasPanY );
		}
		// Solver panel size follows the canvas/preview settings so changing
		// PreviewWidth/Height in the toolbar updates the canvas dimensions.
		ApplyPanelSizeFromDocument();
		Invalidate();
	}

	/// <summary>
	/// Resolve the effective panel size for the solver from the document settings.
	/// PreviewWidth/Height (when &gt; 0) override BaseWidth/Height — lets the
	/// designer simulate different viewport sizes without touching BaseWidth.
	/// </summary>
	public void ApplyPanelSizeFromDocument()
	{
		if ( _document?.Canvas == null ) return;
		var w = _document.Canvas.PreviewWidth > 0 ? _document.Canvas.PreviewWidth : _document.Canvas.BaseWidth;
		var h = _document.Canvas.PreviewHeight > 0 ? _document.Canvas.PreviewHeight : _document.Canvas.BaseHeight;
		_solver.PanelSize = new Vector2( w, h );
	}

	public void SetController( SuiDesignerController controller )
	{
		_controller = controller;
	}

	public void Invalidate()
	{
		Update();
		OnRepaintRequested?.Invoke();
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Coordinate transforms (the "matemática 1:1" — single source)
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Where the document's (0,0) lands in widget pixels when the canvas is
	/// centered with the current zoom. Pan offset is added on top.
	/// </summary>
	public Vector2 CanvasOriginInWidget()
	{
		var w = Size.x;
		var h = Size.y;
		var panel = _solver.PanelSize;
		// Center the panel-at-current-zoom inside the widget.
		var ox = (w - panel.x * _zoom) * 0.5f;
		var oy = (h - panel.y * _zoom) * 0.5f;
		return new Vector2( ox, oy ) + _pan;
	}

	public Vector2 LogicalToWidget( Vector2 logical )
		=> logical * _zoom + CanvasOriginInWidget();

	public Vector2 WidgetToLogical( Vector2 widget )
		=> (widget - CanvasOriginInWidget()) / _zoom;

	/// <summary>The visible panel rect in widget pixels.</summary>
	public Rect PanelRectInWidget()
	{
		var origin = CanvasOriginInWidget();
		return new Rect( origin.x, origin.y, _solver.PanelSize.x * _zoom, _solver.PanelSize.y * _zoom );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Zoom + pan
	// ─────────────────────────────────────────────────────────────────────

	public void SetZoom( float zoom, Vector2? anchorWidgetPos = null )
	{
		zoom = MathF.Max( 0.1f, MathF.Min( 8f, zoom ) );
		if ( MathF.Abs( zoom - _zoom ) < 0.001f ) return;

		// Anchor the zoom around a widget-space point so the user's cursor stays
		// over the same logical pixel after the change.
		if ( anchorWidgetPos.HasValue )
		{
			var anchor = anchorWidgetPos.Value;
			var logicalAtAnchor = WidgetToLogical( anchor );
			_zoom = zoom;
			var newWidgetAtLogical = LogicalToWidget( logicalAtAnchor );
			_pan += anchor - newWidgetAtLogical;
		}
		else
		{
			_zoom = zoom;
		}

		PersistView();
		Invalidate();
	}

	public void SetPan( Vector2 pan )
	{
		_pan = pan;
		PersistView();
		Invalidate();
	}

	public void FitCanvas()
	{
		var w = Size.x;
		var h = Size.y;
		if ( w <= 0 || h <= 0 ) return;
		var panel = _solver.PanelSize;
		var fitZoom = MathF.Min( w / panel.x, h / panel.y ) * 0.95f;
		_zoom = MathF.Max( 0.1f, fitZoom );
		_pan = Vector2.Zero;
		PersistView();
		Invalidate();
	}

	private void PersistView()
	{
		if ( _document?.Settings == null ) return;
		_document.Settings.CanvasZoom = _zoom;
		_document.Settings.CanvasPanX = _pan.x;
		_document.Settings.CanvasPanY = _pan.y;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Paint
	// ─────────────────────────────────────────────────────────────────────

	protected override void OnPaint()
	{
		// Background: canvas dark area — #141414 (rgba 20,20,20) per user spec.
		var bg = new Color( 20 / 255f, 20 / 255f, 20 / 255f );
		Paint.SetBrush( bg );
		Paint.ClearPen();
		Paint.DrawRect( LocalRect );

		var panelRect = PanelRectInWidget();
		PaintCheckerboard( panelRect );

		if ( _document == null ) return;

		// Reapply panel size each paint so live PreviewWidth/Height changes are picked up.
		ApplyPanelSizeFromDocument();

		// Solve layout once per paint. Cheap — pure data math, no allocation.
		_solver.Solve( _document );

		// Push logical→widget transform: translate to canvas origin, scale to zoom.
		// Document elements draw at their logical coords; the transform handles
		// the rest. After document, we ResetTransform and draw chrome in widget pixels.
		var origin = CanvasOriginInWidget();
		Paint.Translate( origin );
		Paint.Scale( _zoom, _zoom );

		// Pass zoom to renderer so image rendering can decide whether the native
		// pixmap needs a CPU pre-resize (heavy downsample = aliasing on GPU).
		_renderer.Zoom = _zoom;
		_renderer.Paint( _document );

		// Grid overlay (in logical space, so it lives under content but over checkerboard).
		if ( _document?.Settings?.ShowGrid == true )
			PaintGridOverlay( _document.Settings.GridSize > 0 ? _document.Settings.GridSize : 8 );

		Paint.ResetTransform();

		// Document boundary outline — #1F1F1F (rgba 31,31,31) per user spec.
		Paint.SetPen( new Color( 31 / 255f, 31 / 255f, 31 / 255f ), 1f );
		Paint.ClearBrush();
		Paint.DrawRect( panelRect );

		// Rulers — top + left, with pixel marks. Drawn AFTER content so they
		// never get clipped, BEFORE chrome so selection chrome sits on top.
		if ( _document?.Settings?.ShowRulers == true )
			PaintRulers( panelRect );

		// Safe area overlay (designer aid) — dashed rect inside the panel rect.
		if ( _document?.Settings?.ShowSafeArea == true && _document?.Canvas?.SafeArea != null )
			PaintSafeArea( panelRect, _document.Canvas.SafeArea );

		// Anchor reference points overlay — small cross at the resolved anchor
		// position of every visible element (designer aid; not generated).
		if ( _document?.Settings?.ShowAnchors == true )
			PaintAnchorMarkers();

		// Layout bounds: outline every element rect for visual debugging.
		if ( _document?.Settings?.ShowLayoutBounds == true || _document?.Settings?.ResponsiveDebug == true )
			PaintLayoutBounds();

		// Widget info: small label per element showing name + size.
		if ( _document?.Settings?.ShowWidgetInfo == true || _document?.Settings?.ResponsiveDebug == true )
			PaintWidgetInfo();

		// Chrome (Phase 2 will populate these).
		PaintChromeOverlays( panelRect );

		// Responsive Debug overlay — issues panel + safe area watermark.
		if ( _document?.Settings?.ResponsiveDebug == true )
			PaintResponsiveDebug( panelRect );

		// Error banner (T3): always last so it sits above all canvas content.
		PaintErrorBanner();
	}

	/// <summary>
	/// Render an error banner at the top of the viewport. Click anywhere on
	/// the banner triggers <see cref="SuiCanvasErrorBanner.OnClick"/>; clicking
	/// the X area triggers <see cref="SuiCanvasErrorBanner.OnDismiss"/>.
	/// </summary>
	private void PaintErrorBanner()
	{
		var b = ErrorBanner;
		if ( b == null || string.IsNullOrEmpty( b.Title ) ) return;

		var rect = new Rect( 8, 8, MathF.Max( 200, Size.x - 16 ), 56 );

		// Slightly translucent red bg so the canvas underneath is still hinted at.
		Paint.SetBrush( new Color( 0.55f, 0.10f, 0.10f, 0.92f ) );
		Paint.SetPen( new Color( 0.85f, 0.25f, 0.25f, 1.0f ), 1.5f );
		Paint.DrawRect( rect, 4f );

		// Icon area
		Paint.SetPen( Color.White );
		Paint.DrawIcon( new Rect( rect.Left + 10, rect.Top + 8, 20, 20 ), "error", 18, TextFlag.LeftCenter );

		// Title (bold) + detail (single-line elided)
		Paint.SetFont( Theme.DefaultFont, 12, 700 );
		Paint.SetPen( Color.White );
		Paint.DrawText( new Rect( rect.Left + 36, rect.Top + 6, rect.Width - 72, 20 ), b.Title, TextFlag.LeftTop );

		if ( !string.IsNullOrEmpty( b.Detail ) )
		{
			Paint.SetFont( Theme.DefaultFont, 10, 400 );
			Paint.SetPen( new Color( 1f, 1f, 1f, 0.85f ) );
			var elided = Paint.GetElidedText( b.Detail, rect.Width - 72, ElideMode.Right, TextFlag.LeftTop );
			Paint.DrawText( new Rect( rect.Left + 36, rect.Top + 26, rect.Width - 72, 24 ), elided, TextFlag.LeftTop );
		}

		// Dismiss X (right side)
		var xRect = new Rect( rect.Right - 28, rect.Top + 6, 22, 22 );
		Paint.SetFont( Theme.DefaultFont, 14, 700 );
		Paint.SetPen( Color.White );
		Paint.DrawText( xRect, "×", TextFlag.Center );

		_bannerRect = rect;
		_bannerDismissRect = xRect;
	}

	private Rect _bannerRect;
	private Rect _bannerDismissRect;

	private static bool PointInside( Rect r, Vector2 p )
		=> p.x >= r.Left && p.x <= r.Right && p.y >= r.Top && p.y <= r.Bottom;

	private bool TryHandleBannerClick( Vector2 widgetPos )
	{
		if ( ErrorBanner == null ) return false;
		if ( _bannerRect.Width < 1 ) return false;
		// Check dismiss first.
		if ( PointInside( _bannerDismissRect, widgetPos ) )
		{
			ErrorBanner.OnDismiss?.Invoke();
			ErrorBanner = null;
			Invalidate();
			return true;
		}
		if ( PointInside( _bannerRect, widgetPos ) )
		{
			ErrorBanner.OnClick?.Invoke();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Paint rulers along the top and left edges of the canvas, in widget
	/// pixels. Major tick = 100 logical px (with label), minor tick = 50 logical px.
	/// Tick density adapts to zoom so we don't overcrowd at small zooms.
	/// </summary>
	private void PaintRulers( Rect panelRect )
	{
		const float rulerThickness = 18f;
		// Ruler bar background — #1E1E1D (rgba 30,30,29) per user spec.
		// Same family as top toolbar so the chrome reads as one cohesive ring.
		var bg = new Color( 30 / 255f, 30 / 255f, 29 / 255f, 1.0f );
		var fg = new Color( 0.55f, 0.55f, 0.60f, 1.0f );
		var fgDim = new Color( 0.40f, 0.40f, 0.45f, 1.0f );

		// Top ruler bar.
		Paint.SetBrush( bg );
		Paint.ClearPen();
		Paint.DrawRect( new Rect( 0, 0, Size.x, rulerThickness ) );
		// Left ruler bar.
		Paint.DrawRect( new Rect( 0, 0, rulerThickness, Size.y ) );

		// Choose tick spacing so major ticks are ≥ 50 widget pixels apart.
		float majorLogical = 100f;
		while ( majorLogical * _zoom < 50f ) majorLogical *= 2f;
		var minorLogical = majorLogical * 0.5f;

		Paint.SetFont( Theme.DefaultFont, 9, 400 );

		// Horizontal ticks (top ruler).
		var firstX = MathF.Floor( WidgetToLogical( new Vector2( rulerThickness, 0 ) ).x / minorLogical ) * minorLogical;
		var lastX = WidgetToLogical( new Vector2( Size.x, 0 ) ).x;
		for ( var lx = firstX; lx <= lastX; lx += minorLogical )
		{
			var wx = LogicalToWidget( new Vector2( lx, 0 ) ).x;
			if ( wx < rulerThickness ) continue;
			var isMajor = MathF.Abs( (lx % majorLogical + majorLogical) % majorLogical ) < 0.001f;
			var tickH = isMajor ? 8f : 4f;
			Paint.SetPen( isMajor ? fg : fgDim, 1f );
			Paint.DrawLine( new Vector2( wx, rulerThickness - tickH ), new Vector2( wx, rulerThickness ) );
			if ( isMajor )
			{
				Paint.ClearBrush();
				Paint.SetPen( fg );
				Paint.DrawText( new Rect( wx + 2, 0, 60, rulerThickness ), $"{(int)lx}", TextFlag.LeftCenter );
			}
		}

		// Vertical ticks (left ruler).
		var firstY = MathF.Floor( WidgetToLogical( new Vector2( 0, rulerThickness ) ).y / minorLogical ) * minorLogical;
		var lastY = WidgetToLogical( new Vector2( 0, Size.y ) ).y;
		for ( var ly = firstY; ly <= lastY; ly += minorLogical )
		{
			var wy = LogicalToWidget( new Vector2( 0, ly ) ).y;
			if ( wy < rulerThickness ) continue;
			var isMajor = MathF.Abs( (ly % majorLogical + majorLogical) % majorLogical ) < 0.001f;
			var tickW = isMajor ? 8f : 4f;
			Paint.SetPen( isMajor ? fg : fgDim, 1f );
			Paint.DrawLine( new Vector2( rulerThickness - tickW, wy ), new Vector2( rulerThickness, wy ) );
			if ( isMajor )
			{
				Paint.ClearBrush();
				Paint.SetPen( fg );
				Paint.DrawText( new Rect( 1, wy - 8, rulerThickness - 2, 14 ), $"{(int)ly}", TextFlag.RightCenter );
			}
		}

		// Corner cap.
		Paint.SetBrush( bg.Darken( 0.1f ) );
		Paint.ClearPen();
		Paint.DrawRect( new Rect( 0, 0, rulerThickness, rulerThickness ) );
	}

	/// <summary>
	/// Paint a dashed safe area rect inside the panel — designer aid showing
	/// where critical UI should fit (TVs, ultrawide, etc).
	/// </summary>
	private void PaintSafeArea( Rect panelRect, SuiSafeArea sa )
	{
		if ( !sa.Enabled ) return;
		var inner = new Rect(
			panelRect.Left + sa.Left * _zoom,
			panelRect.Top + sa.Top * _zoom,
			panelRect.Width - (sa.Left + sa.Right) * _zoom,
			panelRect.Height - (sa.Top + sa.Bottom) * _zoom );

		Paint.SetPen( new Color( 0.30f, 0.65f, 1.0f, 0.85f ), 1.5f );
		Paint.ClearBrush();
		Paint.DrawRect( inner );

		Paint.SetFont( Theme.DefaultFont, 9, 600 );
		Paint.SetPen( new Color( 0.30f, 0.65f, 1.0f, 0.85f ) );
		Paint.DrawText( new Rect( inner.Left + 4, inner.Top + 2, 200, 14 ), "Safe Area", TextFlag.LeftTop );
	}

	/// <summary>
	/// Paint a small cross/anchor glyph at the resolved anchor reference point
	/// of every visible element. Helps designers see where elements are anchored
	/// without selecting each one.
	/// </summary>
	private void PaintAnchorMarkers()
	{
		if ( _document == null ) return;
		// Only paint the anchor for the selected element — drawing every
		// element's anchor at once turns the canvas into yellow spaghetti.
		var sel = _controller?.Selected;
		if ( sel == null ) return;
		if ( string.IsNullOrEmpty( sel.ParentId ) ) return;
		if ( !_solver.TryGetRect( sel.ParentId, out var pr ) ) return;

		var anchor = sel.Layout?.Anchor ?? SuiAnchor.TopLeft;

		// Resolve up to 4 marker positions in parent-local space depending
		// on the anchor type, plus an optional bracket connecting them.
		switch ( anchor )
		{
			case SuiAnchor.Stretch:
				// Four corners of the parent.
				DrawAnchorBracket( pr );
				DrawQuadPin( LogicalToWidget( new Vector2( pr.Left, pr.Top ) ) );
				DrawQuadPin( LogicalToWidget( new Vector2( pr.Right, pr.Top ) ) );
				DrawQuadPin( LogicalToWidget( new Vector2( pr.Left, pr.Bottom ) ) );
				DrawQuadPin( LogicalToWidget( new Vector2( pr.Right, pr.Bottom ) ) );
				break;

			case SuiAnchor.StretchHorizontal:
			{
				var midY = pr.Top + pr.Height * 0.5f;
				var a = LogicalToWidget( new Vector2( pr.Left, midY ) );
				var b = LogicalToWidget( new Vector2( pr.Right, midY ) );
				DrawDashedLine( a, b );
				DrawQuadPin( a );
				DrawQuadPin( b );
				break;
			}

			case SuiAnchor.StretchVertical:
			{
				var midX = pr.Left + pr.Width * 0.5f;
				var a = LogicalToWidget( new Vector2( midX, pr.Top ) );
				var b = LogicalToWidget( new Vector2( midX, pr.Bottom ) );
				DrawDashedLine( a, b );
				DrawQuadPin( a );
				DrawQuadPin( b );
				break;
			}

			default:
			{
				// Single anchor — one pin at the resolved point.
				var p = AnchorPointInParent( anchor, pr );
				DrawQuadPin( LogicalToWidget( p ) );
				break;
			}
		}
	}

	/// <summary>
	/// "Quad Pin" anchor marker — small + shape with a soft circular core.
	/// Tuned to harmonize with the rest of the dark editor (no neon, no
	/// glow halos), but bright enough to spot at a glance.
	/// </summary>
	private void DrawQuadPin( Vector2 pos )
	{
		// Halo / core: subtle blue circle inside a thin light ring.
		var coreColor = new Color( 0f, 0.55f, 1f, 0.85f );
		var ringColor = new Color( 0.85f, 0.88f, 0.95f, 0.85f );

		// Outer thin ring (12px diameter).
		Paint.SetBrush( new Color( 0.85f, 0.88f, 0.95f, 0.18f ) );
		Paint.SetPen( ringColor, 1f );
		Paint.DrawCircle( pos, new Vector2( 12, 12 ) );

		// Solid blue core (6px diameter).
		Paint.SetBrush( coreColor );
		Paint.ClearPen();
		Paint.DrawCircle( pos, new Vector2( 6, 6 ) );

		// Four short tabs (cross arms) — 5px length, gap of 3px from the ring.
		Paint.SetPen( ringColor, 1.5f );
		const float gap = 5f;
		const float len = 4f;
		Paint.DrawLine( new Vector2( pos.x - gap - len, pos.y ), new Vector2( pos.x - gap, pos.y ) );
		Paint.DrawLine( new Vector2( pos.x + gap, pos.y ),       new Vector2( pos.x + gap + len, pos.y ) );
		Paint.DrawLine( new Vector2( pos.x, pos.y - gap - len ), new Vector2( pos.x, pos.y - gap ) );
		Paint.DrawLine( new Vector2( pos.x, pos.y + gap ),       new Vector2( pos.x, pos.y + gap + len ) );
	}

	/// <summary>
	/// Dashed connector — used between two pins (stretch H/V) to imply the
	/// element spans between them. Native PenStyle.Dash, no per-segment loop.
	/// </summary>
	private void DrawDashedLine( Vector2 a, Vector2 b )
	{
		Paint.SetPen( new Color( 0.85f, 0.88f, 0.95f, 0.30f ), 1f, PenStyle.Dash );
		Paint.DrawLine( a, b );
	}

	/// <summary>Dotted bracket along the parent rect — used by Fill anchor
	/// to imply the element snaps to all four sides.</summary>
	private void DrawAnchorBracket( Rect parentRect )
	{
		var widgetRect = LogicalRectToWidget( parentRect );
		Paint.SetPen( new Color( 0.85f, 0.88f, 0.95f, 0.30f ), 1f, PenStyle.Dash );
		Paint.ClearBrush();
		Paint.DrawRect( widgetRect );
	}

	private void PaintLayoutBounds()
	{
		if ( _document == null ) return;
		// Native dashed pen — Qt does the dashing, no per-segment loop.
		// Subtle alpha so bounds read as a "what's there" hint, not chrome.
		Paint.SetPen( new Color( 1f, 1f, 1f, 0.27f ), 1f, PenStyle.Dash );
		Paint.ClearBrush();
		foreach ( var el in _document.Elements )
		{
			if ( string.IsNullOrEmpty( el.ParentId ) ) continue;
			if ( el.Flags?.HiddenInDesigner == true ) continue;
			if ( !_solver.TryGetRect( el.Id, out var r ) ) continue;
			Paint.DrawRect( LogicalRectToWidget( r ) );
		}
	}

	private void PaintWidgetInfo()
	{
		if ( _document == null ) return;
		Paint.SetFont( Theme.DefaultFont, 9, 600 );
		Paint.ClearBrush();
		foreach ( var el in _document.Elements )
		{
			if ( string.IsNullOrEmpty( el.ParentId ) ) continue;
			if ( el.Flags?.HiddenInDesigner == true ) continue;
			if ( !_solver.TryGetRect( el.Id, out var r ) ) continue;
			var widgetRect = LogicalRectToWidget( r );
			if ( widgetRect.Width < 50 || widgetRect.Height < 14 ) continue;

			var label = $"{el.Name}  {(int)el.Layout.Width}×{(int)el.Layout.Height}";
			var pillRect = new Rect( widgetRect.Left + 2, widgetRect.Top + 2, MathF.Min( widgetRect.Width - 4, 160 ), 12 );
			Paint.SetBrush( new Color( 0, 0, 0, 0.65f ) );
			Paint.ClearPen();
			Paint.DrawRect( pillRect, 2 );
			Paint.SetPen( new Color( 0.85f, 0.85f, 0.90f, 0.95f ) );
			Paint.ClearBrush();
			Paint.DrawText( pillRect, label, TextFlag.Center );
		}
	}

	private void PaintResponsiveDebug( Rect panelRect )
	{
		// Float a small "Issues found" banner on top-right + safe area watermark
		// at bottom-right. The actual issue list is computed below.
		var issues = ScanResponsiveIssues();
		if ( issues.Count == 0 )
		{
			// "All good" badge.
			var rect = new Rect( Size.x - 180, 28, 170, 22 );
			Paint.SetBrush( new Color( 0.13f, 0.45f, 0.20f, 0.85f ) );
			Paint.SetPen( new Color( 0.30f, 0.85f, 0.45f, 1f ), 1 );
			Paint.DrawRect( rect, 4 );
			Paint.SetFont( Theme.DefaultFont, 10, 600 );
			Paint.SetPen( Color.White );
			Paint.DrawText( rect, "✓ Responsive OK", TextFlag.Center );
			return;
		}

		// Issues panel — top-right, vertical stack.
		const float panelW = 280;
		var panelHeight = 36 + issues.Count * 30;
		var panel = new Rect( Size.x - panelW - 12, 28, panelW, panelHeight );
		Paint.SetBrush( new Color( 0.13f, 0.13f, 0.16f, 0.95f ) );
		Paint.SetPen( new Color( 0.85f, 0.30f, 0.30f, 0.85f ), 1.5f );
		Paint.DrawRect( panel, 4 );

		Paint.SetFont( Theme.DefaultFont, 10, 700 );
		Paint.SetPen( new Color( 1f, 0.5f, 0.5f ) );
		Paint.DrawText( new Rect( panel.Left + 8, panel.Top + 6, panel.Width - 16, 14 ),
			$"⚠ {issues.Count} Responsive Issue{(issues.Count == 1 ? "" : "s")}", TextFlag.LeftCenter );

		Paint.SetFont( Theme.DefaultFont, 9, 400 );
		for ( int i = 0; i < issues.Count; i++ )
		{
			var rowY = panel.Top + 26 + i * 30;
			Paint.SetPen( Color.White );
			Paint.DrawText( new Rect( panel.Left + 8, rowY, panel.Width - 16, 14 ),
				$"{i + 1}. {issues[i].Title}", TextFlag.LeftCenter );
			Paint.SetPen( new Color( 0.7f, 0.7f, 0.75f ) );
			Paint.DrawText( new Rect( panel.Left + 8, rowY + 14, panel.Width - 16, 14 ),
				issues[i].Detail, TextFlag.LeftCenter );
		}
	}

	private System.Collections.Generic.List<(string Title, string Detail)> ScanResponsiveIssues()
	{
		var list = new System.Collections.Generic.List<(string, string)>();
		if ( _document == null ) return list;

		var sa = _document.Canvas?.SafeArea;
		var saEnabled = sa?.Enabled ?? false;
		var panel = _solver.PanelSize;
		var safeLeft = saEnabled ? sa.Left : 0;
		var safeTop = saEnabled ? sa.Top : 0;
		var safeRight = panel.x - (saEnabled ? sa.Right : 0);
		var safeBottom = panel.y - (saEnabled ? sa.Bottom : 0);

		foreach ( var el in _document.Elements )
		{
			if ( string.IsNullOrEmpty( el.ParentId ) ) continue;
			if ( el.Flags?.HiddenInDesigner == true ) continue;
			if ( !_solver.TryGetRect( el.Id, out var r ) ) continue;

			if ( saEnabled && (r.Right > safeRight + 1 || r.Left < safeLeft - 1
				|| r.Top < safeTop - 1 || r.Bottom > safeBottom + 1) )
			{
				list.Add( ($"Widget outside safe area", $"{el.Name}") );
			}

			if ( el.Type == SuiElementType.Text && (el.Layout?.Width ?? 0) > 0
				&& !string.IsNullOrEmpty( el.Props?.Text ) )
			{
				// Rough text overflow heuristic — text length × fontSize×0.55 estimate.
				var estW = (el.Props.Text?.Length ?? 0) * (el.Props.FontSize > 0 ? el.Props.FontSize * 0.55f : 8f);
				if ( estW > el.Layout.Width + 4 && el.Props.TextSizeMode == SuiTextSizeMode.Fixed )
					list.Add( ($"Text overflow detected", $"{el.Name} (estimated {(int)estW}px > width {(int)el.Layout.Width}px)") );
			}

			if ( el.Layout?.MinWidth.HasValue == true && r.Width < el.Layout.MinWidth.Value - 0.5f )
				list.Add( ($"Min width exceeded", $"{el.Name} ({(int)r.Width} < {(int)el.Layout.MinWidth.Value})") );
		}

		return list;
	}

	private static Vector2 AnchorPointInParent( SuiAnchor a, Rect parentRect )
	{
		float x = parentRect.Left;
		float y = parentRect.Top;
		switch ( a )
		{
			case SuiAnchor.TopLeft: x = parentRect.Left; y = parentRect.Top; break;
			case SuiAnchor.TopCenter: x = parentRect.Left + parentRect.Width * 0.5f; y = parentRect.Top; break;
			case SuiAnchor.TopRight: x = parentRect.Right; y = parentRect.Top; break;
			case SuiAnchor.MiddleLeft: x = parentRect.Left; y = parentRect.Top + parentRect.Height * 0.5f; break;
			case SuiAnchor.MiddleCenter: x = parentRect.Left + parentRect.Width * 0.5f; y = parentRect.Top + parentRect.Height * 0.5f; break;
			case SuiAnchor.MiddleRight: x = parentRect.Right; y = parentRect.Top + parentRect.Height * 0.5f; break;
			case SuiAnchor.BottomLeft: x = parentRect.Left; y = parentRect.Bottom; break;
			case SuiAnchor.BottomCenter: x = parentRect.Left + parentRect.Width * 0.5f; y = parentRect.Bottom; break;
			case SuiAnchor.BottomRight: x = parentRect.Right; y = parentRect.Bottom; break;
		}
		return new Vector2( x, y );
	}

	/// <summary>
	/// Paint a subtle grid overlay (dots) at <paramref name="step"/> logical-pixel
	/// intervals across the panel rect. Drawn AFTER content but still inside the
	/// Paint.Scale block, so coords are in logical space.
	/// </summary>
	private void PaintGridOverlay( float step )
	{
		var panel = _solver.PanelSize;
		// Skip if dots would be too dense (every dot ≤ 4 widget pixels).
		if ( step * _zoom < 4f ) return;

		var color = new Color( 1f, 1f, 1f, 0.08f );
		Paint.SetBrush( color );
		Paint.ClearPen();
		var dotSize = 1f / _zoom; // keep dots ~1px on screen regardless of zoom
		for ( float y = 0; y <= panel.y; y += step )
		{
			for ( float x = 0; x <= panel.x; x += step )
			{
				Paint.DrawRect( new Rect( x - dotSize * 0.5f, y - dotSize * 0.5f, dotSize, dotSize ) );
			}
		}
	}

	private void PaintCheckerboard( Rect rect )
	{
		// 16x16 logical-pixel squares, alternating two near-black shades.
		const float cellLogical = 16f;
		var cell = cellLogical * _zoom;
		if ( cell < 4 ) cell = 4; // don't draw sub-pixel cells

		var dark = new Color( 0.10f, 0.10f, 0.12f );
		var light = new Color( 0.13f, 0.13f, 0.15f );

		Paint.ClearPen();
		Paint.SetBrush( dark );
		Paint.DrawRect( rect );

		Paint.SetBrush( light );
		var cols = (int)MathF.Ceiling( rect.Width / cell );
		var rows = (int)MathF.Ceiling( rect.Height / cell );
		for ( int y = 0; y < rows; y++ )
		{
			for ( int x = 0; x < cols; x++ )
			{
				if ( ((x + y) & 1) == 0 ) continue;
				var cellRect = new Rect(
					rect.Left + x * cell,
					rect.Top + y * cell,
					MathF.Min( cell, rect.Right - (rect.Left + x * cell) ),
					MathF.Min( cell, rect.Bottom - (rect.Top + y * cell) )
				);
				Paint.DrawRect( cellRect );
			}
		}
	}

	private void PaintChromeOverlays( Rect panelRect )
	{
		// Hover outline — only if the hovered element isn't already selected
		// (selection chrome already draws on it).
		if ( HoverElement != null && (_controller == null || !_controller.IsSelected( HoverElement ))
			&& _solver.TryGetRect( HoverElement.Id, out var hoverLogical ) )
		{
			var hoverScreen = LogicalRectToWidget( hoverLogical );
			Paint.SetPen( new Color( 0.20f, 0.55f, 1.0f, 0.7f ), 1f );
			Paint.ClearBrush();
			Paint.DrawRect( hoverScreen );
		}

		// Selection chrome — draw secondary borders on every selected element,
		// then overlay the primary's full chrome (with handles + size label) on top.
		if ( _controller != null && _controller.SelectedCount > 0 )
		{
			var primary = _controller.Selected;
			foreach ( var el in _controller.SelectedSet )
			{
				if ( el == null || el == primary ) continue;
				if ( string.IsNullOrEmpty( el.ParentId ) ) continue;
				if ( !_solver.TryGetRect( el.Id, out var r ) ) continue;
				DrawSecondarySelectionBorder( LogicalRectToWidget( r ) );
			}
			if ( primary != null && !string.IsNullOrEmpty( primary.ParentId )
				&& _solver.TryGetRect( primary.Id, out var primLogical ) )
			{
				DrawSelectionChrome( LogicalRectToWidget( primLogical ), primary );
			}

			// "N selected" label when multi.
			if ( _controller.SelectedCount > 1 )
			{
				var label = $"{_controller.SelectedCount} selected";
				Paint.SetFont( Theme.DefaultFont, 10, 600 );
				Paint.SetPen( new Color( 0.20f, 0.55f, 1.0f, 1.0f ) );
				Paint.ClearBrush();
				Paint.DrawText( new Rect( panelRect.Left + 8, panelRect.Top + 8, 200, 16 ), label, TextFlag.LeftTop );
			}
		}

		// Marquee
		if ( MarqueeRect.HasValue )
		{
			Paint.SetPen( new Color( 0.20f, 0.55f, 1.0f, 0.9f ), 1f );
			Paint.SetBrush( new Color( 0.20f, 0.55f, 1.0f, 0.15f ) );
			Paint.DrawRect( MarqueeRect.Value );
		}

		// Drop-target highlight (T4): when a palette item is being dragged
		// over the canvas, outline the container that will receive the drop.
		if ( _dropHoverContainer != null
			&& _solver.TryGetRect( _dropHoverContainer.Id, out var dropRect ) )
		{
			var widgetRect = LogicalRectToWidget( dropRect );
			Paint.SetPen( new Color( 0.30f, 0.90f, 0.45f, 0.95f ), 2f );
			Paint.SetBrush( new Color( 0.30f, 0.90f, 0.45f, 0.10f ) );
			Paint.DrawRect( widgetRect );
		}
	}

	private static void DrawSecondarySelectionBorder( Rect rect )
	{
		// Slightly dimmer + no handles for non-primary multi-select.
		Paint.SetPen( new Color( 0.20f, 0.55f, 1.0f, 0.85f ), 1f );
		Paint.ClearBrush();
		Paint.DrawRect( rect );
	}

	private void DrawSelectionChrome( Rect rect, SuiElement el )
	{
		// M14 refined: thinner 1px border + smaller 6×6 handles + dimmer accent.
		var borderColor = new Color( 0.30f, 0.65f, 1.0f, 0.95f );
		Paint.SetPen( borderColor, 1f );
		Paint.ClearBrush();
		Paint.DrawRect( rect );

		var isFlex = el.Layout.Mode == SuiLayoutMode.Flex;
		if ( isFlex )
		{
			// Mini badge on top-left corner instead of full overlay text.
			var badgeRect = new Rect( rect.Left, rect.Top - 14, 56, 13 );
			Paint.SetBrush( borderColor );
			Paint.ClearPen();
			Paint.DrawRect( badgeRect, 2 );
			Paint.SetFont( Theme.DefaultFont, 8, 700 );
			Paint.SetPen( Color.White );
			Paint.DrawText( badgeRect, "FLEX", TextFlag.Center );
			return;
		}

		// 8 handles — smaller 6×6 squares.
		var handlePoints = new[]
		{
			new Vector2( rect.Left,  rect.Top ),
			new Vector2( (rect.Left + rect.Right) * 0.5f, rect.Top ),
			new Vector2( rect.Right, rect.Top ),
			new Vector2( rect.Left,  (rect.Top + rect.Bottom) * 0.5f ),
			new Vector2( rect.Right, (rect.Top + rect.Bottom) * 0.5f ),
			new Vector2( rect.Left,  rect.Bottom ),
			new Vector2( (rect.Left + rect.Right) * 0.5f, rect.Bottom ),
			new Vector2( rect.Right, rect.Bottom ),
		};
		Paint.SetPen( Color.White, 1f );
		Paint.SetBrush( borderColor );
		foreach ( var p in handlePoints )
			Paint.DrawRect( new Rect( p.x - 3f, p.y - 3f, 6f, 6f ), 1 );
	}

	internal Rect LogicalRectToWidget( Rect logical )
	{
		var tl = LogicalToWidget( new Vector2( logical.Left, logical.Top ) );
		var br = LogicalToWidget( new Vector2( logical.Right, logical.Bottom ) );
		return new Rect( tl.x, tl.y, br.x - tl.x, br.y - tl.y );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Mouse events — viewport handles pan locally; forwards selection /
	//  drag / resize to the parent canvas via the *Handler delegates.
	// ─────────────────────────────────────────────────────────────────────

	private bool _panning;
	private Vector2 _panLastWidget;

	protected override void OnMousePress( MouseEvent e )
	{
		// Banner click takes precedence over everything (covers top of canvas).
		if ( e.LeftMouseButton && TryHandleBannerClick( e.LocalPosition ) )
		{
			e.Accepted = true;
			return;
		}

		// Middle-mouse OR Alt+left = pan. Match Unity UI Builder + UMG conventions.
		var alt = (e.KeyboardModifiers & KeyboardModifiers.Alt) != 0;
		if ( e.MiddleMouseButton || (e.LeftMouseButton && alt) )
		{
			_panning = true;
			_panLastWidget = e.LocalPosition;
			Cursor = CursorShape.SizeAll;
			e.Accepted = true;
			return;
		}

		if ( MousePressHandler != null && MousePressHandler( e ) )
		{
			e.Accepted = true;
			Update();
			return;
		}
		base.OnMousePress( e );
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		if ( _panning )
		{
			var delta = e.LocalPosition - _panLastWidget;
			_panLastWidget = e.LocalPosition;
			_pan += delta;
			PersistView();
			Invalidate();
			e.Accepted = true;
			return;
		}

		MouseMoveHandler?.Invoke( e );
		base.OnMouseMove( e );
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		if ( _panning )
		{
			_panning = false;
			Cursor = CursorShape.Arrow;
			e.Accepted = true;
			return;
		}

		MouseReleaseHandler?.Invoke( e );
		base.OnMouseReleased( e );
	}

	protected override void OnMouseWheel( WheelEvent e )
	{
		// Zoom anchored at cursor.
		var step = e.Delta > 0 ? 1.1f : (1f / 1.1f);
		SetZoom( _zoom * step, e.Position );
		e.Accepted = true;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Drop target — palette element drop here adds element via controller.
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Element under cursor during a drag-over (used to highlight the
	/// container that will receive the drop). Cleared on leave/drop.
	/// </summary>
	private SuiElement _dropHoverContainer;

	public override void OnDragHover( DragEvent ev )
	{
		base.OnDragHover( ev );
		if ( ev.Data?.Object is not SuiElementType ) return;
		ev.Action = DropAction.Copy;

		// Resolve container under cursor for visual feedback.
		_dropHoverContainer = ResolveDropContainer( WidgetToLogical( ev.LocalPosition ) );
		Update();
	}

	public override void OnDragLeave()
	{
		base.OnDragLeave();
		_dropHoverContainer = null;
		Update();
	}

	public override void OnDragDrop( DragEvent ev )
	{
		base.OnDragDrop( ev );
		_dropHoverContainer = null;

		if ( _document == null || _controller == null ) return;
		if ( ev.Data?.Object is not SuiElementType type ) return;

		var logical = WidgetToLogical( ev.LocalPosition );
		var parent = ResolveDropContainer( logical ) ?? _document.GetRoot();
		if ( parent == null ) return;

		var added = _controller.AddElement( type, parent );
		if ( added != null && added.Layout?.Mode == SuiLayoutMode.Absolute && parent != null )
		{
			// Position the added element at the drop point relative to its parent.
			// Anchor stays at the default (TopLeft) — drop point becomes top-left.
			if ( _solver.TryGetRect( parent.Id, out var pRect ) )
			{
				var localX = MathF.Round( logical.x - pRect.Left );
				var localY = MathF.Round( logical.y - pRect.Top );
				_controller.MoveElement( added, localX, localY );
			}
		}
		ev.Action = DropAction.Copy;
		Update();
	}

	/// <summary>
	/// Resolve the container element under <paramref name="logical"/>: deepest
	/// container element whose rect contains the point. Falls back to root.
	/// </summary>
	private SuiElement ResolveDropContainer( Vector2 logical )
	{
		if ( _document == null ) return null;
		SuiElement best = null;
		foreach ( var el in _document.Elements )
		{
			if ( !IsContainerType( el.Type ) ) continue;
			if ( !_solver.TryGetRect( el.Id, out var rect ) ) continue;
			if ( logical.x < rect.Left || logical.x > rect.Right ) continue;
			if ( logical.y < rect.Top || logical.y > rect.Bottom ) continue;
			best = el; // document order = parent-then-child, so deeper container wins
		}
		return best ?? _document.GetRoot();
	}

	private static bool IsContainerType( SuiElementType type ) => type switch
	{
		SuiElementType.Canvas
			or SuiElementType.Panel
			or SuiElementType.Overlay
			or SuiElementType.HorizontalBox
			or SuiElementType.VerticalBox
			or SuiElementType.Grid
			or SuiElementType.ScrollPanel
			or SuiElementType.InventoryGrid
			or SuiElementType.Hotbar => true,
		_ => false,
	};
}
