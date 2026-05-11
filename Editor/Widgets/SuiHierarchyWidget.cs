using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Hierarchy panel — left side, below palette. Renders the document tree with
/// 100% custom paint: tree-lines connecting parents to children, type icon,
/// element name, and right-side eye (designer visibility) + lock (no-edit)
/// buttons.
///
/// Eye / lock are designer-only:
/// - Eye toggles whether the element is rendered in the canvas. Code
///   generation and runtime preview ignore this flag entirely.
/// - Lock prevents the element from being clicked in the canvas and from
///   having its properties edited in Details. Code/runtime ignore it too.
/// </summary>
public class SuiHierarchyWidget : Widget
{
	private SuiDocument _document;
	private SuiElement _selected;
	private HashSet<string> _selectedIds = new();

	private LineEdit _search;
	private string _filter = "";

	private SuiTreeView _tree;

	public event Action<SuiElement> ElementSelected;
	public event Action<SuiElement, SuiElementType> AddChildRequested;
	public event Action<SuiElement, string> RenameRequested;
	public event Action<SuiElement> DeleteRequested;
	public event Action<SuiElement> DuplicateRequested;
	public event Action<SuiElement> MoveUpRequested;
	public event Action<SuiElement> MoveDownRequested;
	public event Action<SuiElement, SuiElement, int> ReparentRequested;
	public event Action FlagsChanged;

	public SuiHierarchyWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Hierarchy";
		Name = "SuiHierarchy";
		MinimumSize = new Vector2( 200, 200 );
		SetStyles( "background-color: transparent; border: none;" );

		Layout = Layout.Column();
		Layout.Margin = new Sandbox.UI.Margin( 8, 8, 8, 8 );
		Layout.Spacing = 0;

		// Search input — same look as Palette.
		_search = new LineEdit( this );
		_search.PlaceholderText = "Search Hierarchy";
		_search.FixedHeight = 28;
		_search.SetStyles(
			"background-color: rgb(20,20,19);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 3px;" +
			"color: rgb(220,224,230);" +
			"padding: 0 8px;" +
			"font-size: 11px;" );
		_search.TextEdited += s =>
		{
			_filter = (s ?? "").Trim().ToLowerInvariant();
			_tree?.SetFilter( _filter );
		};
		Layout.Add( _search );

		// 8px gap between search and tree.
		var spc = new Widget( this ) { FixedHeight = 8 };
		spc.SetStyles( "background-color: transparent; border: none;" );
		Layout.Add( spc );

		// ScrollArea + plain Widget canvas — same shape as the Palette which
		// scrolls correctly. SuiTreeView is no longer a Widget itself; it's a
		// state holder that adds rows directly to this canvas.
		var scroll = new ScrollArea( this );
		SuiScrollStyle.ApplyTo( scroll );
		_scrollContent = new Widget( scroll );
		_scrollContent.SetStyles( "background-color: transparent; border: none;" );
		_scrollContent.Layout = Layout.Column();
		_scrollContent.Layout.Margin = 0;
		_scrollContent.Layout.Spacing = 0;
		scroll.Canvas = _scrollContent;
		Layout.Add( scroll, 1 );

		_tree = new SuiTreeView( _scrollContent );
		_tree.OnElementSelected = el =>
		{
			_selected = el;
			ElementSelected?.Invoke( el );
		};
		_tree.OnElementContextMenu = el => ShowContextMenuFor( el );
		_tree.OnElementToggleVisibility = el =>
		{
			if ( el?.Flags == null ) return;
			el.Flags.HiddenInDesigner = !el.Flags.HiddenInDesigner;
			_tree.RebuildIfNeeded();
			FlagsChanged?.Invoke();
		};
		_tree.OnElementToggleLock = el =>
		{
			if ( el?.Flags == null ) return;
			el.Flags.Locked = !el.Flags.Locked;
			_tree.RebuildIfNeeded();
			FlagsChanged?.Invoke();
		};
		_tree.OnElementReparent = ( src, dst, idx ) => ReparentRequested?.Invoke( src, dst, idx );

