using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Palette panel — list of element types the user can add to the document
/// (click to add, drag to drop on a container in the canvas).
///
/// 100% custom paint except for the search <see cref="LineEdit"/>, which is
/// the native Editor input (text editing / cursor / IME would be a huge
/// rewrite for marginal visual gain — only its background is restyled).
///
/// Layout:
///   Search input (#141413 fill)
///   Category COMMON      — collapsible header + flat items
///   ─── separator ───
///   Category LAYOUT
///   ─── separator ───
///   Category GAME UI (V1)
///
/// Items are flat: icon + label, no background, hover tint only.
/// </summary>
public class SuiPaletteWidget : Widget
{
	private LineEdit _search;
	private string _filter = "";
	private readonly List<SuiPaletteCategory> _categories = new();

	/// <summary>Raised when the user clicks a palette item.</summary>
	public event Action<SuiElementType> ElementRequested;

	public SuiPaletteWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Palette";
		Name = "SuiPalette";
		MinimumSize = new Vector2( 200, 200 );
		SetStyles( "background-color: transparent; border: none;" );

		Layout = Layout.Column();
		Layout.Margin = new Sandbox.UI.Margin( 8, 8, 8, 8 );
		Layout.Spacing = 0;

		// Search input — native LineEdit with custom styling.
		_search = new LineEdit( this );
		_search.PlaceholderText = "Search Palette";
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
			ApplyFilter();
		};
		Layout.Add( _search );

		// Scrollable content area below the (fixed) search input.
		var scroll = new ScrollArea( this );
		SuiScrollStyle.ApplyTo( scroll );
		_scrollContent = new Widget( scroll );
		_scrollContent.SetStyles( "background-color: transparent; border: none;" );
		_scrollContent.Layout = Layout.Column();
		_scrollContent.Layout.Margin = 0;
		_scrollContent.Layout.Spacing = 0;
		scroll.Canvas = _scrollContent;
		Layout.Add( scroll, 1 );

		// 8px gap between search and first category header.
		AddSpacer( 8 );

		AddCategory( "COMMON", new[]
		{
			SuiElementType.Panel,
			SuiElementType.Text,
			SuiElementType.Image,
			SuiElementType.Button,
		}, separatorBefore: false );

		AddCategory( "LAYOUT", new[]
		{
			SuiElementType.HorizontalBox,
			SuiElementType.VerticalBox,
			SuiElementType.Grid,
			SuiElementType.Overlay,
		}, separatorBefore: true );

		AddCategory( "GAME UI (V1)", new[]
		{
			SuiElementType.ProgressBar,
			SuiElementType.ScrollPanel,
			SuiElementType.InventoryGrid,
			SuiElementType.InventorySlot,
			SuiElementType.ItemIcon,
			SuiElementType.Tooltip,
			SuiElementType.Hotbar,
		}, separatorBefore: true );

		_scrollContent.Layout.AddStretchCell();
	}

	private Widget _scrollContent;

	private void AddCategory( string title, SuiElementType[] types, bool separatorBefore )
	{
		if ( separatorBefore )
		{
			var sep = new SuiPaletteCategorySeparator( _scrollContent );
			_scrollContent.Layout.Add( sep );
		}

		var cat = new SuiPaletteCategory( title, types, _scrollContent );
		cat.ElementClicked += t => ElementRequested?.Invoke( t );
		_categories.Add( cat );
		_scrollContent.Layout.Add( cat );
	}

	private void AddSpacer( int px )
	{
		var spc = new Widget( _scrollContent );
		spc.FixedHeight = px;
		spc.SetStyles( "background-color: transparent; border: none;" );
		_scrollContent.Layout.Add( spc );
	}

	private void ApplyFilter()
	{
		foreach ( var cat in _categories )
			cat.ApplyFilter( _filter );
	}

	internal static string GetIconFor( SuiElementType type ) => IconFor( type );

	internal static string IconFor( SuiElementType type ) => type switch
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

	/// <summary>
	/// Per-type tooltip describing what the element does and when to use it.
	/// Shown on hover over a palette item.
	/// </summary>
	internal static string TooltipFor( SuiElementType type ) => type switch
	{
		SuiElementType.Panel =>
			"Panel — generic container with background + padding.\nUse to group children, add a frame, or apply a single background.",

		SuiElementType.Text =>
			"Text — static label.\nUse for titles, descriptions, status readouts (HP value, score).",

		SuiElementType.Image =>
			"Image — static picture from your project assets.\nUse for icons, backgrounds, decorative art. PNG/JPG/SVG.",

		SuiElementType.Button =>
			"Button — clickable element with hover/press states.\nUse for actions (Save, Cancel, Submit, Open Menu).",

		SuiElementType.HorizontalBox =>
			"HorizontalBox — arranges children left-to-right in a row.\nUse for toolbars, button rows, status lines.",

		SuiElementType.VerticalBox =>
			"VerticalBox — arranges children top-to-bottom in a column.\nUse for menus, lists, sidebar sections.",

		SuiElementType.Grid =>
			"Grid — arranges children in rows × columns.\nUse for inventory grids, calendar layouts, image galleries.",

		SuiElementType.Overlay =>
			"Overlay — stacks children on top of each other (z-ordered).\nUse for modal dialogs, tooltips, badges over icons.",

		SuiElementType.ProgressBar =>
			"ProgressBar — filled bar showing a 0..1 value.\nUse for Health, Mana, XP, cooldowns, loading indicators. Bind Value to a property.",

		SuiElementType.ScrollPanel =>
			"ScrollPanel — container with scroll when content overflows.\nUse for long lists, chat windows, oversized text.",

		SuiElementType.InventoryGrid =>
			"InventoryGrid — grid of inventory slots.\nUse for backpacks, loot windows, equipment screens. Wire Items to a player inventory.",

		SuiElementType.InventorySlot =>
			"InventorySlot — single slot accepting an item + count.\nUsually a child of InventoryGrid; can also stand alone (e.g. ammo slot).",

		SuiElementType.ItemIcon =>
			"ItemIcon — visual representation of an item.\nUsually inside an InventorySlot. Bind to an item resource for sprite + tint.",

		SuiElementType.Tooltip =>
			"Tooltip — floating description shown on hover.\nAnchor to another element. Use for stat explanations, ability details.",

		SuiElementType.Hotbar =>
			"Hotbar — horizontal row of slots bound to number keys (1-9, 0).\nUse for action bars: weapons, abilities, consumables.",

		_ => $"{type} — drag onto the canvas to drop on a container, or click to add at root.",
	};
}

