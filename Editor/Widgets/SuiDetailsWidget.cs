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
/// change emits a <see cref="SuiSetPropertyCommand{T}"/>.
/// </summary>
public class SuiDetailsWidget : Widget
{
	private SuiDocument _document;
	private SuiElement _selected;
	private SuiDesignerController _controller;

	private Widget _bodyHost;

	public SuiDetailsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Details";
		Name = "SuiDetails";
		MinimumSize = new Vector2( 280, 200 );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		var header = new Label( "Details", this );
		header.SetStyles( "padding: 6px 8px; font-weight: bold; color: #e5e7eb;" );
		Layout.Add( header );

		var scroll = new ScrollArea( this );
		scroll.Canvas = new Widget( null );
		scroll.Canvas.Layout = Layout.Column();
		scroll.Canvas.Layout.Margin = new Sandbox.UI.Margin( 8, 4, 8, 8 );
		scroll.Canvas.Layout.Spacing = 0;
		_bodyHost = scroll.Canvas;
		Layout.Add( scroll, 1 );

		Refresh();
	}

	public void SetController( SuiDesignerController controller )
	{
		_controller = controller;
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		Refresh();
	}

	public void SetSelected( SuiElement element )
	{
		_selected = element;
		Refresh();
	}

	private void Refresh()
	{
		if ( _bodyHost?.Layout == null ) return;
		_bodyHost.Layout.Clear( true );
		_activeBody = _bodyHost;

		if ( _selected != null && _document != null )
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
	}

	/// <summary>
	/// Open a new collapsible section. Rows added afterwards land inside its
	/// Body. The first call wraps everything; nested sections are not
	/// supported in M8 polish.
	/// </summary>
	private void BeginSection( string title, bool defaultExpanded = true )
	{
		var section = new SuiCollapsibleSection( title, _bodyHost, defaultExpanded );
		_bodyHost.Layout.Add( section );
		_activeBody = section.Body;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Element sections
	// ─────────────────────────────────────────────────────────────────────

	private void BuildElementSections( SuiElement el )
	{
		BuildIdentitySection( el );
		BuildDesignerSection( el );
		BuildLayoutSection( el );
		BuildStyleSection( el );
		BuildPropsSection( el );
	}

	private void BuildIdentitySection( SuiElement el )
	{
		BeginSection( "Identity" );
		AddTextRow( "Name", el.Name, v => SetProp( el, e => e.Name, ( e, v2 ) => e.Name = v2, v, "Rename" ) );
		AddReadonlyRow( "Id", el.Id );
		AddReadonlyRow( "Type", el.Type.ToString() );
		AddReadonlyRow( "Parent", el.ParentId ?? "(root)" );
		AddTextAreaRow( "Notes", el.Notes ?? "",
			v => SetProp( el, e => e.Notes, ( e, v2 ) => e.Notes = v2, v, "Set notes" ),
			fixedHeight: 60 );
	}

	private void BuildDesignerSection( SuiElement el )
	{
		if ( el.Flags == null ) el.Flags = new SuiElementFlags();
		BeginSection( "Designer", defaultExpanded: false );
		AddBoolRow( "Locked", el.Flags.Locked,
			v => SetProp( el, e => e.Flags.Locked, ( e, v2 ) => e.Flags.Locked = v2, v, "Set locked" ) );
		AddBoolRow( "Hidden in designer", el.Flags.HiddenInDesigner,
			v => SetProp( el, e => e.Flags.HiddenInDesigner, ( e, v2 ) => e.Flags.HiddenInDesigner = v2, v, "Set hidden" ) );
		AddBoolRow( "Is Variable (V1.5)", el.Flags.IsVariable,
			v => SetProp( el, e => e.Flags.IsVariable, ( e, v2 ) => e.Flags.IsVariable = v2, v, "Set is-variable" ) );
	}

	private void BuildLayoutSection( SuiElement el )
	{
		if ( el.Layout == null ) el.Layout = new SuiLayoutData();
		var layout = el.Layout;

		BeginSection( "Transform & Layout" );

		AddEnumRow<SuiLayoutMode>( "Mode", layout.Mode,
			v => { SetProp( el, e => e.Layout.Mode, ( e, v2 ) => e.Layout.Mode = v2, v, "Change layout mode" ); Refresh(); } );

		if ( layout.Mode == SuiLayoutMode.Absolute )
		{
			AddFloatRow( "X", layout.X,
				v => SetProp( el, e => e.Layout.X, ( e, v2 ) => e.Layout.X = v2, v, "Set X" ) );
			AddFloatRow( "Y", layout.Y,
				v => SetProp( el, e => e.Layout.Y, ( e, v2 ) => e.Layout.Y = v2, v, "Set Y" ) );
			AddFloatRow( "Width", layout.Width,
				v => SetProp( el, e => e.Layout.Width, ( e, v2 ) => e.Layout.Width = v2, v, "Set Width" ) );
			AddFloatRow( "Height", layout.Height,
				v => SetProp( el, e => e.Layout.Height, ( e, v2 ) => e.Layout.Height = v2, v, "Set Height" ) );
			AddEnumRow<SuiAnchor>( "Anchor", layout.Anchor,
				v => SetProp( el, e => e.Layout.Anchor, ( e, v2 ) => e.Layout.Anchor = v2, v, "Set anchor" ) );
			AddFloatRow( "Pivot X", layout.PivotX,
				v => SetProp( el, e => e.Layout.PivotX, ( e, v2 ) => e.Layout.PivotX = v2, v, "Set pivot X" ) );
			AddFloatRow( "Pivot Y", layout.PivotY,
				v => SetProp( el, e => e.Layout.PivotY, ( e, v2 ) => e.Layout.PivotY = v2, v, "Set pivot Y" ) );
			AddIntRow( "Z Index", layout.ZIndex,
				v => SetProp( el, e => e.Layout.ZIndex, ( e, v2 ) => e.Layout.ZIndex = v2, v, "Set z-index" ) );
		}
		else
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
		AddFloatRow( $"{label} Left", spacing.Left,
			v => { var ns = CloneSpacing( spacing ); ns.Left = v; SetProp( el,
				e => label == "Margin" ? e.Layout.Margin : e.Layout.Padding,
				( e, sv ) => { if ( label == "Margin" ) e.Layout.Margin = sv; else e.Layout.Padding = sv; },
				ns, $"Set {label.ToLower()} left" ); } );
		AddFloatRow( $"{label} Top", spacing.Top,
			v => { var ns = CloneSpacing( spacing ); ns.Top = v; SetProp( el,
				e => label == "Margin" ? e.Layout.Margin : e.Layout.Padding,
				( e, sv ) => { if ( label == "Margin" ) e.Layout.Margin = sv; else e.Layout.Padding = sv; },
				ns, $"Set {label.ToLower()} top" ); } );
		AddFloatRow( $"{label} Right", spacing.Right,
			v => { var ns = CloneSpacing( spacing ); ns.Right = v; SetProp( el,
				e => label == "Margin" ? e.Layout.Margin : e.Layout.Padding,
				( e, sv ) => { if ( label == "Margin" ) e.Layout.Margin = sv; else e.Layout.Padding = sv; },
				ns, $"Set {label.ToLower()} right" ); } );
		AddFloatRow( $"{label} Bottom", spacing.Bottom,
			v => { var ns = CloneSpacing( spacing ); ns.Bottom = v; SetProp( el,
				e => label == "Margin" ? e.Layout.Margin : e.Layout.Padding,
				( e, sv ) => { if ( label == "Margin" ) e.Layout.Margin = sv; else e.Layout.Padding = sv; },
				ns, $"Set {label.ToLower()} bottom" ); } );
	}

	private static SuiSpacing CloneSpacing( SuiSpacing s )
		=> s == null ? new SuiSpacing() : new SuiSpacing( s.Left, s.Top, s.Right, s.Bottom );

	private void BuildStyleSection( SuiElement el )
	{
		if ( el.Style == null ) el.Style = new SuiStyleData();
		var s = el.Style;

		BeginSection( "Appearance" );
		AddTextRow( "Class Name", s.ClassName ?? "",
			v => SetProp( el, e => e.Style.ClassName, ( e, v2 ) => e.Style.ClassName = v2, v, "Set class name" ) );
		AddColorRow( "Background Color", s.BackgroundColor ?? "",
			v => SetProp( el, e => e.Style.BackgroundColor, ( e, v2 ) => e.Style.BackgroundColor = v2, v, "Set bg color" ) );
		AddColorRow( "Border Color", s.BorderColor ?? "",
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
				AddEnumRow<SuiTextAlign>( "Align", p.TextAlign,
					v => SetProp( el, e => e.Props.TextAlign, ( e, v2 ) => e.Props.TextAlign = v2, v, "Set text-align" ) );
				AddFloatRow( "Letter Spacing", p.LetterSpacing,
					v => SetProp( el, e => e.Props.LetterSpacing, ( e, v2 ) => e.Props.LetterSpacing = v2, v, "Set letter spacing" ) );
				AddEnumRow<SuiTextOverflow>( "Text Overflow", p.TextOverflow,
					v => SetProp( el, e => e.Props.TextOverflow, ( e, v2 ) => e.Props.TextOverflow = v2, v, "Set text overflow" ) );
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
		var lbl = new Label( text.ToUpperInvariant(), Container() );
		lbl.SetStyles( "color: #9ca3af; font-size: 10px; font-weight: bold; padding-top: 6px; padding-bottom: 2px; letter-spacing: 1px;" );
		Container().Layout.Add( lbl );
	}

	private void AddNote( string text )
	{
		var lbl = new Label( text, Container() );
		lbl.WordWrap = true;
		lbl.SetStyles( "color: #6b7280; font-size: 10px; padding-top: 8px;" );
		Container().Layout.Add( lbl );
	}

	private void AddReadonlyRow( string label, string value )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var v = new Label( value, row );
		v.SetStyles( "color: #d1d5db; font-size: 11px;" );
		row.Layout.Add( v, 1 );
		Container().Layout.Add( row );
	}

	private Widget Container() => _activeBody ?? _bodyHost;

	private void AddTextRow( string label, string value, Action<string> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var le = new LineEdit( row );
		le.Text = value ?? "";
		le.EditingFinished += () => onCommit?.Invoke( le.Text ?? "" );
		row.Layout.Add( le, 1 );
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
		// TextEdit.TextChanged is Action<string>; pull the current PlainText
		// off the widget so we don't depend on the arg's semantics.
		te.TextChanged += ( _ ) => onCommit?.Invoke( te.PlainText ?? "" );
		host.Layout.Add( te );
	}

	/// <summary>
	/// Hex-color row with a clickable swatch that opens Editor.ColorPicker via
	/// OpenColorPopup. The hex string is parsed via Color.TryParse on entry and
	/// rendered as a 24px swatch beside the LineEdit. Empty/invalid strings
	/// fall back to white.
	/// </summary>
	private void AddColorRow( string label, string value, Action<string> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );

		var le = new LineEdit( row );
		le.Text = value ?? "";
		le.PlaceholderText = "#rrggbb or #rrggbbaa";
		row.Layout.Add( le, 1 );

		var swatch = new Button( "", "palette", row );
		swatch.FixedWidth = 32;
		swatch.ToolTip = "Open color picker";

		// Visual swatch preview — colour the button's left edge with the current value.
		void PaintSwatch()
		{
			if ( !string.IsNullOrEmpty( le.Text ) && Color.TryParse( le.Text, out var c ) )
			{
				swatch.SetStyles( $"background-color: rgba({(int)(c.r*255)},{(int)(c.g*255)},{(int)(c.b*255)},{c.a});" );
			}
			else
			{
				swatch.SetStyles( "" );
			}
		}
		PaintSwatch();

		// Commit when LineEdit loses focus — keep typed-text path working.
		le.EditingFinished += () =>
		{
			onCommit?.Invoke( le.Text ?? "" );
			PaintSwatch();
		};

		// Swatch click opens the popup. ValueChanged fires per-channel-tweak
		// while the user drags; we only commit on EditingFinished to avoid
		// flooding the command stack with one push per micro-change.
		swatch.Clicked += () =>
		{
			Color startColor = Color.White;
			if ( !string.IsNullOrEmpty( le.Text ) ) Color.TryParse( le.Text, out startColor );

			var picker = ColorPicker.OpenColorPopup( startColor, c =>
			{
				le.Text = ColorToHex( c );
				PaintSwatch();
			} );

			if ( picker != null )
			{
				picker.EditingFinished += () =>
				{
					onCommit?.Invoke( le.Text ?? "" );
					PaintSwatch();
				};
			}
		};

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

		var le = new LineEdit( row );
		le.Text = value ?? "";
		le.PlaceholderText = "ui/icons/example.png";
		le.EditingFinished += () => onCommit?.Invoke( le.Text ?? "" );
		row.Layout.Add( le, 1 );

		var browseBtn = new Button( "", "folder_open", row );
		browseBtn.FixedWidth = 32;
		browseBtn.ToolTip = "Browse images…";
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
				le.Text = asset.Path ?? "";
				onCommit?.Invoke( le.Text );
			};
			picker.Window.Show();
		};
		row.Layout.Add( browseBtn );

		Container().Layout.Add( row );
	}

	private void AddFloatRow( string label, float value, Action<float> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var le = new LineEdit( row );
		le.Text = value.ToString( System.Globalization.CultureInfo.InvariantCulture );
		le.EditingFinished += () =>
		{
			if ( float.TryParse( le.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f ) )
				onCommit?.Invoke( f );
			else
				le.Text = value.ToString( System.Globalization.CultureInfo.InvariantCulture );
		};
		row.Layout.Add( le, 1 );
		Container().Layout.Add( row );
	}

	private void AddIntRow( string label, int value, Action<int> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var le = new LineEdit( row );
		le.Text = value.ToString( System.Globalization.CultureInfo.InvariantCulture );
		le.EditingFinished += () =>
		{
			if ( int.TryParse( le.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var i ) )
				onCommit?.Invoke( i );
			else
				le.Text = value.ToString( System.Globalization.CultureInfo.InvariantCulture );
		};
		row.Layout.Add( le, 1 );
		Container().Layout.Add( row );
	}

	private void AddBoolRow( string label, bool value, Action<bool> onCommit )
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var btn = new Button( value ? "On" : "Off", value ? "check_box" : "check_box_outline_blank", row );
		var captured = value;
		btn.Clicked += () =>
		{
			captured = !captured;
			btn.Text = captured ? "On" : "Off";
			btn.Icon = captured ? "check_box" : "check_box_outline_blank";
			onCommit?.Invoke( captured );
		};
		row.Layout.Add( btn, 1 );
		Container().Layout.Add( row );
	}

	private void AddEnumRow<T>( string label, T value, Action<T> onCommit ) where T : struct, Enum
	{
		var row = MakeRow();
		AddRowLabel( row, label );
		var btn = new Button( value.ToString(), "arrow_drop_down", row );
		btn.Clicked += () =>
		{
			var menu = new Menu( btn );
			foreach ( var name in Enum.GetNames( typeof( T ) ) )
			{
				var captured = name;
				menu.AddOption( name, "", () =>
				{
					if ( Enum.TryParse<T>( captured, out var parsed ) )
					{
						btn.Text = captured;
						onCommit?.Invoke( parsed );
					}
				} );
			}
			menu.OpenAtCursor( true );
		};
		row.Layout.Add( btn, 1 );
		Container().Layout.Add( row );
	}

	private Widget MakeRow()
	{
		var row = new Widget( Container() );
		row.Layout = Layout.Row();
		row.Layout.Margin = new Sandbox.UI.Margin( 0, 2, 0, 2 );
		row.Layout.Spacing = 6;
		return row;
	}

	private void AddRowLabel( Widget row, string text )
	{
		var lbl = new Label( text, row );
		lbl.FixedWidth = 110;
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );
	}
}