		Refresh();
	}

	private Widget _scrollContent;

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		_selected = null;
		_tree?.SetDocument( document );
	}

	public void SetSelected( SuiElement element )
	{
		_selected = element;
		_tree?.SetSelectedSet( _selectedIds );
	}

	public void SetSelectedSet( IReadOnlyCollection<SuiElement> elements )
	{
		_selectedIds = new HashSet<string>();
		if ( elements != null )
		{
			foreach ( var e in elements )
			{
				if ( e?.Id != null ) _selectedIds.Add( e.Id );
			}
		}
		_tree?.SetSelectedSet( _selectedIds );
	}

	public void Refresh() => _tree?.RebuildIfNeeded();

	public void BeginRenameSelected()
	{
		// V2: inline rename via custom text editor would go here. For now this
		// is a no-op (rename still works via context menu → Rename).
	}

	private void ShowContextMenuFor( SuiElement element )
	{
		if ( element == null || _document == null ) return;
		_selected = element;
		ElementSelected?.Invoke( element );

		var isContainer = IsContainerType( element.Type );
		var canDelete = !string.IsNullOrEmpty( element.ParentId );

		var siblingIdx = -1;
		var siblingCount = 0;
		if ( !string.IsNullOrEmpty( element.ParentId ) )
		{
			var parent = _document.GetElement( element.ParentId );
			if ( parent != null )
			{
				siblingIdx = parent.Children.IndexOf( element.Id );
				siblingCount = parent.Children.Count;
			}
		}

		var m = new Menu( this );

		var addLabel = isContainer ? "Add Child" : "Add Sibling";
		var addTarget = isContainer ? element : (_document.GetElement( element.ParentId ) ?? element);
		var addMenu = m.AddMenu( addLabel, "add" );
		AddTypeOptions( addMenu, new[] { SuiElementType.Panel, SuiElementType.Text, SuiElementType.Image, SuiElementType.Button }, addTarget );
		addMenu.AddSeparator();
		AddTypeOptions( addMenu, new[] { SuiElementType.HorizontalBox, SuiElementType.VerticalBox, SuiElementType.Grid, SuiElementType.Overlay }, addTarget );
		addMenu.AddSeparator();
		AddTypeOptions( addMenu, new[] { SuiElementType.ProgressBar, SuiElementType.ScrollPanel, SuiElementType.InventoryGrid, SuiElementType.InventorySlot, SuiElementType.ItemIcon, SuiElementType.Tooltip, SuiElementType.Hotbar }, addTarget );

		m.AddSeparator();
		if ( element.Flags != null )
		{
			var visLabel = element.Flags.HiddenInDesigner ? "Show in designer" : "Hide in designer";
			var visIcon = element.Flags.HiddenInDesigner ? "visibility" : "visibility_off";
			m.AddOption( visLabel, visIcon, () =>
			{
				element.Flags.HiddenInDesigner = !element.Flags.HiddenInDesigner;
				_tree.RebuildIfNeeded();
				FlagsChanged?.Invoke();
			} );

			var lockLabel = element.Flags.Locked ? "Unlock" : "Lock";
			var lockIcon = element.Flags.Locked ? "lock_open" : "lock";
			m.AddOption( lockLabel, lockIcon, () =>
			{
				element.Flags.Locked = !element.Flags.Locked;
				_tree.RebuildIfNeeded();
				FlagsChanged?.Invoke();
			} );
			m.AddSeparator();
		}

		var rename = m.AddOption( "Rename", "edit", () => RenameRequested?.Invoke( element, null ) );
		rename.Enabled = canDelete;
		m.AddOption( "Duplicate", "content_copy", () => DuplicateRequested?.Invoke( element ) );

		var moveUp = m.AddOption( "Move Up", "arrow_upward", () => MoveUpRequested?.Invoke( element ) );
		moveUp.Enabled = siblingIdx > 0;
		var moveDown = m.AddOption( "Move Down", "arrow_downward", () => MoveDownRequested?.Invoke( element ) );
		moveDown.Enabled = siblingIdx >= 0 && siblingIdx < siblingCount - 1;

		var del = m.AddOption( "Delete", "delete", () => DeleteRequested?.Invoke( element ) );
		del.Enabled = canDelete;

		m.OpenAtCursor( true );
	}

	private void AddTypeOptions( Menu menu, IEnumerable<SuiElementType> types, SuiElement target )
	{
		foreach ( var type in types )
		{
			var captured = type;
			menu.AddOption( type.ToString(), IconForType( type ), () => AddChildRequested?.Invoke( target, captured ) );
		}
	}

	internal static bool IsContainerType( SuiElementType type ) => type switch
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

	internal static string IconForType( SuiElementType type ) => type switch
	{
		SuiElementType.Canvas => "crop_free",
		SuiElementType.Panel => "crop_square",
		SuiElementType.Overlay => "layers",
		SuiElementType.Text => "title",
		SuiElementType.Image => "image",
		SuiElementType.Button => "smart_button",
		SuiElementType.HorizontalBox => "view_week",
		SuiElementType.VerticalBox => "view_agenda",
		SuiElementType.Grid => "grid_on",
		SuiElementType.ScrollPanel => "swap_vert",
		SuiElementType.ProgressBar => "linear_scale",
		SuiElementType.InventoryGrid => "grid_view",
		SuiElementType.InventorySlot => "check_box_outline_blank",
		SuiElementType.ItemIcon => "category",
		SuiElementType.Tooltip => "info",
		SuiElementType.Hotbar => "view_carousel",
		_ => "extension",
	};
}

