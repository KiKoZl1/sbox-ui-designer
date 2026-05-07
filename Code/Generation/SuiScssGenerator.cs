using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// SCSS emitter. Produces a single .razor.scss file per .sui document, with
/// the document's PanelComponent type name as the outer selector and one
/// nested rule per element. The nested-under-type-selector convention is
/// what every Facepunch sample uses (see ui-razor.md).
///
/// Every emitted property/value pair runs through
/// <see cref="SuiAllowedPropertyList.Validate"/> first; rejected emissions
/// produce a generation error rather than silent garbage in the output.
///
/// Engine constraints enforced (per ui-anti-patterns.md, Confusion 2):
/// - display: only `flex` or `none`
/// - position: only `static`, `relative`, `absolute`
/// - no CSS Grid, no `position: fixed`, no `display: block`
///
/// Visibility mapping (designer abstraction → real CSS):
/// - Visible -> no rule
/// - Hidden -> `opacity: 0` (still occupies layout space)
/// - Collapsed -> `display: none` (removed from layout)
///
/// Pointer-events emitted only when non-default (None == default == no rule).
/// </summary>
public sealed class SuiScssGenerator
{
	private readonly StringBuilder _sb = new();
	private readonly List<string> _errors = new();
	private readonly List<string> _warnings = new();
	private SuiDocument _doc;
	private Dictionary<string, SuiElement> _byId;