/// <summary>
/// Collapsible category — header with chevron + container with flat items.
/// Click on header toggles expanded state.
/// </summary>
internal sealed class SuiPaletteCategory : Widget
{
	public string Title;
	private bool _expanded = true;
	private SuiPaletteCategoryHeader _header;
	private Widget _itemsContainer;
	private readonly List<SuiPaletteItem> _items = new();

	public event Action<SuiElementType> ElementClicked;

	public SuiPaletteCategory( string title, SuiElementType[] types, Widget parent ) : base( parent )
	{
		Title = title;
		SetStyles( "background-color: transparent; border: none;" );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		_header = new SuiPaletteCategoryHeader( title, this );
		_header.ToggledExpanded += () =>
		{
			_expanded = !_expanded;
			UpdateExpanded();
		};
		Layout.Add( _header );

		_itemsContainer = new Widget( this );
		_itemsContainer.SetStyles( "background-color: transparent; border: none;" );
		_itemsContainer.Layout = Layout.Column();
		_itemsContainer.Layout.Margin = 0;
		_itemsContainer.Layout.Spacing = 0;

		foreach ( var type in types )
		{
			var item = new SuiPaletteItem( type, _itemsContainer );
			item.Clicked += t => ElementClicked?.Invoke( t );
			_items.Add( item );
			_itemsContainer.Layout.Add( item );
		}

		Layout.Add( _itemsContainer );

		UpdateExpanded();
	}

	private void UpdateExpanded()
	{
		if ( _itemsContainer.IsValid() ) _itemsContainer.Visible = _expanded;
		_header.IsExpanded = _expanded;
		_header.Update();
	}

	public void ApplyFilter( string filter )
	{
		bool anyVisible = false;
		foreach ( var item in _items )
		{
			var match = string.IsNullOrEmpty( filter ) || item.SearchKey.Contains( filter );
			item.Visible = match;
			if ( match ) anyVisible = true;
		}
		Visible = anyVisible;
	}
}

/// <summary>
/// Category header — small uppercase label + chevron, no background, hover tint.
/// Click anywhere on it toggles the expanded state of the parent category.
/// </summary>
internal sealed class SuiPaletteCategoryHeader : Widget
{
	public string Title;
	public bool IsExpanded = true;
	public event Action ToggledExpanded;

	public SuiPaletteCategoryHeader( string title, Widget parent ) : base( parent )
	{
		Title = title ?? "";
		FixedHeight = 26;
		Cursor = CursorShape.Finger;
		SetStyles( "background-color: transparent; border: none;" );
	}