/// <summary>
/// Tree state + row builder. Not a Widget — it adds <see cref="SuiTreeRow"/>
/// instances directly into an external container (the ScrollArea's canvas).
/// This is the same shape Palette uses, which scrolls correctly because the
/// canvas's intrinsic size is derived from the child rows.
/// </summary>
public sealed class SuiTreeView
{
	private readonly Widget _container;
	private SuiDocument _document;
	private string _filter = "";
	// COLLAPSED ids — anything not in this set is expanded by default.
	private readonly HashSet<string> _collapsed = new();
	private HashSet<string> _selectedIds = new();
	private readonly List<SuiTreeRow> _rows = new();

	public Action<SuiElement> OnElementSelected;
	public Action<SuiElement> OnElementContextMenu;
	public Action<SuiElement> OnElementToggleVisibility;
	public Action<SuiElement> OnElementToggleLock;
	public Action<SuiElement, SuiElement, int> OnElementReparent;

	public SuiTreeView( Widget container )
	{
		_container = container;
	}

	public void SetDocument( SuiDocument doc )
	{
		_document = doc;
		_collapsed.Clear();
		Rebuild();
	}

	public void SetFilter( string f )
	{
		_filter = f ?? "";
		Rebuild();
	}

	public void SetSelectedSet( HashSet<string> ids )
	{
		_selectedIds = ids ?? new();
		foreach ( var row in _rows )
		{
			row.IsSelected = _selectedIds.Contains( row.Element.Id );
			row.Update();
		}
	}

	public bool IsExpanded( string id ) => !_collapsed.Contains( id );

	public void ToggleExpanded( string id )
	{
		if ( _collapsed.Contains( id ) ) _collapsed.Remove( id );
		else _collapsed.Add( id );
		Rebuild();
	}

	public void RebuildIfNeeded() => Rebuild();

	private void Rebuild()
	{
		if ( _container == null || !_container.IsValid() ) return;
		_container.Layout.Clear( true );
		_rows.Clear();

		if ( _document == null ) return;
		var root = _document.GetRoot();
		if ( root == null ) return;

		var byId = new Dictionary<string, SuiElement>();
		foreach ( var e in _document.Elements )
		{
			if ( !string.IsNullOrEmpty( e.Id ) ) byId[e.Id] = e;
		}

		// Search filter: show matches + their ancestors.
		HashSet<string> visibleIds = null;
		if ( !string.IsNullOrEmpty( _filter ) )
		{
			visibleIds = new HashSet<string>();
			foreach ( var el in _document.Elements )
			{
				if ( el?.Name == null ) continue;
				if ( !el.Name.ToLowerInvariant().Contains( _filter ) ) continue;
				var current = el;
				while ( current != null )
				{
					visibleIds.Add( current.Id );
					if ( string.IsNullOrEmpty( current.ParentId ) ) break;
					byId.TryGetValue( current.ParentId, out current );
				}
			}
		}

		Walk( root, byId, depth: 0, ancestorIsLast: Array.Empty<bool>(), visibleIds );

		// Stretch cell at the bottom keeps the rows packed at the top when
		// the canvas is taller than the row stack (e.g. after collapsing a
		// branch). Without it Qt distributes the slack across rows.
		_container.Layout.AddStretchCell();

		_container.Update();
	}

