using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;
using SboxUiDesigner.EditorUi;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Details dock — right side. Shows properties of the selected element grouped
/// into sections (Identity / Designer / Transform &amp; Layout / Appearance / Type props).
/// When nothing is selected, shows document-wide settings.
///
/// Property editors are hand-built (LineEdit / SuiBoolToggle / SuiEnumPicker)
/// rather than going through ControlSheet, so each edit can be wired through
/// the controller's command stack for undo/redo support. Every property
/// change emits a SuiSetPropertyCommand&lt;T&gt;.
/// </summary>
public class SuiDetailsWidget : Widget
{
	private SuiDocument _document;
	private SuiElement _selected;
	private int _selectedCount = 1;
	private SuiDesignerController _controller;

	private Widget _bodyHost;
	// All AddXRow helpers parent rows under Container() which returns
	// _activeBody when a section is open (set by BeginSection) or _bodyHost
	// otherwise. Reset to _bodyHost on every Refresh.
	private Widget _activeBody;

	private LineEdit _search;
	private string _searchFilter = "";

	// Tracks rows + their labels so search can hide/show without rebuild.
	private readonly System.Collections.Generic.List<(Widget row, string label)> _searchableRows = new();

	public SuiDetailsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Details";
		Name = "SuiDetails";
		MinimumSize = new Vector2( 280, 200 );
		SetStyles( "background-color: transparent; border: none;" );

		Layout = Layout.Column();
		Layout.Margin = new Sandbox.UI.Margin( 8, 8, 8, 8 );
		Layout.Spacing = 0;

