using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;
using SboxUiDesigner.EditorUi.Canvas;
using SboxUiDesigner.EditorUi.Commands;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Canvas dock — the visual designer's centerpiece.
///
/// As of the canvas redesign (PRD doc 15), this widget is a thin shell around
/// <see cref="SuiCanvasViewport"/> (paint-based 2D renderer of the document).
/// It also hosts the inline canvas toolbar (zoom dropdown, resolution picker,
/// snap toggle, preview button — Phase 4).
///
/// The runtime preview that used to live here is now an opt-in modal opened
/// via the main toolbar's "Test in Play" button (real Play mode preview).
/// The reflection-driven SuiPreviewHost machinery is preserved for that flow
/// but is no longer in the edit hot path.
///
/// Mouse interaction (Phase 2) is handled here: hit-test reads from the
/// viewport's <see cref="SuiLayoutSolver"/> dict, drag/resize update element
/// layout values directly during the live drag and emit a single command on
/// release for clean undo.
/// </summary>
public class SuiCanvasWidget : Widget
{
	private SuiDocument _document;
	private SuiDesignerController _controller;
	private SuiCanvasViewport _viewport;

	// ─── Drag state ─────────────────────────────────────────────────────
	// _dragElement is the "primary" being grabbed. Other elements in the
	// SelectedSet move/resize together via _groupDrag entries.
	private SuiElement _dragElement;
	private DragMode _dragMode = DragMode.None;
	private Vector2 _dragStartLogical;
	private float _dragStartX, _dragStartY, _dragStartW, _dragStartH;
	// Captured at BeginDrag: the element's resolved logical rect (where it
	// renders on the canvas) and its parent's rect. Used to compute the new
	// rect after the drag delta, then inverted via RectToLayoutValues so
	// X/Y/W/H are correct for ANY anchor (BottomCenter, TopRight, etc.).
	// Without this, drag math was anchor-agnostic and inverted for Bottom*/
	// Right* anchors.
	private Rect _dragStartRect;
	private Rect _dragStartParentRect;
	private const float DragStartThresholdPx = 4f;
	private bool _dragHasStarted;

	private readonly List<GroupDragEntry> _groupDrag = new();
	private struct GroupDragEntry
	{
		public SuiElement Element;
		public float StartX, StartY, StartW, StartH;
		// Start rect (logical space) + parent rect — same anchor-aware drag
		// pattern as the primary element. Each group member can have its own
		// anchor, so storing per-member is required.
		public Rect StartRect;
		public Rect ParentRect;
	}

	// Marquee
	private Vector2? _marqueeStartWidget;

	private enum DragMode
	{
		None,
		Move,
		ResizeNW, ResizeN, ResizeNE,
		ResizeW, ResizeE,
		ResizeSW, ResizeS, ResizeSE,
	}

	private Label _statusBar;

	public SuiCanvasWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Canvas";
		Name = "SuiCanvas";
		MinimumSize = new Vector2( 400, 300 );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		_viewport = new SuiCanvasViewport( this );
		_viewport.SetSizeMode( SizeMode.CanGrow, SizeMode.CanGrow );
		_viewport.MousePressHandler = HandleMousePress;
		_viewport.MouseMoveHandler = HandleMouseMove;
		_viewport.MouseReleaseHandler = HandleMouseRelease;

		// Old SuiCanvasToolbar removed in M14 v3 — its Screen/Zoom/Snap
		// controls now live in SuiCanvasMiniToolbar (sibling above the
		// canvas, owned by SuiCenterTabsWidget).

		Layout.Add( _viewport, 1 );