	private void Walk( SuiElement el, Dictionary<string, SuiElement> byId, int depth, bool[] ancestorIsLast, HashSet<string> visibleIds )
	{
		if ( el == null ) return;
		if ( visibleIds != null && !visibleIds.Contains( el.Id ) ) return;

		bool hasChildren = el.Children != null && el.Children.Count > 0;
		bool expanded = !_collapsed.Contains( el.Id );

		var row = new SuiTreeRow( this, el, depth, ancestorIsLast, hasChildren, expanded );
		row.Parent = _container;
		row.IsSelected = _selectedIds.Contains( el.Id );
		_rows.Add( row );
		_container.Layout.Add( row );

		if ( hasChildren && expanded )
		{
			for ( int i = 0; i < el.Children.Count; i++ )
			{
				var childId = el.Children[i];
				if ( !byId.TryGetValue( childId, out var child ) ) continue;

				bool isLastChild = (i == el.Children.Count - 1);
				var childAncestors = new bool[ancestorIsLast.Length + 1];
				Array.Copy( ancestorIsLast, childAncestors, ancestorIsLast.Length );
				childAncestors[ancestorIsLast.Length] = isLastChild;

				Walk( child, byId, depth + 1, childAncestors, visibleIds );
			}
		}
	}

	internal void OnRowClicked( SuiTreeRow row )
	{
		OnElementSelected?.Invoke( row.Element );
	}

	internal void OnRowContextMenu( SuiTreeRow row )
	{
		OnElementContextMenu?.Invoke( row.Element );
	}

	internal void OnRowToggleVisibility( SuiTreeRow row )
	{
		OnElementToggleVisibility?.Invoke( row.Element );
	}

	internal void OnRowToggleLock( SuiTreeRow row )
	{
		OnElementToggleLock?.Invoke( row.Element );
	}

	internal void OnRowToggleExpanded( SuiTreeRow row )
	{
		ToggleExpanded( row.Element.Id );
	}

	internal void OnRowDrop( SuiElement source, SuiElement target, float relativeY, float rowHeight )
	{
		if ( source == null || target == null || source.Id == target.Id ) return;
		if ( _document == null ) return;
		if ( string.IsNullOrEmpty( source.ParentId ) ) return;

		// Refuse cycle: target must not be a descendant of source.
		var safety = 1024;
		var cur = target;
		while ( cur != null && --safety > 0 )
		{
			if ( cur.Id == source.Id ) return;
			if ( string.IsNullOrEmpty( cur.ParentId ) ) break;
			cur = _document.GetElement( cur.ParentId );
		}

		// Drop edge — top 5px = sibling-above, bottom 5px = sibling-below,
		// middle = child-of-target (if container) or sibling-after-target.
		const float edgeThreshold = 5f;
		bool topEdge = relativeY < edgeThreshold;
		bool bottomEdge = relativeY > rowHeight - edgeThreshold;

		if ( topEdge || bottomEdge )
		{
			var parent = _document.GetElement( target.ParentId );
			if ( parent == null ) return;
			var idx = parent.Children.IndexOf( target.Id );
			if ( idx < 0 ) idx = parent.Children.Count;
			if ( bottomEdge ) idx += 1;
			OnElementReparent?.Invoke( source, parent, idx );
			return;
		}

		// Middle drop — child of target if container, else sibling-after.
		if ( SuiHierarchyWidget.IsContainerType( target.Type ) )
		{
			OnElementReparent?.Invoke( source, target, target.Children.Count );
			return;
		}

		var leafParent = _document.GetElement( target.ParentId );
		if ( leafParent == null ) return;
		var siblingIdx = leafParent.Children.IndexOf( target.Id );
		siblingIdx = siblingIdx < 0 ? leafParent.Children.Count : siblingIdx + 1;
		OnElementReparent?.Invoke( source, leafParent, siblingIdx );
	}
}