	public string Generate( SuiGenerationContext ctx, SuiGenerationResult result )
	{
		_sb.Clear();
		_errors.Clear();
		_warnings.Clear();

		_doc = ctx?.Document;
		if ( _doc == null )
		{
			result.Errors.Add( "scss: document is null" );
			return "";
		}

		_byId = new();
		foreach ( var el in _doc.Elements )
			if ( !string.IsNullOrEmpty( el.Id ) ) _byId[el.Id] = el;

		var typeName = SuiNameSanitizer.ToCSharpIdentifier( ctx.ClassName ?? _doc.Name );
		var root = _doc.GetRoot();

		_sb.Append( SuiHeaderEmitter.EmitScssHeader( _doc ) );
		_sb.Append( '\n' );

		_sb.AppendLine( $"{typeName} {{" );
		if ( root != null )
		{
			EmitElement( root, depth: 1, isRoot: true );
		}
		_sb.AppendLine( "}" );

		foreach ( var e in _errors ) result.Errors.Add( e );
		foreach ( var w in _warnings ) result.Warnings.Add( w );

		return _sb.ToString();
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Per-element emission
	// ─────────────────────────────────────────────────────────────────────

	private void EmitElement( SuiElement el, int depth, bool isRoot )
	{
		if ( el == null ) return;
		var indent = new string( '\t', depth );
		var className = SuiNameSanitizer.ToCssClass( el.Style?.ClassName ?? el.Type.ToString() );

		_sb.Append( indent ).Append( '.' ).Append( className ).AppendLine( " {" );

		// Layout block
		EmitLayout( el, depth + 1, isRoot );

		// Style block
		EmitStyle( el, depth + 1 );

		// Type-specific props that turn into CSS
		EmitTypeProps( el, depth + 1 );

		// Recurse into children
		foreach ( var childId in el.Children )
		{
			if ( _byId.TryGetValue( childId, out var child ) )
			{
				_sb.AppendLine();
				EmitElement( child, depth + 1, isRoot: false );
			}
		}

		_sb.Append( indent ).AppendLine( "}" );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Layout (Absolute / Flex + spacing)
	// ─────────────────────────────────────────────────────────────────────

	private void EmitLayout( SuiElement el, int depth, bool isRoot )
	{
		var l = el.Layout;
		if ( l == null ) return;

		var hasOverlayChildren = el.Type == SuiElementType.Overlay;

		if ( l.Mode == SuiLayoutMode.Absolute )
		{
			Emit( depth, "position", isRoot ? "absolute" : "absolute" );
			// Anchor → left/top/right/bottom (designer abstraction)
			EmitAnchorRules( depth, l );
			if ( l.Width > 0f ) Emit( depth, "width", $"{Px(l.Width)}" );
			if ( l.Height > 0f ) Emit( depth, "height", $"{Px(l.Height)}" );
			if ( l.MinWidth.HasValue ) Emit( depth, "min-width", $"{Px( l.MinWidth.Value )}" );
			if ( l.MinHeight.HasValue ) Emit( depth, "min-height", $"{Px( l.MinHeight.Value )}" );
			if ( l.MaxWidth.HasValue ) Emit( depth, "max-width", $"{Px( l.MaxWidth.Value )}" );
			if ( l.MaxHeight.HasValue ) Emit( depth, "max-height", $"{Px( l.MaxHeight.Value )}" );
			if ( l.ZIndex != 0 ) Emit( depth, "z-index", l.ZIndex.ToString( CultureInfo.InvariantCulture ) );
		}
		else // Flex
		{
			Emit( depth, "display", "flex" );
			Emit( depth, "flex-direction", FlexDirection( l.FlexDirection ) );
			if ( l.JustifyContent != SuiJustifyContent.FlexStart )
				Emit( depth, "justify-content", JustifyContent( l.JustifyContent ) );
			// Default align-items in s&box runtime is `stretch`; only emit if different
			if ( l.AlignItems != SuiAlignItems.Stretch )
				Emit( depth, "align-items", AlignItems( l.AlignItems ) );
			if ( l.FlexWrap != SuiFlexWrap.NoWrap )
				Emit( depth, "flex-wrap", FlexWrap( l.FlexWrap ) );
			if ( l.Gap > 0f ) Emit( depth, "gap", Px( l.Gap ) );
		}

		// Overlay containers are flexed but each child uses absolute — emit
		// position: relative so children with position: absolute anchor here.
		if ( hasOverlayChildren && l.Mode != SuiLayoutMode.Absolute )
			Emit( depth, "position", "relative" );

		// Margin / padding
		EmitSpacing( depth, "margin", l.Margin );
		EmitSpacing( depth, "padding", l.Padding );
	}

	private void EmitAnchorRules( int depth, SuiLayoutData l )
	{
		// Note: PRD doc 08 lays out the canonical anchor → CSS mapping. For MVP
		// we emit explicit left/top from x/y plus width/height. Right-anchored
		// elements use right: instead of left:; centered ones use the
		// transform fallback. Stretch anchors override width/height with 100%.
		switch ( l.Anchor )
		{
			case SuiAnchor.TopLeft:
				Emit( depth, "left", Px( l.X ) );
				Emit( depth, "top", Px( l.Y ) );
				break;

			case SuiAnchor.TopRight:
				Emit( depth, "right", Px( l.X ) );
				Emit( depth, "top", Px( l.Y ) );
				break;

			case SuiAnchor.BottomLeft:
				Emit( depth, "left", Px( l.X ) );
				Emit( depth, "bottom", Px( l.Y ) );
				break;

			case SuiAnchor.BottomRight:
				Emit( depth, "right", Px( l.X ) );
				Emit( depth, "bottom", Px( l.Y ) );
				break;

			case SuiAnchor.TopCenter:
				Emit( depth, "left", "50%" );
				Emit( depth, "top", Px( l.Y ) );
				Emit( depth, "transform", "translateX(-50%)" );
				break;

			case SuiAnchor.BottomCenter:
				Emit( depth, "left", "50%" );
				Emit( depth, "bottom", Px( l.Y ) );
				Emit( depth, "transform", "translateX(-50%)" );
				break;

			case SuiAnchor.MiddleLeft:
				Emit( depth, "left", Px( l.X ) );
				Emit( depth, "top", "50%" );
				Emit( depth, "transform", "translateY(-50%)" );
				break;

			case SuiAnchor.MiddleRight:
				Emit( depth, "right", Px( l.X ) );
				Emit( depth, "top", "50%" );
				Emit( depth, "transform", "translateY(-50%)" );
				break;

			case SuiAnchor.MiddleCenter:
				Emit( depth, "left", "50%" );
				Emit( depth, "top", "50%" );
				Emit( depth, "transform", "translate(-50%, -50%)" );
				break;

			case SuiAnchor.Stretch:
				Emit( depth, "left", "0" );
				Emit( depth, "top", "0" );
				Emit( depth, "right", "0" );
				Emit( depth, "bottom", "0" );
				break;

			case SuiAnchor.StretchHorizontal:
				Emit( depth, "left", "0" );
				Emit( depth, "right", "0" );
				Emit( depth, "top", Px( l.Y ) );
				break;

			case SuiAnchor.StretchVertical:
				Emit( depth, "top", "0" );
				Emit( depth, "bottom", "0" );
				Emit( depth, "left", Px( l.X ) );
				break;
		}
	}

	private void EmitSpacing( int depth, string property, SuiSpacing s )
	{
		if ( s == null || s.IsZero ) return;
		if ( s.IsUniform )
		{
			Emit( depth, property, Px( s.Left ) );
		}
		else
		{
			// Emit shorthand "top right bottom left"
			Emit( depth, property, $"{Px(s.Top)} {Px(s.Right)} {Px(s.Bottom)} {Px(s.Left)}" );
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Style (background, border, opacity, visibility, pointer-events)
	// ─────────────────────────────────────────────────────────────────────

	private void EmitStyle( SuiElement el, int depth )
	{
		var s = el.Style;
		if ( s == null ) return;

		if ( !string.IsNullOrEmpty( s.BackgroundColor ) ) Emit( depth, "background-color", s.BackgroundColor );
		if ( !string.IsNullOrEmpty( s.BorderColor ) ) Emit( depth, "border-color", s.BorderColor );
		if ( s.BorderWidth > 0f )
		{
			Emit( depth, "border-width", Px( s.BorderWidth ) );
			Emit( depth, "border-style", "solid" );
		}
		if ( s.BorderRadius > 0f ) Emit( depth, "border-radius", Px( s.BorderRadius ) );

		// Opacity: only emit if hidden (visibility=Hidden) OR explicitly < 1.
		if ( s.Visibility == SuiVisibility.Hidden )
		{
			Emit( depth, "opacity", "0" );
		}
		else if ( s.Opacity < 0.9999f )
		{
			Emit( depth, "opacity", Float( s.Opacity ) );
		}

		if ( s.Visibility == SuiVisibility.Collapsed )
		{
			Emit( depth, "display", "none" );
		}

		// Pointer-events: only emit if non-default. Default is None.
		if ( s.PointerEvents == SuiPointerEvents.All )
		{
			Emit( depth, "pointer-events", "all" );
		}

		// Overflow: only emit if non-default (Visible).
		if ( s.Overflow == SuiOverflow.Hidden )
			Emit( depth, "overflow", "hidden" );
		else if ( s.Overflow == SuiOverflow.Scroll )
			Emit( depth, "overflow", "scroll" );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Type-specific props (Text/Image/Grid/etc)
	// ─────────────────────────────────────────────────────────────────────

	private void EmitTypeProps( SuiElement el, int depth )
	{
		var p = el.Props;
		if ( p == null ) return;

		switch ( el.Type )
		{
			case SuiElementType.Text:
				if ( p.FontSize > 0f ) Emit( depth, "font-size", Px( p.FontSize ) );
				if ( !string.IsNullOrEmpty( p.FontFamily ) ) Emit( depth, "font-family", p.FontFamily );
				if ( p.FontWeight != SuiFontWeight.Normal ) Emit( depth, "font-weight", FontWeight( p.FontWeight ) );
				if ( !string.IsNullOrEmpty( p.Color ) ) Emit( depth, "color", p.Color );
				if ( p.TextAlign != SuiTextAlign.Left ) Emit( depth, "text-align", TextAlign( p.TextAlign ) );
				if ( p.LineHeight.HasValue ) Emit( depth, "line-height", Float( p.LineHeight.Value ) );
				if ( p.LetterSpacing != 0f ) Emit( depth, "letter-spacing", Px( p.LetterSpacing ) );
				if ( p.TextOverflow != SuiTextOverflow.Clip )
					Emit( depth, "text-overflow", p.TextOverflow == SuiTextOverflow.Ellipsis ? "ellipsis" : "clip" );
				break;

			case SuiElementType.Image:
			case SuiElementType.ItemIcon:
				if ( !string.IsNullOrEmpty( p.ImagePath ) )
				{
					Emit( depth, "background-image", $"url(\"{p.ImagePath}\")" );
					Emit( depth, "background-size", FitMode( p.FitMode ) );
					Emit( depth, "background-position", BgPosition( p.BackgroundPosition ) );
					if ( !string.IsNullOrEmpty( p.Tint ) && p.Tint != "#ffffff" && p.Tint != "#FFFFFF" )
						Emit( depth, "background-image-tint", p.Tint );
				}
				break;

			case SuiElementType.Grid:
			case SuiElementType.InventoryGrid:
			case SuiElementType.Hotbar:
				// Wrapped-flex strategy (PRD doc 08 strategy A).
				if ( p.Columns > 0 && p.Rows > 0 && p.CellWidth > 0f && p.CellHeight > 0f )
				{
					Emit( depth, "display", "flex" );
					Emit( depth, "flex-direction", "row" );
					Emit( depth, "flex-wrap", el.Type == SuiElementType.Hotbar ? "nowrap" : "wrap" );
					Emit( depth, "gap", Px( p.GridGap ) );
					var w = p.Columns * p.CellWidth + (p.Columns - 1) * p.GridGap;
					var h = p.Rows * p.CellHeight + (p.Rows - 1) * p.GridGap;
					Emit( depth, "width", Px( w ) );
					Emit( depth, "height", Px( h ) );
				}
				break;

			case SuiElementType.ProgressBar:
				// Bare container background; M9 doesn't generate the inner fill div
				// — V1 will. Just ensures the bar shape exists.
				break;
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Emit helper with allowed-property validation
	// ─────────────────────────────────────────────────────────────────────

	private void Emit( int depth, string property, string value )
	{
		var err = SuiAllowedPropertyList.Validate( property, value );
		if ( err != null )
		{
			_errors.Add( $"scss emit blocked: {err}" );
			return;
		}
		var indent = new string( '\t', depth );
		_sb.Append( indent ).Append( property ).Append( ": " ).Append( value ).AppendLine( ";" );
	}

	private static string Px( float v ) => v == 0f ? "0" : v.ToString( "0.##", CultureInfo.InvariantCulture ) + "px";
	private static string Float( float v ) => v.ToString( "0.###", CultureInfo.InvariantCulture );

	// ─────────────────────────────────────────────────────────────────────
	//  Enum → CSS keyword
	// ─────────────────────────────────────────────────────────────────────

	private static string FlexDirection( SuiFlexDirection d ) => d switch
	{
		SuiFlexDirection.Row => "row",
		SuiFlexDirection.Column => "column",
		SuiFlexDirection.RowReverse => "row-reverse",
		SuiFlexDirection.ColumnReverse => "column-reverse",
		_ => "row",
	};

	private static string JustifyContent( SuiJustifyContent j ) => j switch
	{
		SuiJustifyContent.FlexStart => "flex-start",
		SuiJustifyContent.Center => "center",
		SuiJustifyContent.FlexEnd => "flex-end",
		SuiJustifyContent.SpaceBetween => "space-between",
		SuiJustifyContent.SpaceAround => "space-around",
		SuiJustifyContent.SpaceEvenly => "space-evenly",
		_ => "flex-start",
	};

	private static string AlignItems( SuiAlignItems a ) => a switch
	{
		SuiAlignItems.FlexStart => "flex-start",
		SuiAlignItems.Center => "center",
		SuiAlignItems.FlexEnd => "flex-end",
		SuiAlignItems.Stretch => "stretch",
		SuiAlignItems.Baseline => "baseline",
		_ => "stretch",
	};

	private static string FlexWrap( SuiFlexWrap w ) => w switch
	{
		SuiFlexWrap.NoWrap => "nowrap",
		SuiFlexWrap.Wrap => "wrap",
		SuiFlexWrap.WrapReverse => "wrap-reverse",
		_ => "nowrap",
	};

	private static string TextAlign( SuiTextAlign a ) => a switch
	{
		SuiTextAlign.Left => "left",
		SuiTextAlign.Center => "center",
		SuiTextAlign.Right => "right",
		SuiTextAlign.Justify => "justify",
		_ => "left",
	};

	private static string FontWeight( SuiFontWeight w ) => w switch
	{
		SuiFontWeight.Light => "300",
		SuiFontWeight.Normal => "400",
		SuiFontWeight.Medium => "500",
		SuiFontWeight.SemiBold => "600",
		SuiFontWeight.Bold => "700",
		SuiFontWeight.ExtraBold => "800",
		_ => "400",
	};

	private static string FitMode( SuiImageFitMode m ) => m switch
	{
		SuiImageFitMode.Contain => "contain",
		SuiImageFitMode.Cover => "cover",
		SuiImageFitMode.Stretch => "100% 100%",
		SuiImageFitMode.None => "auto",
		_ => "contain",
	};

	private static string BgPosition( SuiBackgroundPosition p ) => p switch
	{
		SuiBackgroundPosition.Center => "center",
		SuiBackgroundPosition.Top => "top",
		SuiBackgroundPosition.Bottom => "bottom",
		SuiBackgroundPosition.Left => "left",
		SuiBackgroundPosition.Right => "right",
		SuiBackgroundPosition.TopLeft => "left top",
		SuiBackgroundPosition.TopRight => "right top",
		SuiBackgroundPosition.BottomLeft => "left bottom",
		SuiBackgroundPosition.BottomRight => "right bottom",
		_ => "center",
	};
}