		// Status bar at the bottom — shows selection summary.
		// Background is #1E1E1F (rgba 30,30,31) per user spec — different from
		// the bottom panel's bg so the canvas reads as a closed box.
		_statusBar = new Label( "", this );
		_statusBar.SetStyles( "padding: 4px 8px; color: rgb(156,163,175); font-size: 11px; background-color: rgb(30,30,31); border: none;" );
		_statusBar.FixedHeight = 22;
		Layout.Add( _statusBar );
		RefreshStatusBar();
	}

	/// <summary>
	/// Public access to the viewport — used by Window menu handlers (Zoom In/Out,
	/// Fit to Screen) to drive zoom/pan from outside.
	/// </summary>
	public SuiCanvasViewport GetViewport() => _viewport;

	public void SetController( SuiDesignerController controller )
	{
		if ( _controller != null )
			_controller.SelectionChanged -= OnControllerSelectionChanged;

		_controller = controller;
		_viewport?.SetController( controller );

		if ( _controller != null )
			_controller.SelectionChanged += OnControllerSelectionChanged;
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		_viewport?.SetDocument( document );
		RefreshStatusBar();
	}

	private void OnControllerSelectionChanged()
	{
		_viewport?.Invalidate();
		RefreshStatusBar();
	}

	private void RefreshStatusBar()
	{
		// Underlying QLabel may already be destroyed if this widget was hot-reloaded
		// while still subscribed to controller events. Guard with IsValid() —
		// otherwise Label.Text setter throws "QLabel was null when calling setText",
		// which propagates up and breaks anything calling RefreshStatusBar
		// (notably the drag-release path).
		if ( !_statusBar.IsValid() ) return;
		if ( _controller == null || _controller.Selected == null )
		{
			_statusBar.Text = "Nothing selected";
			return;
		}

		if ( _controller.SelectedCount > 1 )
		{
			_statusBar.Text = $"{_controller.SelectedCount} elements selected";
			return;
		}

		var el = _controller.Selected;
		var l = el.Layout;
		_statusBar.Text = $"{el.Name}  ·  {el.Type}  ·  {(int)l.Width}×{(int)l.Height}  @ {(int)l.X},{(int)l.Y}  ·  Anchor: {l.Anchor}";
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Mouse handling — Phase 2
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// True when the element is positioned by its parent's absolute layout
	/// (parent Mode == Absolute, so X/Y/Anchor on this element are honored).
	/// False when the parent is a Flex container — children there are flowed,
	/// dragging them does nothing because the parent re-computes their pos.
	///
	/// Replaces the older check (<c>el.Layout.Mode == Absolute</c>) which was
	/// looking at the WRONG side: an element's own Mode controls how IT lays
	/// out its CHILDREN, but its OWN positioning depends on the PARENT.
	/// </summary>
	private bool IsPositionedByParent( SuiElement el )
	{
		if ( el == null || _document == null ) return false;
		if ( string.IsNullOrEmpty( el.ParentId ) ) return false; // root — not draggable anyway
		var parent = _document.GetElement( el.ParentId );
		var parentMode = parent?.Layout?.Mode ?? SuiLayoutMode.Absolute;
		return parentMode == SuiLayoutMode.Absolute;
	}

	private bool HandleMousePress( MouseEvent e )
	{
		if ( _document == null || _viewport == null ) return false;

		var solver = _viewport.Solver;
		var logical = _viewport.WidgetToLogical( e.LocalPosition );
		var shift = (e.KeyboardModifiers & KeyboardModifiers.Shift) != 0;

		// 1) Resize handle on the primary selected (handles only on primary —
		// you can't drag handles when N>1 elements are selected, it's ambiguous).
		var primary = _controller?.Selected;
		if ( primary != null && !string.IsNullOrEmpty( primary.ParentId )
			&& solver.TryGetRect( primary.Id, out var primRect )
			&& _controller.SelectedCount == 1 )
		{
			var handle = HitTestHandle( primRect, logical );
			if ( handle != DragMode.None && IsPositionedByParent( primary ) )
			{
				BeginDrag( primary, handle, logical );
				return true;
			}
		}

		// 2) Click on any element body — pick it.
		var hit = HitTestElement( logical );
		if ( hit != null && !string.IsNullOrEmpty( hit.ParentId ) )
		{
			if ( shift )
			{
				// Shift+click toggles in/out of selection.
				_controller?.ToggleSelected( hit );
				// If still selected, allow drag (group drag if multi).
				if ( _controller.IsSelected( hit ) && IsPositionedByParent( hit ) )
					BeginDrag( hit, DragMode.Move, logical );
				return true;
			}

			// UMG-style "click-through guard": if the deepest hit is a descendant
			// of the currently-selected element, prefer keeping the ancestor
			// selected and drag IT. The user almost always means "I want to move
			// this panel" when clicking inside it, even if a child happens to be
			// under the cursor. Use Alt+click (handled at viewport-level for pan)
			// or click on empty space then re-click to escape this — V2 may add
			// an explicit "select-through" modifier.
			if ( primary != null && hit != primary && IsAncestorOf( primary, hit ) )
			{
				if ( IsPositionedByParent( primary )
					&& solver.TryGetRect( primary.Id, out var prect )
					&& PointInRect( logical, prect ) )
				{
					BeginDrag( primary, DragMode.Move, logical );
					return true;
				}
			}

			// Plain click. If the clicked element is ALREADY in a multi-selection,
			// don't reset — start a group drag. Otherwise replace selection with it.
			if ( _controller != null && _controller.IsSelected( hit ) && _controller.SelectedCount > 1 )
			{
				if ( IsPositionedByParent( hit ) )
					BeginDrag( hit, DragMode.Move, logical );
				return true;
			}

			_controller?.SetSelected( hit );
			if ( IsPositionedByParent( hit ) )
				BeginDrag( hit, DragMode.Move, logical );
			return true;
		}

		// 3) Empty canvas. Shift+drag = additive marquee (don't clear current).
		// Plain click on empty = clear selection + start marquee.
		if ( !shift ) _controller?.ClearSelection();
		_marqueeStartWidget = e.LocalPosition;
		return true;
	}

	private void HandleMouseMove( MouseEvent e )
	{
		if ( _viewport == null ) return;
		var solver = _viewport.Solver;

		// Update hover indicator.
		if ( _dragElement == null && _marqueeStartWidget == null )
		{
			var logical = _viewport.WidgetToLogical( e.LocalPosition );
			var hovered = HitTestElement( logical );
			if ( hovered != _viewport.HoverElement )
			{
				_viewport.HoverElement = hovered;
				_viewport.Invalidate();
			}
		}

		// Marquee drag.
		if ( _marqueeStartWidget.HasValue )
		{
			var start = _marqueeStartWidget.Value;
			var current = e.LocalPosition;
			var x = MathF.Min( start.x, current.x );
			var y = MathF.Min( start.y, current.y );
			var w = MathF.Abs( current.x - start.x );
			var h = MathF.Abs( current.y - start.y );
			_viewport.MarqueeRect = new Rect( x, y, w, h );
			_viewport.Invalidate();
			return;
		}

		// Pending-then-confirmed drag.
		if ( _dragElement != null )
		{
			var logical = _viewport.WidgetToLogical( e.LocalPosition );
			var dx = logical.x - _dragStartLogical.x;
			var dy = logical.y - _dragStartLogical.y;

			if ( !_dragHasStarted )
			{
				// Convert the delta to widget pixels for threshold check.
				var px = (logical - _dragStartLogical) * _viewport.Zoom;
				if ( px.Length < DragStartThresholdPx ) return;
				_dragHasStarted = true;
			}

			// Modifier keys
			var shift = (e.KeyboardModifiers & KeyboardModifiers.Shift) != 0;
			var ctrl = (e.KeyboardModifiers & KeyboardModifiers.Ctrl) != 0;
			var alt = (e.KeyboardModifiers & KeyboardModifiers.Alt) != 0;

			ApplyDragLive( dx, dy, shift, ctrl, alt );
			_viewport.Invalidate();
		}
	}

	private void HandleMouseRelease( MouseEvent e )
	{
		// Marquee end — pick ALL elements whose rects intersect the marquee.
		if ( _marqueeStartWidget.HasValue )
		{
			var rect = _viewport.MarqueeRect;
			_viewport.MarqueeRect = null;
			_marqueeStartWidget = null;

			var shift = (e.KeyboardModifiers & KeyboardModifiers.Shift) != 0;

			if ( rect.HasValue && rect.Value.Width > 4 && rect.Value.Height > 4 )
			{
				// Convert marquee from widget pixels to logical pixels.
				var topLeft = _viewport.WidgetToLogical( new Vector2( rect.Value.Left, rect.Value.Top ) );
				var bottomRight = _viewport.WidgetToLogical( new Vector2( rect.Value.Right, rect.Value.Bottom ) );
				var marqueeLogical = new Rect( topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y );

				var hits = HitTestMarqueeAll( marqueeLogical );
				if ( shift && _controller != null )
				{
					// Additive marquee (Shift held) — keep current + union with hits.
					var combined = new HashSet<SuiElement>( _controller.SelectedSet );
					foreach ( var h in hits ) combined.Add( h );
					_controller.SetSelection( combined );
				}
				else
				{
					_controller?.SetSelection( hits );
				}
			}
			_viewport.Invalidate();
			return;
		}

		// Drag end — emit command if delta non-zero.
		// Wrap CommitDrag in try/finally: any exception in the command/refresh
		// chain (e.g. a destroyed QLabel side-effect) must NOT leave _dragElement
		// alive — otherwise the next OnMouseMove keeps dragging the element
		// after the button is already released. See SUI-DRAG diagnostic logs.
		if ( _dragElement != null )
		{
			try
			{
				CommitDrag();
			}
			finally
			{
				_dragElement = null;
				_dragMode = DragMode.None;
				_dragHasStarted = false;
				_groupDrag.Clear();
				_viewport.Invalidate();
			}
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Hit-test
	// ─────────────────────────────────────────────────────────────────────

	private SuiElement HitTestElement( Vector2 logical )
	{
		if ( _document == null || _viewport == null ) return null;
		var solver = _viewport.Solver;
		// Walk the tree in PAINT ORDER (children sorted by ZIndex per parent).
		// "Last hit wins" along that walk → mirrors the renderer exactly, so
		// the element the user sees on top is the one that gets selected.
		SuiElement best = null;
		var root = _document.GetRoot();
		if ( root != null )
			HitTestWalk( root, solver, logical, ref best );
		return best;
	}

	private void HitTestWalk( SuiElement parent, SuiLayoutSolver solver, Vector2 logical, ref SuiElement best )
	{
		var ordered = SuiLayoutSolver.GetRenderOrderedChildren( parent, solver.ById );
		foreach ( var el in ordered )
		{
			if ( el.Flags?.HiddenInDesigner == true ) continue;
			// LOCK: locked elements skip themselves AND their entire subtree —
			// click passes through to elements behind/beside, exactly like
			// CSS pointer-events: none for the whole branch.
			if ( el.Flags?.Locked == true ) continue;
			if ( !solver.TryGetRect( el.Id, out var rect ) ) continue;
			if ( PointInRect( logical, rect ) ) best = el;
			HitTestWalk( el, solver, logical, ref best );
		}
	}

	/// <summary>
	/// Find every element whose rect intersects the marquee. Skips Root,
	/// hidden-in-designer, and any element with no computed rect. Used by
	/// marquee multi-select. Walks in render order so the returned list
	/// mirrors visual stacking when the caller iterates it.
	/// </summary>
	private List<SuiElement> HitTestMarqueeAll( Rect marqueeLogical )
	{
		var hits = new List<SuiElement>();
		if ( _document == null || _viewport == null ) return hits;
		var solver = _viewport.Solver;
		var root = _document.GetRoot();
		if ( root != null )
			MarqueeWalk( root, solver, marqueeLogical, hits );
		return hits;
	}

	private void MarqueeWalk( SuiElement parent, SuiLayoutSolver solver, Rect marqueeLogical, List<SuiElement> hits )
	{
		var ordered = SuiLayoutSolver.GetRenderOrderedChildren( parent, solver.ById );
		foreach ( var el in ordered )
		{
			if ( el.Flags?.HiddenInDesigner == true ) continue;
			if ( el.Flags?.Locked == true ) continue; // marquee respects lock too
			if ( solver.TryGetRect( el.Id, out var rect ) && RectsIntersect( marqueeLogical, rect ) )
				hits.Add( el );
			MarqueeWalk( el, solver, marqueeLogical, hits );
		}
	}

	private static bool RectsIntersect( Rect a, Rect b )
	{
		return a.Left < b.Right && a.Right > b.Left
			&& a.Top < b.Bottom && a.Bottom > b.Top;
	}

	/// <summary>
	/// True if <paramref name="ancestor"/> is a direct or indirect parent of
	/// <paramref name="descendant"/> in the document tree.
	/// </summary>
	private bool IsAncestorOf( SuiElement ancestor, SuiElement descendant )
	{
		if ( ancestor == null || descendant == null || _document == null ) return false;
		var safety = 1024;
		var currentId = descendant.ParentId;
		while ( !string.IsNullOrEmpty( currentId ) && --safety > 0 )
		{
			if ( currentId == ancestor.Id ) return true;
			var current = _document.GetElement( currentId );
			if ( current == null ) return false;
			currentId = current.ParentId;
		}
		return false;
	}

	private DragMode HitTestHandle( Rect rect, Vector2 logical )
	{
		// Handle hit-radius in logical pixels — convert from a screen-pixel radius.
		var radius = 8f / _viewport.Zoom;
		if ( radius < 4f ) radius = 4f;

		var midX = (rect.Left + rect.Right) * 0.5f;
		var midY = (rect.Top + rect.Bottom) * 0.5f;
		var corners = new (DragMode mode, Vector2 pos)[]
		{
			(DragMode.ResizeNW, new Vector2( rect.Left,  rect.Top )),
			(DragMode.ResizeN,  new Vector2( midX,        rect.Top )),
			(DragMode.ResizeNE, new Vector2( rect.Right, rect.Top )),
			(DragMode.ResizeW,  new Vector2( rect.Left,  midY )),
			(DragMode.ResizeE,  new Vector2( rect.Right, midY )),
			(DragMode.ResizeSW, new Vector2( rect.Left,  rect.Bottom )),
			(DragMode.ResizeS,  new Vector2( midX,        rect.Bottom )),
			(DragMode.ResizeSE, new Vector2( rect.Right, rect.Bottom )),
		};
		foreach ( var (mode, pos) in corners )
		{
			if ( (logical - pos).Length < radius ) return mode;
		}
		return DragMode.None;
	}

	private static bool PointInRect( Vector2 p, Rect r )
		=> p.x >= r.Left && p.x <= r.Right && p.y >= r.Top && p.y <= r.Bottom;

	// ─────────────────────────────────────────────────────────────────────
	//  Drag / resize
	// ─────────────────────────────────────────────────────────────────────

	private void BeginDrag( SuiElement el, DragMode mode, Vector2 logical )
	{
		_dragElement = el;
		_dragMode = mode;
		_dragStartLogical = logical;
		_dragStartX = el.Layout.X;
		_dragStartY = el.Layout.Y;
		_dragStartW = el.Layout.Width > 0 ? el.Layout.Width : 100;
		_dragStartH = el.Layout.Height > 0 ? el.Layout.Height : 32;
		_dragHasStarted = false;

		// Snapshot the resolved rects so drag math can work in logical-rect
		// space (anchor-agnostic) and reverse to X/Y via RectToLayoutValues.
		_dragStartRect = default;
		_dragStartParentRect = default;
		var solver = _viewport?.Solver;
		if ( solver != null )
		{
			solver.TryGetRect( el.Id, out _dragStartRect );
			if ( !string.IsNullOrEmpty( el.ParentId ) )
				solver.TryGetRect( el.ParentId, out _dragStartParentRect );
		}

		// Group drag — every other parent-positioned selected element snapshots
		// its start so it moves in lockstep. Group drag is move-only; resize via
		// handle stays single-element (handles only show on the primary).
		// Filter uses IsPositionedByParent (parent.Mode == Absolute), not the
		// element's own Mode — a Flex container under an Absolute parent IS
		// draggable; a child of a Flex parent is NOT.
		_groupDrag.Clear();
		if ( mode == DragMode.Move && _controller != null && _controller.SelectedCount > 1 )
		{
			foreach ( var other in _controller.SelectedSet )
			{
				if ( other == el ) continue;
				if ( other == null ) continue;
				if ( string.IsNullOrEmpty( other.ParentId ) ) continue;
				if ( other.Layout == null ) continue;
				if ( !IsPositionedByParent( other ) ) continue;

				var entry = new GroupDragEntry
				{
					Element = other,
					StartX = other.Layout.X,
					StartY = other.Layout.Y,
					StartW = other.Layout.Width,
					StartH = other.Layout.Height,
				};
				var solverRef = _viewport?.Solver;
				if ( solverRef != null )
				{
					solverRef.TryGetRect( other.Id, out entry.StartRect );
					solverRef.TryGetRect( other.ParentId, out entry.ParentRect );
				}
				_groupDrag.Add( entry );
			}
		}
	}

	private void ApplyDragLive( float dx, float dy, bool shift, bool ctrl, bool alt )
	{
		if ( _dragElement == null ) return;
		var el = _dragElement;

		// For non-Move modes that change W/H, signs of W/H growth depend on
		// which handle was grabbed. We mutate startX/Y to keep top-left fixed
		// when only the opposite side moves.
		float x = _dragStartX, y = _dragStartY, w = _dragStartW, h = _dragStartH;

		switch ( _dragMode )
		{
			case DragMode.Move:
				if ( shift )
				{
					// Lock to dominant axis.
					if ( MathF.Abs( dx ) > MathF.Abs( dy ) ) dy = 0;
					else dx = 0;
				}
				// Anchor-aware move: translate the START RECT in logical space,
				// then invert via RectToLayoutValues so X/Y mean the right thing
				// regardless of anchor. Without this, BottomCenter/TopRight etc.
				// drag in inverted directions (move down → element goes up).
				if ( _dragStartRect.Width > 0 && _dragStartParentRect.Width > 0 )
				{
					var newRect = new Rect(
						_dragStartRect.Left + dx,
						_dragStartRect.Top + dy,
						_dragStartRect.Width,
						_dragStartRect.Height );
					var anchor = el.Layout?.Anchor ?? SuiAnchor.TopLeft;
					var (nx, ny, nw, nh) = SuiLayoutSolver.RectToLayoutValues( newRect, anchor, _dragStartParentRect );
					x = nx;
					y = ny;
					// Don't touch w/h on Move (RectToLayoutValues passes them through
					// for non-Stretch anchors; for Stretch they become margins which
					// is the correct semantic — Stretch drag updates margins).
					w = nw;
					h = nh;
				}
				else
				{
					// Fallback when solver hasn't filled rects yet (very early frame
					// after BeginDrag) — match the pre-fix behavior so the element
					// at least responds, even if direction is off for non-TopLeft.
					x = _dragStartX + dx;
					y = _dragStartY + dy;
				}
				break;
			case DragMode.ResizeE: w = MathF.Max( 1, _dragStartW + dx ); break;
			case DragMode.ResizeW:
				w = MathF.Max( 1, _dragStartW - dx );
				x = _dragStartX + (_dragStartW - w);
				break;
			case DragMode.ResizeS: h = MathF.Max( 1, _dragStartH + dy ); break;
			case DragMode.ResizeN:
				h = MathF.Max( 1, _dragStartH - dy );
				y = _dragStartY + (_dragStartH - h);
				break;
			case DragMode.ResizeSE:
				w = MathF.Max( 1, _dragStartW + dx );
				h = MathF.Max( 1, _dragStartH + dy );
				if ( ctrl ) { var aspect = _dragStartW / _dragStartH; h = w / aspect; }
				break;
			case DragMode.ResizeSW:
				w = MathF.Max( 1, _dragStartW - dx );
				h = MathF.Max( 1, _dragStartH + dy );
				x = _dragStartX + (_dragStartW - w);
				if ( ctrl ) { var aspect = _dragStartW / _dragStartH; h = w / aspect; }
				break;
			case DragMode.ResizeNE:
				w = MathF.Max( 1, _dragStartW + dx );
				h = MathF.Max( 1, _dragStartH - dy );
				y = _dragStartY + (_dragStartH - h);
				if ( ctrl ) { var aspect = _dragStartW / _dragStartH; h = w / aspect; }
				break;
			case DragMode.ResizeNW:
				w = MathF.Max( 1, _dragStartW - dx );
				h = MathF.Max( 1, _dragStartH - dy );
				x = _dragStartX + (_dragStartW - w);
				y = _dragStartY + (_dragStartH - h);
				if ( ctrl ) { var aspect = _dragStartW / _dragStartH; h = w / aspect; }
				break;
		}

		// Snap-to-grid when enabled (and not pressing Alt — Alt = "snap off" convention).
		var gridSize = _document?.Settings?.GridSize ?? 0;
		var snap = (_document?.Settings?.SnapToGrid ?? false) && !alt && gridSize > 0;
		if ( snap )
		{
			x = MathF.Round( x / gridSize ) * gridSize;
			y = MathF.Round( y / gridSize ) * gridSize;
			w = MathF.Round( w / gridSize ) * gridSize;
			h = MathF.Round( h / gridSize ) * gridSize;
			if ( w < 1 ) w = 1;
			if ( h < 1 ) h = 1;
		}

		// Live-mutate (no command yet — that goes on release).
		el.Layout.X = x;
		el.Layout.Y = y;
		el.Layout.Width = w;
		el.Layout.Height = h;

		// Group drag (move only) — every other selected parent-positioned
		// element shifts by the same LOGICAL-SPACE delta (dx,dy). Each member
		// converts that delta to its OWN (X,Y) using its anchor + parent rect
		// via RectToLayoutValues, so a TopLeft and a BottomCenter in the same
		// selection both move down visually when the user drags down.
		if ( _dragMode == DragMode.Move && _groupDrag.Count > 0 )
		{
			foreach ( var g in _groupDrag )
			{
				float gx, gy;
				if ( g.StartRect.Width > 0 && g.ParentRect.Width > 0 )
				{
					var newRect = new Rect(
						g.StartRect.Left + dx,
						g.StartRect.Top + dy,
						g.StartRect.Width,
						g.StartRect.Height );
					var anchor = g.Element.Layout?.Anchor ?? SuiAnchor.TopLeft;
					var (nx, ny, _, _) = SuiLayoutSolver.RectToLayoutValues( newRect, anchor, g.ParentRect );
					gx = nx;
					gy = ny;
				}
				else
				{
					gx = g.StartX + dx;
					gy = g.StartY + dy;
				}
				if ( snap )
				{
					gx = MathF.Round( gx / gridSize ) * gridSize;
					gy = MathF.Round( gy / gridSize ) * gridSize;
				}
				g.Element.Layout.X = gx;
				g.Element.Layout.Y = gy;
			}
		}
	}

	private void CommitDrag()
	{
		if ( _dragElement == null || _controller == null ) return;
		if ( !_dragHasStarted ) { _groupDrag.Clear(); return; }

		var el = _dragElement;
		var endX = el.Layout.X;
		var endY = el.Layout.Y;
		var endW = el.Layout.Width;
		var endH = el.Layout.Height;

		// Snapshot group ends BEFORE rollback (we mutated them live).
		var groupEnds = new List<(SuiElement el, float x, float y)>( _groupDrag.Count );
		foreach ( var g in _groupDrag )
		{
			groupEnds.Add( (g.Element, g.Element.Layout.X, g.Element.Layout.Y) );
		}

		// Roll back primary so the command's Apply re-applies final values
		// (one-step undo restores pre-drag).
		el.Layout.X = _dragStartX;
		el.Layout.Y = _dragStartY;
		el.Layout.Width = _dragStartW;
		el.Layout.Height = _dragStartH;

		// Roll back the group entries too.
		foreach ( var g in _groupDrag )
		{
			g.Element.Layout.X = g.StartX;
			g.Element.Layout.Y = g.StartY;
			g.Element.Layout.Width = g.StartW;
			g.Element.Layout.Height = g.StartH;
		}

		var movedXY = !FloatEq( _dragStartX, endX ) || !FloatEq( _dragStartY, endY );
		var resized = !FloatEq( _dragStartW, endW ) || !FloatEq( _dragStartH, endH );

		if ( movedXY )
			_controller.MoveElement( el, endX, endY );
		if ( resized )
			_controller.ResizeElement( el, endW, endH );

		// Group: emit a Move command per peer. Each command is independent so
		// undo will revert them in sequence (acceptable for V1 — V2 could group
		// these into a single composite command for atomic undo).
		foreach ( var (other, gx, gy) in groupEnds )
		{
			if ( !FloatEq( gx, other.Layout.X ) || !FloatEq( gy, other.Layout.Y ) )
				_controller.MoveElement( other, gx, gy );
		}

		_groupDrag.Clear();
	}

	private static bool FloatEq( float a, float b ) => MathF.Abs( a - b ) < 0.01f;
}