/// <summary>
/// Single row in the tree — paint-only widget rendering tree-lines + chevron
/// + type icon + element name + right-side eye / lock toggle icons.
/// </summary>
public sealed class SuiTreeRow : Widget
{
	private const int IndentPx = 18;
	private const int RowHeight = 24;

	public SuiElement Element { get; }
	public int Depth { get; }
	public bool[] AncestorIsLast { get; }
	public bool HasChildren { get; }
	public bool IsExpandedNow { get; set; }
	public bool IsSelected { get; set; }

	private readonly SuiTreeView _tree;
	private bool _hover;
	private Rect _chevronRect;
	private Rect _eyeRect;
	private Rect _lockRect;

	public SuiTreeRow( SuiTreeView tree, SuiElement el, int depth, bool[] ancestorIsLast, bool hasChildren, bool isExpanded )
		: base( null )
	{
		_tree = tree;
		Element = el;
		Depth = depth;
		AncestorIsLast = ancestorIsLast;
		HasChildren = hasChildren;
		IsExpandedNow = isExpanded;
		FixedHeight = RowHeight;
		Cursor = CursorShape.Finger;
		IsDraggable = !string.IsNullOrEmpty( el.ParentId );
		AcceptDrops = true;
		SetStyles( "background-color: transparent; border: none;" );
	}

	protected override void OnPaint()
	{
		var rect = LocalRect;

		// Selection / hover background.
		if ( IsSelected )
		{
			Paint.SetBrushAndPen( new Color( 15 / 255f, 63 / 255f, 121 / 255f, 0.55f ) );
			Paint.DrawRect( rect );
		}
		else if ( _hover )
		{
			Paint.SetBrushAndPen( Color.White.WithAlpha( 0.04f ) );
			Paint.DrawRect( rect );
		}

		Paint.SetDefaultFont( 11 );

		// Tree lines for ancestors that still have continuations below.
		var lineColor = Color.White.WithAlpha( 0.10f );
		Paint.SetPen( lineColor, 1f );
		for ( int d = 0; d < AncestorIsLast.Length - 1; d++ )
		{
			if ( !AncestorIsLast[d] )
			{
				float lx = (d + 1) * IndentPx - IndentPx / 2f + 2;
				Paint.DrawLine( new Vector2( lx, 0 ), new Vector2( lx, Height ) );
			}
		}

		// Own L-shape connector (only if not root depth).
		if ( Depth > 0 )
		{
			float xVert = Depth * IndentPx - IndentPx / 2f + 2;
			float yMid = Height / 2f;
			bool selfIsLast = AncestorIsLast.Length > 0 && AncestorIsLast[AncestorIsLast.Length - 1];
			// Vertical from top to mid (or full height if not last).
			Paint.DrawLine( new Vector2( xVert, 0 ), new Vector2( xVert, selfIsLast ? yMid : Height ) );
			// Horizontal from xVert to chevron/icon area.
			Paint.DrawLine( new Vector2( xVert, yMid ), new Vector2( xVert + IndentPx / 2f - 1, yMid ) );
		}

		float x = Depth * IndentPx + 2;

		// Chevron — only painted when has children.
		if ( HasChildren )
		{
			_chevronRect = new Rect( x, (Height - 14) / 2f, 14, 14 );
			var chevColor = new Color( 165 / 255f, 172 / 255f, 182 / 255f );
			Paint.SetPen( chevColor, 1.5f );
			float cx = x + 7;
			float cy = Height / 2f;
			if ( IsExpandedNow )
			{
				// ▾ down
				Paint.DrawLine( new Vector2( cx - 3, cy - 1 ), new Vector2( cx, cy + 2 ) );
				Paint.DrawLine( new Vector2( cx, cy + 2 ), new Vector2( cx + 3, cy - 1 ) );
			}
			else
			{
				// ▸ right
				Paint.DrawLine( new Vector2( cx - 1, cy - 3 ), new Vector2( cx + 2, cy ) );
				Paint.DrawLine( new Vector2( cx + 2, cy ), new Vector2( cx - 1, cy + 3 ) );
			}
		}
		else
		{
			_chevronRect = new Rect( 0, 0, 0, 0 );
		}
		x += 16;

		// Type icon.
		var textColor = IsSelected ? Color.White : new Color( 220 / 255f, 224 / 255f, 230 / 255f );
		Paint.SetPen( textColor );
		Paint.DrawIcon( new Rect( x, (Height - 14) / 2f, 14, 14 ), SuiHierarchyWidget.IconForType( Element.Type ), 14 );
		x += 20;

		// Eye + Lock — right-aligned with 12px padding from the right edge so
		// they don't collide with the panel border / scrollbar.
		var hidden = Element.Flags?.HiddenInDesigner == true;
		var locked = Element.Flags?.Locked == true;

		const int rightPad = 12;
		_lockRect = new Rect( Width - rightPad - 14, (Height - 14) / 2f, 14, 14 );
		_eyeRect = new Rect( _lockRect.Left - 6 - 14, (Height - 14) / 2f, 14, 14 );

		// Eye: bright white when visible, dim grey when hidden + thin diagonal line.
		var eyeColor = hidden
			? new Color( 130 / 255f, 134 / 255f, 140 / 255f )
			: new Color( 235 / 255f, 238 / 255f, 242 / 255f );
		Paint.SetPen( eyeColor );
		Paint.DrawIcon( _eyeRect, "visibility", 14 );
		if ( hidden )
		{
			// Soft diagonal slash across the eye to indicate "off".
			Paint.SetPen( eyeColor, 1.2f );
			Paint.DrawLine(
				new Vector2( _eyeRect.Left + 2, _eyeRect.Bottom - 3 ),
				new Vector2( _eyeRect.Right - 2, _eyeRect.Top + 3 ) );
		}

		// Lock: amber when locked, dim grey otherwise.
		var lockColor = locked
			? new Color( 235 / 255f, 178 / 255f, 96 / 255f )
			: new Color( 130 / 255f, 134 / 255f, 140 / 255f );
		Paint.SetPen( lockColor );
		Paint.DrawIcon( _lockRect, locked ? "lock" : "lock_open", 14 );

		// Name label — leaves room for the right icons.
		Paint.SetPen( textColor );
		var labelRect = new Rect( x, 0, _eyeRect.Left - x - 4, Height );
		Paint.DrawText( labelRect, Element.Name ?? "(unnamed)", TextFlag.LeftCenter );
	}