		// Search input — same dark look as Palette / Hierarchy.
		_search = new LineEdit( this );
		_search.PlaceholderText = "Search Details";
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
			_searchFilter = (s ?? "").Trim().ToLowerInvariant();
			ApplySearchFilter();
		};
		Layout.Add( _search );

		// 8px gap between search and scroll area.
		var spc = new Widget( this ) { FixedHeight = 8 };
		spc.SetStyles( "background-color: transparent; border: none;" );
		Layout.Add( spc );

		var scroll = new ScrollArea( this );
		SuiScrollStyle.ApplyTo( scroll );
		var canvas = new Widget( scroll );
		canvas.SetStyles( "background-color: transparent; border: none;" );
		canvas.Layout = Layout.Column();
		canvas.Layout.Margin = new Sandbox.UI.Margin( 0, 0, 0, 0 );
		canvas.Layout.Spacing = 0;
		// CRITICAL: sbox-public ShaderGraph Properties uses this exact pattern.
		// Without HorizontalSizeMode = Flexible the canvas tries to grow past
		// the scroll viewport and children with stretch=1 push past the
		// scrollbar regardless of any Margin setting.
		canvas.HorizontalSizeMode = SizeMode.Flexible;
		canvas.VerticalSizeMode = SizeMode.CanGrow;
		scroll.Canvas = canvas;
		_bodyHost = canvas;
		Layout.Add( scroll, 1 );

		Refresh();
	}

	public void SetController( SuiDesignerController controller )
	{
		_controller = controller;
	}

	public void SetDocument( SuiDocument document )
	{
		// Only rebuild when the document INSTANCE actually changed (loading a
		// different .sui). Property edits via SetProp also fire DocumentChanged
		// on the controller, which would otherwise trigger a full Refresh on
		// every keystroke commit — that destroys+recreates all rows and yanks
		// the scroll position to the bottom. The LineEdit/etc already shows
		// the just-typed value, so a rebuild is wasteful in that path.
		// Selection changes use SetSelectedSet, which has its own Refresh.
		// Mode / Anchor changes call Refresh() inline because they affect
		// which rows are visible.
		var changed = !ReferenceEquals( _document, document );
		_document = document;
		if ( changed ) Refresh();
	}

	public void SetSelected( SuiElement element )
	{
		_selected = element;
		_selectedCount = element != null ? 1 : 0;
		Refresh();
	}

	/// <summary>
	/// Update both primary and selection count for multi-select. When count &gt;
	/// 1 the panel shows a header note instead of full per-element editors.
	///
	/// SafeRefresh — this method is called from the controller's
	/// SelectionChanged event, which fires for genuine selection changes AND
	/// for data mutations on the currently-selected element (canvas drag
	/// commits, anchor swaps, etc). Both cases legitimately want a row rebuild.
	/// The Window no longer cascades DocumentChanged into SelectionChanged, so
	/// rapid property edits (e.g. color slider drag) don't trigger this path.
	/// </summary>
	public void SetSelectedSet( SuiElement primary, int count )
	{
		_selected = primary;
		_selectedCount = count;
		Refresh();
	}

	private void Refresh()
	{
		if ( _bodyHost?.Layout == null ) return;
		_bodyHost.Layout.Clear( true );
		_activeBody = _bodyHost;
		_searchableRows.Clear();

		if ( _selectedCount > 1 && _document != null )
		{
			AddNote( $"{_selectedCount} elements selected. Editing multiple at once is V2 — pick a single element to edit its properties, or use the canvas to drag/resize the group." );
		}
		else if ( _selected != null && _document != null )
		{
			BuildElementSections( _selected );
		}
		else if ( _document != null )
		{
			BuildDocumentSections();
		}
		else
		{
			AddNote( "(no document loaded)" );
		}

		_bodyHost.Layout.AddStretchCell();
		_activeBody = _bodyHost;

		// Re-apply current search filter so newly built rows respect it.
		if ( !string.IsNullOrEmpty( _searchFilter ) ) ApplySearchFilter();
	}

	/// <summary>
	/// Open a new collapsible section. Rows added afterwards land inside its
	/// Body. The first call wraps everything; nested sections are not
	/// supported in M8 polish.
	/// </summary>
	private void BeginSection( string title, bool defaultExpanded = true )
	{
		var section = new SuiDetailsSection( title.ToUpperInvariant(), _bodyHost, defaultExpanded );
		_bodyHost.Layout.Add( section );
		_activeBody = section.Body;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Element sections
	// ─────────────────────────────────────────────────────────────────────

	private void BuildElementSections( SuiElement el )
	{
		// Common → Transform → Appearance → (type-specific) → Binding.
		// Designer + Notes sections were removed — Lock / Hidden toggles
		// already live as inline icons on each Hierarchy row, and Notes
		// added clutter without serving a real workflow. "Is Variable" was
		// folded into Binding (it's binding-related state).
		BuildCommonSection( el );
		BuildLayoutSection( el );
		BuildStyleSection( el );
		BuildPropsSection( el );
		BuildBindingSection( el );
	}

	private void BuildBindingSection( SuiElement el )
	{
		BeginSection( "Binding", defaultExpanded: false );
		if ( el.Flags == null ) el.Flags = new SuiElementFlags();

		// "Is Variable" — flags this element as a code-exposed handle for
		// the generated PanelComponent. Lives here because it's only
		// meaningful in a binding context (V1.5 hookup).
		AddBoolRow( "Is Variable", el.Flags.IsVariable,
			v => SetProp( el, e => e.Flags.IsVariable, ( e, v2 ) => e.Flags.IsVariable = v2, v, "Set is-variable" ) );

		if ( _document?.Bindings == null )
		{
			AddNote( "(no document)" );
			return;
		}

		// Find the first binding targeting this element. The full binding
		// list lives in the Bottom Panel → Bindings tab; this section only
		// surfaces the primary binding for quick edit.
		SuiPropertyBinding b = null;
		foreach ( var x in _document.Bindings )
		{
			if ( x.TargetElementId == el.Id ) { b = x; break; }
		}

		AddBoolRow( "IsBound", b != null, on =>
		{
			if ( on && b == null )
			{
				_document.Bindings.Add( new SuiPropertyBinding { TargetElementId = el.Id, Property = "Value", Mode = "OneWay" } );
			}
			else if ( !on && b != null )
			{
				_document.Bindings.Remove( b );
			}
			Refresh();
		} );

		if ( b != null )
		{
			AddTextRow( "Binding Source", b.Source ?? "", v => { b.Source = v; } );
			AddDropdownStringRow( "Binding Mode", b.Mode ?? "OneWay",
				new[] { "OneWay", "TwoWay", "OneTime" },
				v => { b.Mode = v; } );
			AddTextRow( "Binding Path", b.Path ?? "", v => { b.Path = v; } );
			AddTextRow( "Property", b.Property ?? "Value", v => { b.Property = v; } );
		}
	}

	private void AddDropdownStringRow( string label, string current, string[] options, Action<string> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var dd = new SuiDropdownField( row );
		dd.Value = current;
		dd.SetOptions( options );
		dd.ValueSelected += v => onCommit?.Invoke( v );
		ApplyTooltipTo( dd );
		row.Layout.Add( dd, 1 );
		Container().Layout.Add( row );
	}

	// BuildNotesSection / BuildDesignerSection removed — Notes was clutter
	// per user request, and the Designer section (Locked / Hidden in designer /
	// Is Variable) had its toggles already accessible elsewhere: lock + eye
	// icons live on every Hierarchy row, and Is Variable moved into the
	// Binding section where it's contextually relevant.

	private void BuildCommonSection( SuiElement el )
	{
		BeginSection( "Common" );
		AddTextRow( "Name", el.Name ?? "",
			v => { if ( !string.IsNullOrEmpty( v ) ) _controller?.RenameElement( el, v ); } );
		AddTextRow( "Tooltip Text", el.TooltipText ?? "",
			v => SetProp( el, e => e.TooltipText, ( e, v2 ) => e.TooltipText = v2, v, "Set tooltip" ) );

		// Removed:
		//   Is Visible — duplicated Visibility=Collapsed in Appearance
		//   Class      — covered by the Style ref system (V2)
		//   Style      — V2 placeholder, no real wiring yet
	}

	private void BuildLayoutSection( SuiElement el )
	{
		if ( el.Layout == null ) el.Layout = new SuiLayoutData();
		var layout = el.Layout;

		// Root represents the document's drawable area (1920x1080) and always
		// fills the panel — its layout fields are forced by the generator. Showing
		// them in the inspector invites accidental edits that look like they work
		// but offset every child of the document. Hide the section entirely.
		if ( string.IsNullOrEmpty( el.ParentId ) )
			return;

		BeginSection( "Transform" );

		AddEnumRow<SuiLayoutMode>( "Mode", layout.Mode,
			v => { SetProp( el, e => e.Layout.Mode, ( e, v2 ) => e.Layout.Mode = v2, v, "Change layout mode" ); Refresh(); } );

		// Two independent decisions, not one:
		//  1. Position fields (Anchor/X/Y/W/H/Pivot/ZIndex) — controlled by the
		//     PARENT's Mode. Absolute parent → manual position; Flex parent →
		//     the parent flows us, position fields are ignored.
		//  2. Flex container fields (Direction/Justify/Align/Wrap/Gap) —
		//     controlled by THIS element's Mode. Flex mode → I lay out my
		//     children as flex; Absolute mode → my children are positioned.
		// An element can need BOTH (Flex container child of an Absolute parent
		// — common pattern: a Hotbar at BottomCenter on screen, whose children
		// flow as a flex row).
		var parent = _document?.GetElement( el.ParentId );
		var parentMode = parent?.Layout?.Mode ?? SuiLayoutMode.Absolute;
		var positionedByParent = parentMode == SuiLayoutMode.Absolute;
		var isFlexContainer = layout.Mode == SuiLayoutMode.Flex;

		if ( positionedByParent )
		{
			// Position + Size compactos — paired rows (X/Y, W/H) ao invés de 1 por linha.
			AddFloatPairRow( "Position",
				"X", layout.X, v => SetProp( el, e => e.Layout.X, ( e, v2 ) => e.Layout.X = v2, v, "Set X" ),
				"Y", layout.Y, v => SetProp( el, e => e.Layout.Y, ( e, v2 ) => e.Layout.Y = v2, v, "Set Y" ) );
			AddFloatPairRow( "Size",
				"W", layout.Width, v => SetProp( el, e => e.Layout.Width, ( e, v2 ) => e.Layout.Width = v2, v, "Set Width" ),
				"H", layout.Height, v => SetProp( el, e => e.Layout.Height, ( e, v2 ) => e.Layout.Height = v2, v, "Set Height" ) );

			AddAnchorPickerRow( layout.Anchor, v =>
			{
				_controller?.SetAnchor( el, v );
				Refresh();
			} );

			AddFloatPairRow( "Pivot",
				"X", layout.PivotX, v => SetProp( el, e => e.Layout.PivotX, ( e, v2 ) => e.Layout.PivotX = v2, v, "Set pivot X" ),
				"Y", layout.PivotY, v => SetProp( el, e => e.Layout.PivotY, ( e, v2 ) => e.Layout.PivotY = v2, v, "Set pivot Y" ) );

			AddIntRow( "Z Index", layout.ZIndex,
				v => SetProp( el, e => e.Layout.ZIndex, ( e, v2 ) => e.Layout.ZIndex = v2, v, "Set z-index" ) );
		}
		else
		{
			// Parent is Flex — Size still useful as intrinsic flex-item size hint.
			// Position/Anchor/Pivot/Z Index hidden (parent flows us).
			AddFloatPairRow( "Size",
				"W", layout.Width, v => SetProp( el, e => e.Layout.Width, ( e, v2 ) => e.Layout.Width = v2, v, "Set Width" ),
				"H", layout.Height, v => SetProp( el, e => e.Layout.Height, ( e, v2 ) => e.Layout.Height = v2, v, "Set Height" ) );
		}

		if ( isFlexContainer )
		{
			AddEnumRow<SuiFlexDirection>( "Direction", layout.FlexDirection,
				v => SetProp( el, e => e.Layout.FlexDirection, ( e, v2 ) => e.Layout.FlexDirection = v2, v, "Set flex direction" ) );
			AddEnumRow<SuiJustifyContent>( "Justify", layout.JustifyContent,
				v => SetProp( el, e => e.Layout.JustifyContent, ( e, v2 ) => e.Layout.JustifyContent = v2, v, "Set justify-content" ) );
			AddEnumRow<SuiAlignItems>( "Align Items", layout.AlignItems,
				v => SetProp( el, e => e.Layout.AlignItems, ( e, v2 ) => e.Layout.AlignItems = v2, v, "Set align-items" ) );
			AddEnumRow<SuiFlexWrap>( "Wrap", layout.FlexWrap,
				v => SetProp( el, e => e.Layout.FlexWrap, ( e, v2 ) => e.Layout.FlexWrap = v2, v, "Set flex-wrap" ) );
			AddFloatRow( "Gap", layout.Gap,
				v => SetProp( el, e => e.Layout.Gap, ( e, v2 ) => e.Layout.Gap = v2, v, "Set gap" ) );
		}

		// Margin and padding shown as 4-float rows.
		BuildSpacingRows( "Margin", layout.Margin, ( m, v ) => layout.Margin = v, el );
		BuildSpacingRows( "Padding", layout.Padding, ( m, v ) => layout.Padding = v, el );
	}

	private void BuildSpacingRows( string label, SuiSpacing spacing, Action<SuiSpacing, SuiSpacing> setter, SuiElement el )
	{
		spacing ??= new SuiSpacing();
		var snap = spacing; // captured by closures below

		void Commit( SuiSpacing ns, string subLabel )
		{
			SetProp( el,
				e => label == "Margin" ? e.Layout.Margin : e.Layout.Padding,
				( e, sv ) => { if ( label == "Margin" ) e.Layout.Margin = sv; else e.Layout.Padding = sv; },
				ns,
				$"Set {label.ToLower()} {subLabel}" );
		}

		AddFloatQuadRow( label,
			snap.Left, v => { var ns = CloneSpacing( snap ); ns.Left = v; Commit( ns, "left" ); },
			snap.Top, v => { var ns = CloneSpacing( snap ); ns.Top = v; Commit( ns, "top" ); },
			snap.Right, v => { var ns = CloneSpacing( snap ); ns.Right = v; Commit( ns, "right" ); },
			snap.Bottom, v => { var ns = CloneSpacing( snap ); ns.Bottom = v; Commit( ns, "bottom" ); } );
	}

	private static SuiSpacing CloneSpacing( SuiSpacing s )
		=> s == null ? new SuiSpacing() : new SuiSpacing( s.Left, s.Top, s.Right, s.Bottom );

	private void BuildStyleSection( SuiElement el )
	{
		if ( el.Style == null ) el.Style = new SuiStyleData();
		var s = el.Style;

		BeginSection( "Appearance" );
		// "Class Name" was here too — removed because it duplicates the
		// "Name" row in the Common section conceptually for the user. The
		// per-element style class (CSS class ref) lives via the Style
		// dropdown / future style-system instead.
		AddColorRow( "Background", s.BackgroundColor ?? "",
			v => SetProp( el, e => e.Style.BackgroundColor, ( e, v2 ) => e.Style.BackgroundColor = v2, v, "Set bg color" ) );
		AddColorRow( "Border", s.BorderColor ?? "",
			v => SetProp( el, e => e.Style.BorderColor, ( e, v2 ) => e.Style.BorderColor = v2, v, "Set border color" ) );
		AddFloatRow( "Border Width", s.BorderWidth,
			v => SetProp( el, e => e.Style.BorderWidth, ( e, v2 ) => e.Style.BorderWidth = v2, v, "Set border width" ) );
		AddFloatRow( "Border Radius", s.BorderRadius,
			v => SetProp( el, e => e.Style.BorderRadius, ( e, v2 ) => e.Style.BorderRadius = v2, v, "Set border radius" ) );
		AddFloatRow( "Opacity", s.Opacity,
			v => SetProp( el, e => e.Style.Opacity, ( e, v2 ) => e.Style.Opacity = v2, ClampOpacity( v ), "Set opacity" ) );
		AddEnumRow<SuiVisibility>( "Visibility", s.Visibility,
			v => SetProp( el, e => e.Style.Visibility, ( e, v2 ) => e.Style.Visibility = v2, v, "Set visibility" ) );
		AddEnumRow<SuiPointerEvents>( "Pointer Events", s.PointerEvents,
			v => SetProp( el, e => e.Style.PointerEvents, ( e, v2 ) => e.Style.PointerEvents = v2, v, "Set pointer events" ) );
		AddEnumRow<SuiOverflow>( "Overflow", s.Overflow,
			v => SetProp( el, e => e.Style.Overflow, ( e, v2 ) => e.Style.Overflow = v2, v, "Set overflow" ) );
	}

	private static float ClampOpacity( float v ) => v < 0f ? 0f : ( v > 1f ? 1f : v );

	private void BuildPropsSection( SuiElement el )
	{
		if ( el.Props == null ) el.Props = new SuiElementProps();
		var p = el.Props;

		switch ( el.Type )
		{
			case SuiElementType.Text:
				BeginSection( "Text" );
				AddTextRow( "Text", p.Text,
					v => SetProp( el, e => e.Props.Text, ( e, v2 ) => e.Props.Text = v2, v, "Set text" ) );
				AddFloatRow( "Font Size", p.FontSize,
					v => SetProp( el, e => e.Props.FontSize, ( e, v2 ) => e.Props.FontSize = v2, v, "Set font size" ) );
				AddTextRow( "Font Family", p.FontFamily ?? "",
					v => SetProp( el, e => e.Props.FontFamily, ( e, v2 ) => e.Props.FontFamily = v2, v, "Set font family" ) );
				AddEnumRow<SuiFontWeight>( "Font Weight", p.FontWeight,
					v => SetProp( el, e => e.Props.FontWeight, ( e, v2 ) => e.Props.FontWeight = v2, v, "Set font weight" ) );
				AddColorRow( "Color", p.Color ?? "",
					v => SetProp( el, e => e.Props.Color, ( e, v2 ) => e.Props.Color = v2, v, "Set text color" ) );

				// TextSizeMode + alignments — refresh after change so the
				// conditional rows below appear/disappear correctly.
				AddEnumRow<SuiTextSizeMode>( "Size Mode", p.TextSizeMode,
					v =>
					{
						SetProp( el, e => e.Props.TextSizeMode, ( e, v2 ) => e.Props.TextSizeMode = v2, v, "Set text size mode" );
						Refresh();
					} );

				// TextAlign relevant in Fixed + AutoHeightWrap. In Auto, rect == text so it's moot.
				if ( p.TextSizeMode != SuiTextSizeMode.Auto )
				{
					AddEnumRow<SuiTextAlign>( "Align", p.TextAlign,
						v => SetProp( el, e => e.Props.TextAlign, ( e, v2 ) => e.Props.TextAlign = v2, v, "Set text-align" ) );
				}

				// VerticalAlign only in Fixed mode (height auto-grows in others).
				if ( p.TextSizeMode == SuiTextSizeMode.Fixed )
				{
					AddEnumRow<SuiVerticalAlign>( "Vertical Align", p.VerticalAlign,
						v => SetProp( el, e => e.Props.VerticalAlign, ( e, v2 ) => e.Props.VerticalAlign = v2, v, "Set vertical-align" ) );
				}

				AddFloatRow( "Letter Spacing", p.LetterSpacing,
					v => SetProp( el, e => e.Props.LetterSpacing, ( e, v2 ) => e.Props.LetterSpacing = v2, v, "Set letter spacing" ) );
				AddEnumRow<SuiTextOverflow>( "Text Overflow", p.TextOverflow,
					v => SetProp( el, e => e.Props.TextOverflow, ( e, v2 ) => e.Props.TextOverflow = v2, v, "Set text overflow" ) );

				AddBoolRow( "Auto Wrap Text", p.AutoWrapText,
					v =>
					{
						SetProp( el, e => e.Props.AutoWrapText, ( e, v2 ) => e.Props.AutoWrapText = v2, v, "Set auto wrap text" );
						// Auto Wrap implies AutoHeightWrap mode, so flip the size mode too.
						if ( v ) SetProp( el, e => e.Props.TextSizeMode, ( e, v2 ) => e.Props.TextSizeMode = v2, SuiTextSizeMode.AutoHeightWrap, "Set wrap mode" );
						Refresh();
					} );
				if ( p.AutoWrapText )
				{
					AddFloatRow( "Wrap Text At", p.WrapTextAt,
						v => SetProp( el, e => e.Props.WrapTextAt, ( e, v2 ) => e.Props.WrapTextAt = v2, v, "Set wrap width" ) );
				}

				// Mode hint note explaining what the user is in for.
				switch ( p.TextSizeMode )
				{
					case SuiTextSizeMode.Auto:
						AddNote( "Auto: width and height are derived from the text content. Text Width/Height in the Layout section are ignored." );
						break;
					case SuiTextSizeMode.AutoHeightWrap:
						AddNote( "AutoHeightWrap: Width is the max-width for wrapping. Height grows with the number of lines." );
						break;
					case SuiTextSizeMode.Fixed:
						AddNote( "Fixed: you specify Width and Height. Text positions inside the box per Align + Vertical Align." );
						break;
				}
				break;

			case SuiElementType.Image:
			case SuiElementType.ItemIcon:
				BeginSection( "Image" );
				AddImageAssetRow( "Image Path", p.ImagePath ?? "",
					v => SetProp( el, e => e.Props.ImagePath, ( e, v2 ) => e.Props.ImagePath = v2, v, "Set image path" ) );
				AddColorRow( "Tint", p.Tint ?? "",
					v => SetProp( el, e => e.Props.Tint, ( e, v2 ) => e.Props.Tint = v2, v, "Set tint" ) );
				AddEnumRow<SuiImageFitMode>( "Fit Mode", p.FitMode,
					v => SetProp( el, e => e.Props.FitMode, ( e, v2 ) => e.Props.FitMode = v2, v, "Set fit mode" ) );
				AddEnumRow<SuiBackgroundPosition>( "Background Position", p.BackgroundPosition,
					v => SetProp( el, e => e.Props.BackgroundPosition, ( e, v2 ) => e.Props.BackgroundPosition = v2, v, "Set bg position" ) );
				break;

			case SuiElementType.Button:
				BeginSection( "Button" );
				AddTextRow( "Button Text", p.ButtonText ?? "",
					v => SetProp( el, e => e.Props.ButtonText, ( e, v2 ) => e.Props.ButtonText = v2, v, "Set button text" ) );
				break;

			case SuiElementType.Grid:
			case SuiElementType.InventoryGrid:
			case SuiElementType.Hotbar:
				BeginSection( "Grid" );
				AddIntRow( "Columns", p.Columns,
					v => SetProp( el, e => e.Props.Columns, ( e, v2 ) => e.Props.Columns = v2, v, "Set columns" ) );
				AddIntRow( "Rows", p.Rows,
					v => SetProp( el, e => e.Props.Rows, ( e, v2 ) => e.Props.Rows = v2, v, "Set rows" ) );
				AddFloatRow( "Cell Width", p.CellWidth,
					v => SetProp( el, e => e.Props.CellWidth, ( e, v2 ) => e.Props.CellWidth = v2, v, "Set cell width" ) );
				AddFloatRow( "Cell Height", p.CellHeight,
					v => SetProp( el, e => e.Props.CellHeight, ( e, v2 ) => e.Props.CellHeight = v2, v, "Set cell height" ) );
				AddFloatRow( "Gap", p.GridGap,
					v => SetProp( el, e => e.Props.GridGap, ( e, v2 ) => e.Props.GridGap = v2, v, "Set grid gap" ) );
				AddBoolRow( "Auto Fill", p.AutoFill,
					v => SetProp( el, e => e.Props.AutoFill, ( e, v2 ) => e.Props.AutoFill = v2, v, "Set auto-fill" ) );
				AddEnumRow<SuiGridGenerationStrategy>( "Strategy", p.GridStrategy,
					v => SetProp( el, e => e.Props.GridStrategy, ( e, v2 ) => e.Props.GridStrategy = v2, v, "Set grid strategy" ) );
				break;

			case SuiElementType.ProgressBar:
				BeginSection( "Progress Bar" );
				AddFloatRow( "Min", p.ProgressMin,
					v => SetProp( el, e => e.Props.ProgressMin, ( e, v2 ) => e.Props.ProgressMin = v2, v, "Set min" ) );
				AddFloatRow( "Max", p.ProgressMax,
					v => SetProp( el, e => e.Props.ProgressMax, ( e, v2 ) => e.Props.ProgressMax = v2, v, "Set max" ) );
				AddFloatRow( "Preview Value", p.ProgressPreviewValue,
					v => SetProp( el, e => e.Props.ProgressPreviewValue, ( e, v2 ) => e.Props.ProgressPreviewValue = v2, v, "Set preview value" ) );
				AddColorRow( "Fill Color", p.ProgressFillColor ?? "",
					v => SetProp( el, e => e.Props.ProgressFillColor, ( e, v2 ) => e.Props.ProgressFillColor = v2, v, "Set fill color" ) );
				AddEnumRow<SuiProgressDirection>( "Direction", p.ProgressDirection,
					v => SetProp( el, e => e.Props.ProgressDirection, ( e, v2 ) => e.Props.ProgressDirection = v2, v, "Set direction" ) );
				break;

			case SuiElementType.InventorySlot:
				BeginSection( "Inventory Slot" );
				AddIntRow( "Slot Index", p.SlotIndex,
					v => SetProp( el, e => e.Props.SlotIndex, ( e, v2 ) => e.Props.SlotIndex = v2, v, "Set slot index" ) );
				AddImageAssetRow( "Preview Icon", p.PreviewIconPath ?? "",
					v => SetProp( el, e => e.Props.PreviewIconPath, ( e, v2 ) => e.Props.PreviewIconPath = v2, v, "Set preview icon" ) );
				AddIntRow( "Preview Count", p.PreviewCount,
					v => SetProp( el, e => e.Props.PreviewCount, ( e, v2 ) => e.Props.PreviewCount = v2, v, "Set preview count" ) );
				break;
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Document settings (no element selected)
	// ─────────────────────────────────────────────────────────────────────

	private void BuildDocumentSections()
	{
		BeginSection( "Document" );
		AddReadonlyRow( "Name", _document.Name ?? "" );
		AddReadonlyRow( "Id", _document.DocumentId ?? "" );
		AddReadonlyRow( "Schema", $"v{_document.SchemaVersion}" );
		AddReadonlyRow( "Elements", _document.Elements.Count.ToString() );

		if ( _document.Canvas != null )
		{
			BeginSection( "Canvas" );
			AddIntRow( "Base Width", _document.Canvas.BaseWidth,
				v => { _document.Canvas.BaseWidth = v; } );
			AddIntRow( "Base Height", _document.Canvas.BaseHeight,
				v => { _document.Canvas.BaseHeight = v; } );
			AddEnumRow<SuiScaleMode>( "Scale Mode", _document.Canvas.ScaleMode,
				v => { _document.Canvas.ScaleMode = v; } );
		}

		if ( _document.Output != null )
		{
			BeginSection( "Output", defaultExpanded: false );
			AddReadonlyRow( "Configured", _document.Output.Configured.ToString() );
			AddReadonlyRow( "Folder", _document.Output.RootFolder ?? "(not set)" );
			AddTextRow( "Namespace", _document.Output.Namespace ?? "",
				v => _document.Output.Namespace = v );
			AddTextRow( "Class Name", _document.Output.ClassName ?? "",
				v => _document.Output.ClassName = v );
		}

		AddNote( "Edits to canvas/output settings on this panel are not undoable in M5 — they are simple writes. Element edits ARE undoable." );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Generic SetProperty helper — funnels every change through the
	//  controller's command stack so undo/redo works consistently.
	// ─────────────────────────────────────────────────────────────────────

	private void SetProp<T>(
		SuiElement element,
		Func<SuiElement, T> getter,
		Action<SuiElement, T> setter,
		T newValue,
		string description )
	{
		if ( _controller == null )
		{
			// Fall back to direct write so the panel still works in tests.
			setter( element, newValue );
			return;
		}
		_controller.SetProperty( element, getter, setter, newValue, description );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Row builders
	// ─────────────────────────────────────────────────────────────────────

	private void AddSectionHeader( string text )
	{
		// Sub-header inside an already-open section (e.g. the "Notes" subgroup
		// inside Identity). Top-level groups should use BeginSection so the
		// user gets the collapsible chrome.
		var sub = new SuiDetailsSubHeader( text, Container() );
		Container().Layout.Add( sub );
	}

	private void AddNote( string text )
	{
		var lbl = new Label( text, Container() );
		lbl.WordWrap = true;
		lbl.SetStyles( "color: rgb(120,125,135); font-size: 10px; padding: 6px 4px 6px 4px; background-color: transparent; border: none;" );
		Container().Layout.Add( lbl );
	}

	private void AddReadonlyRow( string label, string value )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var v = new Label( value, row );
		v.SetStyles( "color: rgb(220,224,230); font-size: 11px; background-color: transparent; border: none;" );
		row.Layout.Add( v, 1 );
		Container().Layout.Add( row );
	}

	private Widget Container() => _activeBody ?? _bodyHost;

	private void AddTextRow( string label, string value, Action<string> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var f = new SuiTextField( row );
		f.Text = value ?? "";
		f.ValueCommitted += v => onCommit?.Invoke( v ?? "" );
		ApplyTooltipTo( f );
		row.Layout.Add( f, 1 );
		Container().Layout.Add( row );
	}

	/// <summary>
	/// Multi-line text editor — for free-form fields like Notes where the user
	/// might want to paste paragraphs. Uses Editor.TextEdit which gives word-wrap
	/// and a sane editable surface.
	/// </summary>
	private void AddTextAreaRow( string label, string value, Action<string> onCommit, int fixedHeight = 80 )
	{
		AddSectionHeader( label );
		var host = Container();
		var te = new TextEdit( host );
		te.PlainText = value ?? "";
		te.FixedHeight = fixedHeight;
		te.SetStyles(
			"background-color: rgb(20,20,19);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 3px;" +
			"color: rgb(220,224,230);" +
			"padding: 4px 6px;" +
			"font-size: 11px;" );
		te.TextChanged += ( _ ) => onCommit?.Invoke( te.PlainText ?? "" );
		host.Layout.Add( te );
	}

	/// <summary>
	/// Color-field row using <see cref="SuiColorSwatchField"/> — a full-width
	/// color swatch that displays the actual color (not the hex string) with
	/// hex overlay text in a contrasting tone. Click anywhere to open
	/// <see cref="SuiColorPickerPopup"/>; right-click for copy/paste/clear.
	///
	/// Replaces the older Editor.ColorPicker.OpenColorPopup integration which
	/// suffered from SV-gradient stale-state, lag, and intermittent commits
	/// (see ISSUES.md ISSUE-001/003).
	/// </summary>
	private void AddColorRow( string label, string value, Action<string> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );

		var field = new SuiColorSwatchField( row );
		field.SetValue( value ?? "" );
		field.SetCommitHandler( hex =>
		{
			onCommit?.Invoke( hex ?? "" );
			field.SetValue( hex ?? "" );
		} );
		ApplyTooltipTo( field );
		// Fill the row with the swatch (matches the dropdown / text-field
		// stretch behaviour so all rows have aligned right edges).
		row.Layout.Add( field, 1 );

		Container().Layout.Add( row );
	}

	private static string ColorToHex( Color c )
	{
		var r = (int)System.Math.Clamp( c.r * 255f, 0f, 255f );
		var g = (int)System.Math.Clamp( c.g * 255f, 0f, 255f );
		var b = (int)System.Math.Clamp( c.b * 255f, 0f, 255f );
		var a = (int)System.Math.Clamp( c.a * 255f, 0f, 255f );
		return a < 255
			? $"#{r:x2}{g:x2}{b:x2}{a:x2}"
			: $"#{r:x2}{g:x2}{b:x2}";
	}

	/// <summary>
	/// Image asset path row with a "Browse..." button that opens
	/// Editor.AssetPicker filtered to AssetType.ImageFile. The picked asset's
	/// project-relative Path becomes the field value.
	/// </summary>
	private void AddImageAssetRow( string label, string value, Action<string> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );

		var f = new SuiTextField( row );
		f.Text = value ?? "";
		f.ValueCommitted += v => onCommit?.Invoke( v ?? "" );
		ApplyTooltipTo( f );
		row.Layout.Add( f, 1 );

		var browseBtn = new SuiBrowseButton( row );
		ApplyTooltipTo( browseBtn );
		browseBtn.Clicked += () =>
		{
			var picker = AssetPicker.Create( this, AssetType.ImageFile, new()
			{
				EnableMultiselect = false,
				EnableCloud = false
			} );
			picker.Window.StateCookie = "SuiDesigner.ImagePicker";
			picker.Window.RestoreFromStateCookie();
			picker.Window.Title = "Pick image";
			picker.OnAssetPicked = assets =>
			{
				var asset = assets?.FirstOrDefault();
				if ( asset == null ) return;

				// Editor.Asset.Path normalizes to the asset's "compiled source"
				// path which may use a different extension (e.g. always .jpg for
				// image assets) and lowercase the whole thing. We want the actual
				// source filename as it lives on disk so the runtime resource
				// loader resolves it correctly. Derive from AbsolutePath →
				// project-relative.
				string resolved = ResolveSourceRelativePath( asset );

				Log.Info( $"[Sui picker] asset.Name='{asset.Name}', asset.Path='{asset.Path}', asset.AbsolutePath='{asset.AbsolutePath}', resolved='{resolved}'" );

				f.Text = resolved;
				onCommit?.Invoke( f.Text );
			};
			picker.Window.Show();
		};
		row.Layout.Add( browseBtn );

		Container().Layout.Add( row );
	}

	/// <summary>
	/// Editor.Asset.Path is the "canonical" path the engine uses internally,
	/// which for image assets often ends up lowercased and with a normalized
	/// extension (e.g. .jpg) regardless of the source file. The runtime
	/// resource loader expects the actual source filename + case, so we
	/// reconstruct the relative path from the absolute on-disk path.
	/// </summary>
	private static string ResolveSourceRelativePath( Editor.Asset asset )
	{
		if ( asset == null ) return "";
		var abs = asset.AbsolutePath;
		if ( string.IsNullOrEmpty( abs ) ) return asset.Path ?? "";

		// Project-relative root is &lt;project&gt;/Assets/ — strip everything up to
		// and including that prefix and return the remainder.
		var assetsToken = System.IO.Path.DirectorySeparatorChar + "Assets" + System.IO.Path.DirectorySeparatorChar;
		var idx = abs.IndexOf( assetsToken, System.StringComparison.OrdinalIgnoreCase );
		if ( idx < 0 )
		{
			// Try forward-slash variant for cross-platform safety.
			assetsToken = "/Assets/";
			idx = abs.IndexOf( assetsToken, System.StringComparison.OrdinalIgnoreCase );
		}
		if ( idx < 0 ) return asset.Path ?? abs;

		var rel = abs.Substring( idx + assetsToken.Length ).Replace( '\\', '/' );
		return rel;
	}

	private void AddFloatRow( string label, float value, Action<float> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var f = new SuiNumberField( row );
		f.Value = value;
		f.ValueCommitted += v => onCommit?.Invoke( v );
		ApplyTooltipTo( f );
		row.Layout.Add( f, 1 );
		Container().Layout.Add( row );
	}

	private void AddIntRow( string label, int value, Action<int> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var f = new SuiNumberField( row );
		f.Value = value;
		f.ValueCommitted += v => onCommit?.Invoke( (int)System.Math.Round( v ) );
		ApplyTooltipTo( f );
		row.Layout.Add( f, 1 );
		Container().Layout.Add( row );
	}

	private void AddBoolRow( string label, bool value, Action<bool> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var t = new SuiToggleField( value, row );
		t.ValueChanged += v => onCommit?.Invoke( v );
		ApplyTooltipTo( t );
		row.Layout.Add( t, 1 );
		Container().Layout.Add( row );
	}

	private void AddEnumRow<T>( string label, T value, Action<T> onCommit ) where T : struct, Enum
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var dd = new SuiDropdownField( row );
		dd.Value = value.ToString();
		dd.SetOptions( Enum.GetNames( typeof( T ) ) );
		dd.ValueSelected += v =>
		{
			if ( Enum.TryParse<T>( v, out var parsed ) ) onCommit?.Invoke( parsed );
		};
		ApplyTooltipTo( dd );
		row.Layout.Add( dd, 1 );
		Container().Layout.Add( row );
	}

	private Widget MakeRow()
	{
		var row = new Widget( Container() );
		row.SetStyles( "background-color: transparent; border: none;" );
		row.Layout = Layout.Row();
		row.Layout.Margin = new Sandbox.UI.Margin( 0, 2, 0, 2 );
		row.Layout.Spacing = 4;
		row.FixedHeight = 26;
		return row;
	}

	// Tooltip resolved by the most recent AddRowLabel call. ApplyTooltipTo()
	// uses it to stamp the same hover text onto each control that gets added
	// to the row after the label — fields cover the row visually, so tooltip
	// must live on them too, not only on the row container.
	private string _pendingTooltip;

	private void AddRowLabel( Widget row, string text )
	{
		var lbl = new SuiDetailsRowLabel( text, row );
		lbl.FixedWidth = 100;
		row.Layout.Add( lbl );

		_pendingTooltip = SuiDetailsTooltips.Lookup( text );
		if ( !string.IsNullOrEmpty( _pendingTooltip ) )
		{
			row.ToolTip = _pendingTooltip;
			lbl.ToolTip = _pendingTooltip;
		}

		// Register so the search filter can hide rows whose label doesn't match.
		_searchableRows.Add( (row, text.ToLowerInvariant()) );
	}

	/// <summary>
	/// Stamp the row's tooltip onto the given widget — call after creating
	/// each field control so hover anywhere over the row shows the tip.
	/// </summary>
	private void ApplyTooltipTo( Widget w )
	{
		if ( w == null || string.IsNullOrEmpty( _pendingTooltip ) ) return;
		w.ToolTip = _pendingTooltip;
	}

	/// <summary>
	/// Apply the current search filter — hides rows whose label doesn't match.
	/// Called every time the user types in the search box. Cleared rows are
	/// re-shown when the filter is empty.
	/// </summary>
	private void ApplySearchFilter()
	{
		var f = _searchFilter ?? "";
		foreach ( var (row, label) in _searchableRows )
		{
			row.Visible = string.IsNullOrEmpty( f ) || label.Contains( f );
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Compact paired / quad input rows (M14)
	//  Pattern from Editor.RectControlWidget / Vector2ControlWidget — multiple
	//  small float inputs on a single row with sub-labels (X/Y, L/T/R/B, etc).
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Single row with two float inputs side-by-side. Cuts vertical space in
	/// half for paired values like Position X/Y, Size W/H, Pivot X/Y.
	/// </summary>
	private void AddFloatPairRow(
		string label,
		string subA, float valueA, Action<float> onCommitA,
		string subB, float valueB, Action<float> onCommitB )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		// 2 fields × 80px each. Total row: 90 + 4 + (10+80+4)*2 + 4 ≈ 290 px.
		AddMiniFloatField( row, subA, valueA, onCommitA, fieldWidth: 80 );
		AddMiniFloatField( row, subB, valueB, onCommitB, fieldWidth: 80 );
		row.Layout.AddStretchCell();
		Container().Layout.Add( row );
	}

	/// <summary>
	/// Two rows of two float inputs each (Margin / Padding — Left+Right then
	/// Top+Bottom). Single 4-field row was too tight in a 290px column;
	/// splitting gives each field 80px which matches the pair-row look.
	/// </summary>
	private void AddFloatQuadRow(
		string label,
		float vL, Action<float> onL,
		float vT, Action<float> onT,
		float vR, Action<float> onR,
		float vB, Action<float> onB )
	{
		// Row 1: [label] [L] [R]
		var row1 = MakeRow();
		AddRowLabel( row1, label );
		AddMiniFloatField( row1, "L", vL, onL, fieldWidth: 80 );
		AddMiniFloatField( row1, "R", vR, onR, fieldWidth: 80 );
		row1.Layout.AddStretchCell();
		Container().Layout.Add( row1 );

		// Row 2: [spacer matching label] [T] [B]
		var row2 = MakeRow();
		var spacer = new Widget( row2 );
		spacer.SetStyles( "background-color: transparent; border: none;" );
		spacer.FixedWidth = 100; // matches AddRowLabel width so T/B align with L/R
		row2.Layout.Add( spacer );
		AddMiniFloatField( row2, "T", vT, onT, fieldWidth: 80 );
		AddMiniFloatField( row2, "B", vB, onB, fieldWidth: 80 );
		row2.Layout.AddStretchCell();
		Container().Layout.Add( row2 );

		// Search filter looks at row→label map. Register the second row under
		// the same key so it hides/shows together with the first.
		_searchableRows.Add( (row2, label.ToLowerInvariant()) );
	}

	/// <summary>
	/// Compact float input: tiny accent label inside a fixed-width LineEdit
	/// pair. Returns nothing — wires commit on EditingFinished.
	/// </summary>
	private void AddMiniFloatField( Widget row, string accent, float value, Action<float> onCommit, int fieldWidth = 80 )
	{
		var lbl = new SuiVectorLabel( accent, row );
		lbl.FixedWidth = 10;
		ApplyTooltipTo( lbl );
		row.Layout.Add( lbl );

		var f = new SuiNumberField( row );
		f.FixedWidth = fieldWidth;
		f.Value = value;
		f.ValueCommitted += v => onCommit?.Invoke( v );
		ApplyTooltipTo( f );
		row.Layout.Add( f );
	}

	private static string FormatFloat( float v )
	{
		// Trailing zeros look noisy in numeric fields. Show int when no fraction.
		if ( MathF.Abs( v - MathF.Round( v ) ) < 0.0001f ) return ((int)MathF.Round( v )).ToString();
		return v.ToString( "0.###" );
	}

	/// <summary>
	/// Anchor row — compact button showing current anchor by name + small grid
	/// icon indicator. Click opens a popup with the visual 3×3 picker.
	/// Matches the mockup pattern (Image 3 + Image 4) where Anchors collapses
	/// to a single line with a "Bottom Right" / "Top Left" / etc label.
	/// </summary>
	private void AddAnchorPickerRow( SuiAnchor current, Action<SuiAnchor> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, "Anchor" );

		var btn = new SuiAnchorPickerButton( current, row );
		ApplyTooltipTo( btn );
		btn.Clicked += () =>
		{
			// Native dropdown menu (not a Window). The anchor grid is added
			// as a sub-widget so the popup behaves exactly like every other
			// dropdown — auto-closes on outside click, no window chrome.
			var menu = new Menu( btn );
			var picker = new SuiAnchorPicker( null );
			picker.FixedSize = new Vector2( 120, 120 );
			picker.SetCurrent( btn.Anchor );

			// IMPORTANT: close the menu and update the button BEFORE calling
			// onCommit. onCommit triggers Refresh() which destroys this btn;
			// touching SetAnchor / menu.Close after that crashes with
			// "QLabel was null". Order matters.
			void Apply( SuiAnchor a )
			{
				menu.Close();
				if ( btn.IsValid ) btn.SetAnchor( a );
				onCommit?.Invoke( a );
			}

			picker.AnchorSelected = a => Apply( a );
			menu.AddWidget( picker );

			menu.AddSeparator();
			menu.AddOption( "Fill", "open_in_full", () => Apply( SuiAnchor.Stretch ) );
			menu.AddOption( "Stretch Horizontal", "swap_horiz", () => Apply( SuiAnchor.StretchHorizontal ) );
			menu.AddOption( "Stretch Vertical", "swap_vert", () => Apply( SuiAnchor.StretchVertical ) );

			menu.OpenAt( btn.ScreenPosition + new Vector2( 0, btn.Height ) );
		};
		row.Layout.Add( btn, 1 );

		Container().Layout.Add( row );
	}

	private static string AnchorLabel( SuiAnchor a ) => a switch
	{
		SuiAnchor.TopLeft => "Top Left",
		SuiAnchor.TopCenter => "Top Center",
		SuiAnchor.TopRight => "Top Right",
		SuiAnchor.MiddleLeft => "Middle Left",
		SuiAnchor.MiddleCenter => "Middle Center",
		SuiAnchor.MiddleRight => "Middle Right",
		SuiAnchor.BottomLeft => "Bottom Left",
		SuiAnchor.BottomCenter => "Bottom Center",
		SuiAnchor.BottomRight => "Bottom Right",
		SuiAnchor.Stretch => "Fill (Stretch)",
		SuiAnchor.StretchHorizontal => "Stretch Horizontal",
		SuiAnchor.StretchVertical => "Stretch Vertical",
		_ => a.ToString(),
	};
}