	protected override void OnPaint()
	{
		// Header is fully flat — no hover background, no border.
		// Chevron — ▾ when expanded, ▸ when collapsed.
		var chevColor = new Color( 150 / 255f, 156 / 255f, 165 / 255f );
		Paint.SetPen( chevColor, 1.5f );
		var cy = Height / 2f;
		if ( IsExpanded )
		{
			// ▾ down chevron at x=6
			Paint.DrawLine( new Vector2( 6, cy - 2 ), new Vector2( 10, cy + 2 ) );
			Paint.DrawLine( new Vector2( 10, cy + 2 ), new Vector2( 14, cy - 2 ) );
		}
		else
		{
			// ▸ right chevron
			Paint.DrawLine( new Vector2( 7, cy - 4 ), new Vector2( 11, cy ) );
			Paint.DrawLine( new Vector2( 11, cy ), new Vector2( 7, cy + 4 ) );
		}

		// Title — small caps, muted color.
		Paint.SetPen( new Color( 150 / 255f, 156 / 255f, 165 / 255f ) );
		Paint.SetDefaultFont( 10 );
		var titleRect = new Rect( 22, 0, Width - 24, Height );
		Paint.DrawText( titleRect, Title, TextFlag.LeftCenter );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton ) ToggledExpanded?.Invoke();
	}
}

/// <summary>
/// Flat palette item — icon + label, no background. Hover/press tints.
/// Click adds the element at root via the parent's <c>ElementRequested</c>.
/// Drag carries the element type for drop-on-container in the canvas.
/// </summary>
internal sealed class SuiPaletteItem : Widget
{
	public SuiElementType ElementType { get; }
	public string Label { get; }
	public string Icon { get; }
	public string SearchKey { get; }

	public event Action<SuiElementType> Clicked;

	private bool _hover;
	private bool _pressed;

	public SuiPaletteItem( SuiElementType type, Widget parent ) : base( parent )
	{
		ElementType = type;
		Label = type.ToString();
		Icon = SuiPaletteWidget.IconFor( type );
		SearchKey = Label.ToLowerInvariant();
		FixedHeight = 24;
		Cursor = CursorShape.Finger;
		IsDraggable = true;
		SetStyles( "background-color: transparent; border: none;" );
		ToolTip = SuiPaletteWidget.TooltipFor( type );
	}

	protected override void OnPaint()
	{
		// Icon + label — left-aligned, indented past the chevron column.
		// NO background fill, NO border. Hover state is a thin blue underline only.
		Paint.SetPen( new Color( 220 / 255f, 224 / 255f, 230 / 255f ) );
		Paint.SetDefaultFont( 11 );

		float x = 22f;
		if ( !string.IsNullOrEmpty( Icon ) )
		{
			var iconRect = new Rect( x, (Height - 14) / 2f, 14, 14 );
			Paint.DrawIcon( iconRect, Icon, 14 );
			x += 22f;
		}

		var labelRect = new Rect( x, 0, Width - x - 4, Height );
		Paint.DrawText( labelRect, Label, TextFlag.LeftCenter );

		// Hover/press indicator — 2px blue underline at the bottom (#0F3F79).
		if ( _hover || _pressed )
		{
			var alpha = _pressed ? 1.0f : 0.85f;
			Paint.SetBrushAndPen( new Color( 15 / 255f, 63 / 255f, 121 / 255f, alpha ) );
			Paint.DrawRect( new Rect( 0, Height - 2, Width, 2 ) );
		}
	}

	protected override void OnMouseEnter() { _hover = true; Update(); }
	protected override void OnMouseLeave() { _hover = false; _pressed = false; Update(); }
	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton ) { _pressed = true; Update(); }
	}
	protected override void OnMouseReleased( MouseEvent e )
	{
		if ( _pressed && e.LeftMouseButton )
		{
			_pressed = false;
			bool shouldFire = _hover;
			// Callback may rebuild parent and destroy this widget. Avoid
			// touching Qt state afterwards unless we survived.
			if ( shouldFire ) Clicked?.Invoke( ElementType );
			if ( IsValid ) Update();
		}
	}

	protected override void OnDragStart()
	{
		var drag = new Drag( this );
		drag.Data.Object = ElementType;
		drag.Execute();
	}
}

/// <summary>
/// 1px horizontal line that visually separates categories in the palette.
/// </summary>
internal sealed class SuiPaletteCategorySeparator : Widget
{
	public SuiPaletteCategorySeparator( Widget parent = null ) : base( parent )
	{
		FixedHeight = 13;
		SetStyles( "background-color: transparent; border: none;" );
	}

	protected override void OnPaint()
	{
		// 1px horizontal line, full width, centered vertically.
		float yCenter = Height / 2f;
		Paint.SetPen( Color.White.WithAlpha( 0.06f ) );
		Paint.DrawLine( new Vector2( 0, yCenter ), new Vector2( Width, yCenter ) );
	}
}