	protected override void OnMouseEnter() { _hover = true; Update(); }
	protected override void OnMouseLeave() { _hover = false; Update(); }

	protected override void OnMousePress( MouseEvent e )
	{
		// Each callback may rebuild the tree (Refresh) which destroys this
		// row mid-handler. We early-return immediately after dispatch so no
		// further code touches the now-zombie `this`. Qt's mouse pipeline
		// tolerates that, but anything we'd call after (Update, field access)
		// would crash with "QWidget was null".
		if ( e.LeftMouseButton )
		{
			if ( HasChildren && _chevronRect.IsInside( e.LocalPosition ) )
			{
				_tree.OnRowToggleExpanded( this );
				return;
			}
			if ( _eyeRect.IsInside( e.LocalPosition ) )
			{
				_tree.OnRowToggleVisibility( this );
				return;
			}
			if ( _lockRect.IsInside( e.LocalPosition ) )
			{
				_tree.OnRowToggleLock( this );
				return;
			}
			_tree.OnRowClicked( this );
			return;
		}
		if ( e.RightMouseButton )
		{
			_tree.OnRowContextMenu( this );
			return;
		}
	}

	protected override void OnDragStart()
	{
		if ( !IsDraggable ) return;
		var drag = new Drag( this );
		drag.Data.Object = Element;
		drag.Execute();
	}

	public override void OnDragHover( DragEvent ev )
	{
		ev.Action = DropAction.Copy;
	}

	public override void OnDragDrop( DragEvent ev )
	{
		if ( ev.Data.Object is SuiElement source )
		{
			_tree.OnRowDrop( source, Element, ev.LocalPosition.y, Height );
		}
	}
}
