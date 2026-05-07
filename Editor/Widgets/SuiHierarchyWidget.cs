using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Hierarchy dock — left side, below palette. Renders the document tree using
/// the editor's native <see cref="TreeView"/>. Tree nodes paint themselves via
/// <see cref="Paint"/> (Qt-style immediate drawing) so the icon, name, and
/// selection highlight render correctly inside the engine's editor renderer
/// (CSS-style SetStyles does not reach this surface).
///
/// Pattern reference: Facepunch TreeView.Example (FilesystemTreeNode) in
/// sbox-public/game/addons/tools/Code/Widgets/TreeView/TreeView.Example.cs.
/// Context-menu pattern: apetavern/what-lurks-below SceneNode.OnContextMenu.
/// </summary>
public class SuiHierarchyWidget : Widget
{
	private SuiDocument _document;
	private SuiElement _selected;

	private TreeView _tree;

	/// <summary>Raised when the user selects an element in the tree.</summary>
	public event Action<SuiElement> ElementSelected;

	/// <summary>Raised when the user picks "Add Child → Type" in the context menu.
	/// Args: (target parent element, requested child type).</summary>
	public event Action<SuiElement, SuiElementType> AddChildRequested;

	/// <summary>Raised when the user picks "Rename" in the context menu (or hits F2).</summary>
	public event Action<SuiElement, string> RenameRequested;

	/// <summary>Raised when the user picks "Delete".</summary>
	public event Action<SuiElement> DeleteRequested;

	/// <summary>Raised when the user picks "Duplicate".</summary>
	public event Action<SuiElement> DuplicateRequested;

	/// <summary>Raised when the user picks "Move Up" / "Move Down".</summary>
	public event Action<SuiElement> MoveUpRequested;
	public event Action<SuiElement> MoveDownRequested;

	/// <summary>Raised when a drag operation reorders / reparents the element.
	/// Args: (moved element, new parent, insert index in new parent).</summary>
	public event Action<SuiElement, SuiElement, int> ReparentRequested;

	public SuiHierarchyWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Hierarchy";
		Name = "SuiHierarchy";
		MinimumSize = new Vector2( 200, 200 );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		var header = new Label( "Hierarchy", this );
		header.SetStyles( "padding: 6px; font-weight: bold; color: #e5e7eb;" );
		Layout.Add( header );

		_tree = new TreeView( this );
		_tree.IndentWidth = 16;
		_tree.ItemSpacing = 2;
		_tree.ExpandForSelection = true;
		_tree.AcceptDrops = true; // required so OnDragStart on tree nodes is reached
		_tree.ItemSelected = OnTreeItemSelected;
		Layout.Add( _tree, 1 );

