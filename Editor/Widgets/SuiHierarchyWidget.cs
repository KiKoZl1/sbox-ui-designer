using System;
using System.Collections.Generic;
using Editor;
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
/// </summary>
public class SuiHierarchyWidget : Widget
{
	private SuiDocument _document;
	private SuiElement _selected;

	private TreeView _tree;

	/// <summary>Raised when the user selects an element in the tree.</summary>
	public event Action<SuiElement> ElementSelected;

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

		var rootNode = new SuiElementTreeNode( root, byId, IsSelectedFor );
		_tree.SetItems( new[] { rootNode } );
		ExpandRecursive( rootNode );
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
		if ( obj is SuiElementTreeNode node && node.Element != null )
		{
			_selected = node.Element;
			ElementSelected?.Invoke( node.Element );
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
}

/// <summary>
/// TreeView node that draws a single SuiElement row: icon (per type) + name +
/// type label, with the standard editor selection highlight.
/// </summary>
internal sealed class SuiElementTreeNode : TreeNode
{
	public SuiElement Element { get; }
	public Dictionary<string, SuiElement> ByIdMap { get; }

	private readonly Func<SuiElement, bool> _isSelectedFn;

	public SuiElementTreeNode( SuiElement element, Dictionary<string, SuiElement> byIdMap, Func<SuiElement, bool> isSelectedFn )
	{
		Element = element;
		ByIdMap = byIdMap;
		_isSelectedFn = isSelectedFn;
		Value = element;
	}

	public override bool HasChildren => Element != null && Element.Children != null && Element.Children.Count > 0;

	public override string Name
	{
		get => Element?.Name ?? "(null)";
		set { /* rename handled by controller, not by TreeView's inline rename */ }
	}

	public override void OnPaint( VirtualWidget item )
	{
		PaintSelection( item );

		var iconRect = item.Rect;
		Paint.SetPen( Color.White );
		Paint.DrawIcon( iconRect, IconForType( Element.Type ), 16, TextFlag.LeftCenter );

		Paint.SetPen( Theme.Text );
		Paint.DrawText(
			item.Rect.Shrink( 24, 0, 0, 0 ),
			$"{Element.Name}  ·  {Element.Type}",
			TextFlag.LeftCenter );
	}

	protected override void BuildChildren()
	{
		Clear();
		if ( Element == null || ByIdMap == null ) return;
		foreach ( var childId in Element.Children )
		{
			if ( ByIdMap.TryGetValue( childId, out var child ) )
				AddItem( new SuiElementTreeNode( child, ByIdMap, _isSelectedFn ) );
		}
	}

	private static string IconForType( SuiElementType type ) => type switch
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