		Refresh();
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		_selected = null;
		Refresh();
	}

	public void SetSelected( SuiElement element )
	{
		_selected = element;
		// TreeView keeps its own selection state via IsSelected(object) which
		// looks up by the node's Value. Our nodes set Value = element, so the
		// view picks up the new selection automatically on the next paint.
	}

	public void Refresh()
	{
		if ( _tree == null ) return;

		if ( _document == null )
		{
			_tree.SetItems( Array.Empty<object>() );
			return;
		}

		var byId = BuildIdMap( _document );
		var root = _document.GetRoot();
		if ( root == null )
		{
			_tree.SetItems( Array.Empty<object>() );
			return;
		}

		var rootNode = new SuiElementTreeNode( root, byId, this, IsSelectedFor );
		_tree.SetItems( new[] { rootNode } );
		ExpandRecursive( rootNode );
	}

	/// <summary>
	/// Programmatically begin renaming the selected element via the TreeView's
	/// inline rename UI. Called by the F2 shortcut on SuiDesignerWindow.
	/// </summary>
	public void BeginRenameSelected()
	{
		if ( _selected == null || _tree == null ) return;
		_tree.BeginRename();
	}

	private bool IsSelectedFor( SuiElement element )
	{
		return _selected != null && element != null && _selected.Id == element.Id;
	}

	private void ExpandRecursive( SuiElementTreeNode node )
	{
		_tree.Open( node );
		foreach ( var child in node.Children )
		{
			if ( child is SuiElementTreeNode sn )
				ExpandRecursive( sn );
		}
	}

	private void OnTreeItemSelected( object obj )
	{
		// BaseItemWidget.ItemSelected passes the TreeNode's Value, not the node
		// itself — and our nodes set Value = element. So obj is the SuiElement.
		// (Earlier the type-test against SuiElementTreeNode silently swallowed
		// every click because the actual payload was a SuiElement.)
		if ( obj is SuiElement element )
		{
			_selected = element;
			ElementSelected?.Invoke( element );
		}
	}

	private static Dictionary<string, SuiElement> BuildIdMap( SuiDocument doc )
	{
		var map = new Dictionary<string, SuiElement>();
		foreach ( var el in doc.Elements )
		{
			if ( !string.IsNullOrEmpty( el.Id ) ) map[el.Id] = el;
		}
		return map;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Context menu invoked by SuiElementTreeNode.OnContextMenu.
	//  Centralised here so the menu wiring lives in one place even though
	//  individual nodes are rebuilt on every Refresh().
	// ─────────────────────────────────────────────────────────────────────

	internal bool ShowContextMenu( SuiElementTreeNode node )
	{
		if ( node?.Element == null ) return false;

		var element = node.Element;
		var isContainer = IsContainerType( element.Type );
		var canDelete = !string.IsNullOrEmpty( element.ParentId );

		// Selecting the right-clicked element first means commands like
		// "Duplicate" / "Move Up" target what the user just clicked, even
		// if it wasn't already the selection.
		_selected = element;
		ElementSelected?.Invoke( element );

		// Sibling-index aware: enable Move Up / Down only when there is room.
		var siblingIdx = -1;
		var siblingCount = 0;
		if ( !string.IsNullOrEmpty( element.ParentId ) && _document != null )
		{
			var parent = _document.GetElement( element.ParentId );
			if ( parent != null )
			{
				siblingIdx = parent.Children.IndexOf( element.Id );
				siblingCount = parent.Children.Count;
			}
		}

		var m = new Menu( _tree );

		// Add Child / Add Sibling submenu
		var addLabel = isContainer ? "Add Child" : "Add Sibling";
		var addTarget = isContainer ? element : (_document?.GetElement( element.ParentId ) ?? element);
		var addMenu = m.AddMenu( addLabel, "add" );
		AddTypeOptions( addMenu, new[]
		{
			SuiElementType.Panel, SuiElementType.Text, SuiElementType.Image, SuiElementType.Button,
		}, addTarget );
		addMenu.AddSeparator();
		AddTypeOptions( addMenu, new[]
		{
			SuiElementType.HorizontalBox, SuiElementType.VerticalBox, SuiElementType.Grid, SuiElementType.Overlay,
		}, addTarget );
		addMenu.AddSeparator();
		AddTypeOptions( addMenu, new[]
		{
			SuiElementType.ProgressBar, SuiElementType.ScrollPanel, SuiElementType.InventoryGrid,
			SuiElementType.InventorySlot, SuiElementType.ItemIcon, SuiElementType.Tooltip, SuiElementType.Hotbar,
		}, addTarget );

		m.AddSeparator();

		var rename = m.AddOption( "Rename", "edit", () => RenameRequested?.Invoke( element, null ) );
		rename.StatusTip = "F2";
		rename.Enabled = canDelete; // root cannot rename either

		m.AddOption( "Duplicate", "content_copy", () => DuplicateRequested?.Invoke( element ) );

		var moveUp = m.AddOption( "Move Up", "arrow_upward", () => MoveUpRequested?.Invoke( element ) );
		moveUp.Enabled = siblingIdx > 0;

		var moveDown = m.AddOption( "Move Down", "arrow_downward", () => MoveDownRequested?.Invoke( element ) );
		moveDown.Enabled = siblingIdx >= 0 && siblingIdx < siblingCount - 1;

		var del = m.AddOption( "Delete", "delete", () => DeleteRequested?.Invoke( element ) );
		del.Enabled = canDelete;
		del.StatusTip = canDelete ? "Del" : "Cannot delete root";

		m.OpenAtCursor( true );
		return true;
	}

	private void AddTypeOptions( Menu menu, IEnumerable<SuiElementType> types, SuiElement target )
	{
		// Plain options grouped via AddSeparator from the caller — Menu doesn't
		// have a "section heading" widget on its own.
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

	internal void OnRenameCommitted( SuiElement element, string newName )
	{
		if ( element == null || string.IsNullOrEmpty( newName ) ) return;
		RenameRequested?.Invoke( element, newName );
	}

	internal void OnReparentCommitted( SuiElement child, SuiElement newParent, int insertIndex )
	{
		if ( child == null || newParent == null ) return;
		ReparentRequested?.Invoke( child, newParent, insertIndex );
	}

	/// <summary>
	/// UE-UMG-style drop resolution based on which edge of the target row the
	/// pointer is over:
	///
	///   ┌─────────────────────────┐
	///   │  Top    -> insert ABOVE │  (sibling of target, same parent)
	///   ├─────────────────────────┤
	///   │  Middle -> become CHILD │  (only if target is a container; else
	///   │           of target     │   falls back to sibling-after)
	///   ├─────────────────────────┤
	///   │ Bottom  -> insert BELOW │  (sibling of target, same parent)
	///   └─────────────────────────┘
	///
	/// The 5-pixel edge threshold is set by BaseItemWidget when it builds the
	/// ItemDragEvent (LocalPosition.y &lt; 5 -> Top; &gt; Height-5 -> Bottom).
	/// </summary>
	internal void HandleDrop( SuiElement source, SuiElement target, BaseItemWidget.ItemEdge dropEdge )
	{
		if ( source == null || target == null || source.Id == target.Id ) return;
		if ( _document == null ) return;
		if ( string.IsNullOrEmpty( source.ParentId ) ) return; // refuse moving root

		// Refuse cycle: target cannot be a descendant of source.
		if ( IsDescendantInDoc( target.Id, source.Id ) ) return;

		var dropOnEdge = (dropEdge & BaseItemWidget.ItemEdge.Top) != 0
			|| (dropEdge & BaseItemWidget.ItemEdge.Bottom) != 0;

		// Edge drop — sibling insertion at target's position.
		if ( dropOnEdge )
		{
			var parent = _document.GetElement( target.ParentId );
			if ( parent == null ) return; // target was root; fall back to nothing
			var idx = parent.Children.IndexOf( target.Id );
			if ( idx < 0 ) idx = parent.Children.Count;
			if ( (dropEdge & BaseItemWidget.ItemEdge.Bottom) != 0 ) idx += 1;

			// If we're moving inside the same parent and removing source first
			// would shift indices, the controller's command handles index
			// stability — we just pass the visual index here.
			ReparentRequested?.Invoke( source, parent, idx );
			return;
		}

		// Middle drop — make source a child of target (UE-UMG style).
		if ( IsContainerType( target.Type ) )
		{
			ReparentRequested?.Invoke( source, target, target.Children.Count );
			return;
		}

		// Middle drop on a leaf — fall back to sibling-after for the same
		// parent so the gesture isn't lost.
		var leafParent = _document.GetElement( target.ParentId );
		if ( leafParent == null ) return;
		var siblingIdx = leafParent.Children.IndexOf( target.Id );
		if ( siblingIdx < 0 ) siblingIdx = leafParent.Children.Count;
		else siblingIdx += 1;
		ReparentRequested?.Invoke( source, leafParent, siblingIdx );
	}

	private bool IsDescendantInDoc( string candidateId, string ancestorId )
	{
		if ( _document == null ) return false;
		var safety = 1024;
		var currentId = candidateId;
		while ( !string.IsNullOrEmpty( currentId ) && --safety > 0 )
		{
			if ( currentId == ancestorId ) return true;
			var current = _document.GetElement( currentId );
			if ( current == null ) return false;
			currentId = current.ParentId;
		}
		return false;
	}
}


/// <summary>
/// TreeView node that draws a single SuiElement row: icon (per type) + name +
/// type label, with the standard editor selection highlight.
///
/// Hosts the OnContextMenu / OnRename / OnItemDrag overrides that delegate
/// back to <see cref="SuiHierarchyWidget"/> — the widget centralises the
/// actual menu wiring so individual node instances (which get rebuilt on
/// every Refresh) stay light.
/// </summary>
internal sealed class SuiElementTreeNode : TreeNode
{
	public SuiElement Element { get; }
	public Dictionary<string, SuiElement> ByIdMap { get; }

	private readonly SuiHierarchyWidget _owner;
	private readonly Func<SuiElement, bool> _isSelectedFn;

	public SuiElementTreeNode(
		SuiElement element,
		Dictionary<string, SuiElement> byIdMap,
		SuiHierarchyWidget owner,
		Func<SuiElement, bool> isSelectedFn )
	{
		Element = element;
		ByIdMap = byIdMap;
		_owner = owner;
		_isSelectedFn = isSelectedFn;
		Value = element;
	}

	public override bool HasChildren => Element != null && Element.Children != null && Element.Children.Count > 0;

	public override bool CanEdit => Element != null && !string.IsNullOrEmpty( Element.ParentId );

	public override string Name
	{
		get => Element?.Name ?? "(null)";
		set { /* see OnRename — TreeView writes here during inline rename */ }
	}

	public override void OnPaint( VirtualWidget item )
	{
		PaintSelection( item );

		var iconRect = item.Rect;
		Paint.SetPen( Color.White );
		Paint.DrawIcon( iconRect, SuiHierarchyWidget.IconForType( Element.Type ), 16, TextFlag.LeftCenter );

		Paint.SetPen( Theme.Text );
		Paint.DrawText(
			item.Rect.Shrink( 24, 0, 0, 0 ),
			$"{Element.Name}  ·  {Element.Type}",
			TextFlag.LeftCenter );
	}

	public override bool OnContextMenu()
	{
		return _owner != null && _owner.ShowContextMenu( this );
	}

	public override void OnRename( VirtualWidget item, string text, List<TreeNode> selection = null )
	{
		base.OnRename( item, text, selection );
		_owner?.OnRenameCommitted( Element, text );
	}

	public override bool OnDragStart()
	{
		// TreeView default: TreeView.OnDragItem(item) -> node.OnDragStart().
		// We must set up Drag.Data.Object and Execute() here for the drag to
		// actually begin; returning true tells the TreeView the drag is live.
		if ( Element == null ) return false;
		if ( string.IsNullOrEmpty( Element.ParentId ) ) return false; // refuse root

		var drag = new Drag( TreeView );
		drag.Data.Object = this; // source TreeNode -- read on drop side via e.Data.Object
		drag.Execute();
		return true;
	}

	public override DropAction OnDragDrop( BaseItemWidget.ItemDragEvent e )
	{
		// ItemDragEvent / ItemEdge are nested inside Editor.BaseItemWidget —
		// must be qualified from outside the Editor namespace. The base
		// TreeNode declaration uses the bare name because it lives in
		// 'namespace Editor'.
		if ( _owner == null || Element == null ) return DropAction.Ignore;

		// `e.Data.Object` is whatever OnDragStart put in there — for SUI nodes
		// that's the source SuiElementTreeNode. `this` is the target node.
		if ( e.Data.Object is not SuiElementTreeNode sourceNode || sourceNode.Element == null )
			return DropAction.Ignore;

		if ( !e.IsDrop )
		{
			// Hover phase — accept so the cursor shows a "move" icon. The
			// actual reparent only happens when IsDrop becomes true.
			return DropAction.Move;
		}

		_owner.HandleDrop( sourceNode.Element, Element, e.DropEdge );
		return DropAction.Move;
	}

	protected override void BuildChildren()
	{
		Clear();
		if ( Element == null || ByIdMap == null ) return;
		foreach ( var childId in Element.Children )
		{
			if ( ByIdMap.TryGetValue( childId, out var child ) )
				AddItem( new SuiElementTreeNode( child, ByIdMap, _owner, _isSelectedFn ) );
		}
	}
}
