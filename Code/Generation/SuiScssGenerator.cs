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

		// User-owned sidecar — emitted ONLY in Final mode (compile-to-output).
		// SuiCompileWriter creates <typeName>.User.scss once and never overwrites,
		// so the @import always resolves at runtime. In Preview mode the cache
		// writer doesn't emit a sidecar; importing a missing file silently breaks
		// SCSS compilation and the panel ends up unstyled, so we skip the import.
		if ( ctx.Mode == SuiGenerationMode.Final )
		{
			_sb.AppendLine();
			_sb.AppendLine( $"// User-protected styles for {typeName} — safe to edit." );
			_sb.AppendLine( $"@import \"{typeName}.User.scss\";" );
		}

		foreach ( var e in _errors ) result.Errors.Add( e );
		foreach ( var w in _warnings ) result.Warnings.Add( w );

		return _sb.ToString();
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Per-element emission
	// ─────────────────────────────────────────────────────────────────────

	private void EmitElement( SuiElement el, int depth, bool isRoot, SuiLayoutMode parentMode = SuiLayoutMode.Absolute )
	{
		if ( el == null ) return;
		var indent = new string( ' ', depth * 2 );
		// Use the per-element UNIQUE class (sui-<id>) as the selector, not
		// the user-provided ClassName which is intentionally shared across
		// siblings. Without this, multiple slots all called ".slot" would
		// cascade-overwrite each other and only the last rule would win.
		var selectorClass = SuiRazorGenerator.ElementUniqueClass( el );

		_sb.Append( indent ).Append( '.' ).Append( selectorClass ).AppendLine( " {" );

		// Layout block — parent's mode controls whether THIS element is positioned
		// (parent=Absolute) or flowed (parent=Flex).
		EmitLayout( el, depth + 1, isRoot, parentMode );

		// Style block
		EmitStyle( el, depth + 1 );

		// Type-specific props that turn into CSS
		EmitTypeProps( el, depth + 1 );

		// V1.5 M3.5 (PRD 25) — interactive state CSS. Emit transition,
		// cursor, and pseudo-class blocks for Button / InventorySlot /
		// ItemIcon. Order matters: :hover < :focus < :active < .disabled
		// so the disabled class wins specificity-wise when the user
		// authors overlapping properties (decision logged in PRD 25 § 6).
		EmitInteractiveStates( el, depth + 1 );

		// Recurse into children — pass THIS element's mode so children know
		// whether their parent flows them or positions them.
		// Grid/InventoryGrid/Hotbar are flex containers via EmitTypeProps even
		// when Layout.Mode == Absolute, so their children must be treated as
		// flex items (no position:absolute) or they all stack at (0,0).
		var childParentMode = el.Layout?.Mode ?? SuiLayoutMode.Absolute;
		if ( el.Type == SuiElementType.Grid
			|| el.Type == SuiElementType.InventoryGrid
			|| el.Type == SuiElementType.Hotbar )
		{
			childParentMode = SuiLayoutMode.Flex;
		}
		foreach ( var childId in el.Children )
		{
			if ( _byId.TryGetValue( childId, out var child ) )
			{
				_sb.AppendLine();
				EmitElement( child, depth + 1, isRoot: false, parentMode: childParentMode );
			}
		}

		_sb.Append( indent ).AppendLine( "}" );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Layout (Absolute / Flex + spacing)
	// ─────────────────────────────────────────────────────────────────────

	private void EmitLayout( SuiElement el, int depth, bool isRoot, SuiLayoutMode parentMode )
	{
		var l = el.Layout;
		if ( l == null ) return;

		var hasOverlayChildren = el.Type == SuiElementType.Overlay;

		// Decide whether THIS element is "positioned" (position:absolute + anchor)
		// or "flowed" (a flex item under its parent). Root always positions itself.
		// Otherwise: parent.Mode == Absolute  → position absolute via anchor.
		//            parent.Mode == Flex      → flex item, no position, no x/y.
		var positionedByParent = isRoot || parentMode == SuiLayoutMode.Absolute;

		if ( positionedByParent )
		{
			Emit( depth, "position", "absolute" );

			if ( isRoot )
			{
				// Root always fills the panel — it represents the document's
				// 1920x1080 drawable area. Stretch via right:0;bottom:0 instead
				// of explicit width/height because the runtime world panel's
				// own internal Scale interacts unpredictably with explicit px
				// widths on root.
				Emit( depth, "left", "0" );
				Emit( depth, "top", "0" );
				Emit( depth, "right", "0" );
				Emit( depth, "bottom", "0" );
			}
			else
			{
				EmitAnchorRules( depth, l, el );

				// For Stretch anchors, Layout.Width / Layout.Height are MARGINS
				// (right/bottom), not the element's box size — EmitAnchorRules
				// already wrote them as `right: …` / `bottom: …`. Emitting them
				// here as `width/height` again would shrink the element to that
				// margin value (e.g. width: 8px) and defeat the stretch.
				var isStretchAnchor =
					l.Anchor == SuiAnchor.Stretch ||
					l.Anchor == SuiAnchor.StretchHorizontal ||
					l.Anchor == SuiAnchor.StretchVertical;
				if ( !isStretchAnchor )
				{
					if ( l.Width > 0f ) Emit( depth, "width", $"{Px(l.Width)}" );
					if ( l.Height > 0f ) Emit( depth, "height", $"{Px(l.Height)}" );
				}
				else
				{
					// StretchHorizontal: Height (cross axis) is still a real size.
					// StretchVertical: Width (cross axis) is still a real size.
					// Stretch: both axes are margins, neither emitted.
					if ( l.Anchor == SuiAnchor.StretchHorizontal && l.Height > 0f )
						Emit( depth, "height", $"{Px( l.Height )}" );
					if ( l.Anchor == SuiAnchor.StretchVertical && l.Width > 0f )
						Emit( depth, "width", $"{Px( l.Width )}" );
				}
				if ( l.MinWidth.HasValue ) Emit( depth, "min-width", $"{Px( l.MinWidth.Value )}" );
				if ( l.MinHeight.HasValue ) Emit( depth, "min-height", $"{Px( l.MinHeight.Value )}" );
				if ( l.MaxWidth.HasValue ) Emit( depth, "max-width", $"{Px( l.MaxWidth.Value )}" );
				if ( l.MaxHeight.HasValue ) Emit( depth, "max-height", $"{Px( l.MaxHeight.Value )}" );
				if ( l.ZIndex != 0 ) Emit( depth, "z-index", l.ZIndex.ToString( CultureInfo.InvariantCulture ) );
			}
		}
		else
		{
			// Flex item — let the parent's flex layout place us. Emit
			// position: relative so any absolute-positioned descendants
			// (e.g. an Image child with Stretch anchor) anchor to THIS
			// element's box, not to a distant positioned ancestor like the
			// root card. Without this, all such descendants overlap at the
			// nearest positioned ancestor (visible as one icon stretching
			// across the whole card with the others hidden behind it).
			Emit( depth, "position", "relative" );
			if ( l.Width > 0f ) Emit( depth, "width", $"{Px(l.Width)}" );
			if ( l.Height > 0f ) Emit( depth, "height", $"{Px(l.Height)}" );
			if ( l.MinWidth.HasValue ) Emit( depth, "min-width", $"{Px( l.MinWidth.Value )}" );
			if ( l.MinHeight.HasValue ) Emit( depth, "min-height", $"{Px( l.MinHeight.Value )}" );
			if ( l.MaxWidth.HasValue ) Emit( depth, "max-width", $"{Px( l.MaxWidth.Value )}" );
			if ( l.MaxHeight.HasValue ) Emit( depth, "max-height", $"{Px( l.MaxHeight.Value )}" );
		}

		// If THIS element is a Flex container, emit display:flex + its props
		// regardless of how it's positioned by its parent (a flex item can itself
		// be a flex container — that's how rows-inside-columns work).
		if ( l.Mode == SuiLayoutMode.Flex )
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
		if ( hasOverlayChildren && l.Mode != SuiLayoutMode.Absolute && positionedByParent == false )
			Emit( depth, "position", "relative" );

		// Margin / padding
		EmitSpacing( depth, "margin", l.Margin );
		EmitSpacing( depth, "padding", l.Padding );
	}

	/// <summary>
	/// V1.5 M4 — element parent dimensions, when knowable from the document
	/// (i.e. when the element is a direct child of the Canvas root). Returns
	/// (parentWidth, parentHeight) or null when unknown (nested centered
	/// elements). Caller uses this to pre-compute pixel-exact center
	/// positions so the engine's Box.Rect == visual rect.
	/// </summary>
	private (float W, float H)? ResolveParentDimensions( SuiElement el )
	{
		if ( el == null || _doc == null ) return null;

		// Direct Canvas child — parent dimensions are the canvas BaseWidth/Height.
		if ( !string.IsNullOrEmpty( el.ParentId )
			&& _byId.TryGetValue( el.ParentId, out var parent )
			&& parent.Type == SuiElementType.Canvas )
		{
			var c = _doc.Canvas;
			if ( c != null && c.BaseWidth > 0 && c.BaseHeight > 0 )
				return (c.BaseWidth, c.BaseHeight);
		}

		// Nested under another fixed-size element (rare) — fall back to that
		// element's Width/Height when set.
		if ( !string.IsNullOrEmpty( el.ParentId )
			&& _byId.TryGetValue( el.ParentId, out var parent2 )
			&& parent2?.Layout != null
			&& parent2.Layout.Width > 0 && parent2.Layout.Height > 0 )
		{
			return (parent2.Layout.Width, parent2.Layout.Height);
		}

		return null;
	}

	private void EmitAnchorRules( int depth, SuiLayoutData l )
	{
		EmitAnchorRules( depth, l, null );
	}

	private void EmitAnchorRules( int depth, SuiLayoutData l, SuiElement el )
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

			// V1.5 M4 — Center anchors: compute exact pixel position from
			// known parent dimensions when possible. Engine Box.Rect then
			// equals visual rect (no transform involved), and popups /
			// tooltips / drag-math that read Box.Rect work correctly.
			//
			// Fallback when parent dimensions are unknown (nested centered
			// elements without explicit parent size): `transform: translate`
			// to preserve visual centering. The popup-offset gap returns in
			// this fallback case — documented as an edge-case limitation.
			case SuiAnchor.TopCenter:
				EmitCenterHorizontal( depth, l, el );
				Emit( depth, "top", Px( l.Y ) );
				break;

			case SuiAnchor.BottomCenter:
				EmitCenterHorizontal( depth, l, el );
				Emit( depth, "bottom", Px( l.Y ) );
				break;

			case SuiAnchor.MiddleLeft:
				Emit( depth, "left", Px( l.X ) );
				EmitCenterVertical( depth, l, el );
				break;

			case SuiAnchor.MiddleRight:
				Emit( depth, "right", Px( l.X ) );
				EmitCenterVertical( depth, l, el );
				break;

			case SuiAnchor.MiddleCenter:
				EmitCenterHorizontal( depth, l, el );
				EmitCenterVertical( depth, l, el );
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

	/// <summary>
	/// V1.5 M4 — emit horizontal centering. Pixel math when parent
	/// dimensions known (engine Box.Rect = visual rect → popups correct);
	/// transform fallback when unknown (popup offset gap returns).
	/// </summary>
	private void EmitCenterHorizontal( int depth, SuiLayoutData l, SuiElement el )
	{
		var parent = ResolveParentDimensions( el );
		if ( parent.HasValue && l.Width > 0 )
		{
			var leftPx = (parent.Value.W - l.Width) * 0.5f + l.X;
			Emit( depth, "left", Px( leftPx ) );
		}
		else
		{
			Emit( depth, "left", "50%" );
			Emit( depth, "transform", $"translateX(-50%) translateX({Px( l.X )})" );
		}
	}

	private void EmitCenterVertical( int depth, SuiLayoutData l, SuiElement el )
	{
		var parent = ResolveParentDimensions( el );
		if ( parent.HasValue && l.Height > 0 )
		{
			var topPx = (parent.Value.H - l.Height) * 0.5f + l.Y;
			Emit( depth, "top", Px( topPx ) );
		}
		else
		{
			// Compose with any existing transform from EmitCenterHorizontal.
			// If both fallbacks fire (MiddleCenter without parent dims),
			// we'd write 2 `transform` rules — the later one wins. So we
			// merge here when both axes are unknown.
			Emit( depth, "top", "50%" );
			Emit( depth, "transform", $"translate(-50%, -50%) translate({Px( l.X )}, {Px( l.Y )})" );
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

		// V1.5 M3.5 — Normal-state background image (PRD 25). Pseudo-class
		// blocks for Hover/Pressed/etc handle their own background-image; this
		// covers the Normal layer. background-size driven by Style.BackgroundSize
		// (Cover/Contain/Stretch/Custom). Custom emits explicit px W/H.
		if ( !string.IsNullOrEmpty( s.BackgroundImage ) )
		{
			Emit( depth, "background-image", $"url(\"{s.BackgroundImage}\")" );
			Emit( depth, "background-repeat", "no-repeat" );
			Emit( depth, "background-size", BackgroundSizeCss( s ) );
			Emit( depth, "background-position", "center" );
		}

		// Border: emit BOTH width and color, or NEITHER. Emitting width alone
		// makes s&box's CSS parser fall back to a white border (canvas paint
		// skips the stroke entirely when color is empty), causing a Preview vs
		// Canvas divergence the user can't fix from the inspector.
		var hasBorderColor = !string.IsNullOrEmpty( s.BorderColor );
		var hasBorderWidth = s.BorderWidth > 0f;
		if ( hasBorderColor && hasBorderWidth )
		{
			Emit( depth, "border-color", s.BorderColor );
			Emit( depth, "border-width", Px( s.BorderWidth ) );
		}

		// V1.5 M3.5 — border-radius driven by ButtonShape preset when this is
		// an interactive element. Rectangle/Custom fall back to Style.BorderRadius;
		// Square/Round/Pill force their derived radius. For non-interactive
		// types the cascade is unchanged.
		var radius = ResolveBorderRadius( el );
		if ( !string.IsNullOrEmpty( radius ) )
			Emit( depth, "border-radius", radius );

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

				// TextSizeMode-specific rules — controls wrap/sizing/vertical alignment.
				switch ( p.TextSizeMode )
				{
					case SuiTextSizeMode.Auto:
						// Single-line, content-sized. Override any width/height the
						// shared layout pass might have emitted by re-emitting auto.
						Emit( depth, "white-space", "nowrap" );
						Emit( depth, "width", "auto" );
						Emit( depth, "height", "auto" );
						break;
					case SuiTextSizeMode.AutoHeightWrap:
						// Width fixed, height grows with wrap. Don't emit explicit height.
						Emit( depth, "white-space", "normal" );
						Emit( depth, "height", "auto" );
						break;
					case SuiTextSizeMode.Fixed:
						// User width/height are emitted normally by the layout pass.
						// Vertical align via flex on the Text element.
						Emit( depth, "display", "flex" );
						Emit( depth, "flex-direction", "column" );
						Emit( depth, "justify-content", VerticalAlignToJustify( p.VerticalAlign ) );
						break;
				}
				break;

			case SuiElementType.Image:
			case SuiElementType.ItemIcon:
			case SuiElementType.InventorySlot:
			{
				// Source path: Image uses ImagePath; ItemIcon and InventorySlot
				// store their icon in PreviewIconPath (the same field that the
				// canvas's PaintItemIcon reads). Without this fallback, the
				// runtime SCSS emits no background-image for ItemIcon and the
				// slot/icon shows empty in Play even though the canvas paints
				// it correctly — pure SCSS/canvas divergence.
				var imagePath = !string.IsNullOrEmpty( p.ImagePath ) ? p.ImagePath : p.PreviewIconPath;
				if ( !string.IsNullOrEmpty( imagePath ) )
				{
					Emit( depth, "background-image", $"url(\"{imagePath}\")" );
					Emit( depth, "background-size", FitMode( p.FitMode ) );
					Emit( depth, "background-position", BgPosition( p.BackgroundPosition ) );
					// CSS default for background-repeat is "repeat", which makes
					// Contain/Cover/None tile when the fitted image is smaller
					// than the container. Force no-repeat so what the canvas
					// paints matches what the runtime shows.
					Emit( depth, "background-repeat", "no-repeat" );
					if ( !string.IsNullOrEmpty( p.Tint ) && p.Tint != "#ffffff" && p.Tint != "#FFFFFF" )
						Emit( depth, "background-image-tint", p.Tint );
				}
				break;
			}

			case SuiElementType.Button:
				// Button = <div class="btn"><label class="label">text</label></div>.
				// Center the label inside the div via flex and emit text styles on
				// both the outer (for hover/state hooks) and the inner .label
				// (so the text actually inherits the user-configured font + color
				// without relying on the runtime's CSS-inheritance behavior, which
				// has been spotty for font-size on nested labels).
				Emit( depth, "display", "flex" );
				Emit( depth, "flex-direction", "row" );
				Emit( depth, "justify-content", "center" );
				Emit( depth, "align-items", "center" );

				// Emit per-button text styles on the inner .label using a nested
				// rule. We close it before continuing other emissions.
				var indentLabel = new string( ' ', depth * 2 );
				_sb.Append( indentLabel ).AppendLine( ".label {" );
				if ( p.FontSize > 0f ) Emit( depth + 1, "font-size", Px( p.FontSize ) );
				if ( !string.IsNullOrEmpty( p.FontFamily ) ) Emit( depth + 1, "font-family", p.FontFamily );
				if ( p.FontWeight != SuiFontWeight.Normal ) Emit( depth + 1, "font-weight", FontWeight( p.FontWeight ) );
				if ( !string.IsNullOrEmpty( p.Color ) ) Emit( depth + 1, "color", p.Color );
				if ( p.TextAlign != SuiTextAlign.Left ) Emit( depth + 1, "text-align", TextAlign( p.TextAlign ) );
				_sb.Append( indentLabel ).AppendLine( "}" );
				break;

			case SuiElementType.Grid:
			case SuiElementType.InventoryGrid:
			case SuiElementType.Hotbar:
				// Wrapped-flex strategy (PRD doc 08 strategy A).
				if ( p.Columns <= 0 || p.Rows <= 0 || p.CellWidth <= 0f || p.CellHeight <= 0f )
					break;

				// If Layout.Mode == Flex, EmitLayout already wrote display/flex/gap
				// from the user's settings. Re-emitting here causes duplicate
				// declarations (later wins): cell-derived `gap: 4px` overrode the
				// user's `gap: 8px`, and cell-derived `width: 64px` overrode the
				// user's `width: 800px`, collapsing the Hotbar to one cell wide.
				var gridLayout = el.Layout;
				var alreadyFlex = gridLayout != null && gridLayout.Mode == SuiLayoutMode.Flex;
				if ( !alreadyFlex )
				{
					Emit( depth, "display", "flex" );
					Emit( depth, "flex-direction", "row" );
					Emit( depth, "flex-wrap", el.Type == SuiElementType.Hotbar ? "nowrap" : "wrap" );
					Emit( depth, "gap", Px( p.GridGap ) );
				}

				// Only auto-size from cells when the user hasn't pinned a size.
				// Pinned size (Layout.Width/Height > 0) is the user's intent;
				// derived size is just a sensible default for new grids.
				// IMPORTANT: when the grid has a border (border-width > 0) AND
				// the runtime uses border-box sizing (s&box's default), the
				// border eats into the content area. A grid sized exactly
				// `Cols × CellW + (Cols-1) × Gap` will wrap one fewer column
				// than intended because the last item overflows by `borderW × 2`.
				// Compensate by adding the border allowance up front.
				// Padding emission is handled separately via EmitSpacing so a
				// user-configured padding does NOT need to be added here.
				var hasExplicitW = gridLayout != null && gridLayout.Width > 0f;
				var hasExplicitH = gridLayout != null && gridLayout.Height > 0f;
				var borderSlack = 2f * ((el.Style?.BorderWidth ?? 0f) > 0f ? el.Style.BorderWidth : 0f);
				var paddingSlackX = (el.Layout?.Padding?.Left ?? 0f) + (el.Layout?.Padding?.Right ?? 0f);
				var paddingSlackY = (el.Layout?.Padding?.Top ?? 0f) + (el.Layout?.Padding?.Bottom ?? 0f);
				if ( !hasExplicitW )
				{
					var w = p.Columns * p.CellWidth + (p.Columns - 1) * p.GridGap + borderSlack + paddingSlackX;
					Emit( depth, "width", Px( w ) );
				}
				if ( !hasExplicitH )
				{
					var h = p.Rows * p.CellHeight + (p.Rows - 1) * p.GridGap + borderSlack + paddingSlackY;
					Emit( depth, "height", Px( h ) );
				}
				break;

			case SuiElementType.ProgressBar:
				// Bare container background; M9 doesn't generate the inner fill div
				// — V1 will. Just ensures the bar shape exists.
				break;

			// V1.5 M4 (PRD 21) — Input widget engine-template overrides.
			// Each emits nested selectors that reach into the inner panels the
			// engine builds (track/thumb/checkmark/etc.) so user-authored
			// colors actually apply in runtime, matching the canvas paint 1:1.

			case SuiElementType.TextEntry:
				// `.textentry` is the implicit engine root tag class; our
				// per-element class (sui-el-X) wraps it via the same selector.
				// Inner `.content-label` holds the typed text + placeholder.
				Emit( depth, "flex-direction", "row" );
				Emit( depth, "align-items", "center" );
				var teInner = new string( ' ', depth * 2 );
				_sb.Append( teInner ).AppendLine( "> .content-label {" );
				Emit( depth + 1, "flex-grow", "1" );
				Emit( depth + 1, "padding", "0 8px" );
				if ( p.FontSize > 0f ) Emit( depth + 1, "font-size", Px( p.FontSize ) );
				if ( !string.IsNullOrEmpty( p.FontFamily ) ) Emit( depth + 1, "font-family", p.FontFamily );
				if ( p.FontWeight != SuiFontWeight.Normal ) Emit( depth + 1, "font-weight", FontWeight( p.FontWeight ) );
				if ( !string.IsNullOrEmpty( p.Color ) ) Emit( depth + 1, "color", p.Color );
				_sb.Append( teInner ).AppendLine( "}" );
				// Placeholder = 60% opacity of the text color when label has the
				// engine-added `.placeholder` class.
				_sb.Append( teInner ).AppendLine( "> .content-label.placeholder {" );
				Emit( depth + 1, "opacity", "0.5" );
				_sb.Append( teInner ).AppendLine( "}" );
				break;

			case SuiElementType.Slider:
				// V1.5 M4 — Custom slider 100% owned. NO engine SliderControl.
				// Markup tree (see SuiRazorGenerator.EmitSliderWithTooltip):
				//   <div class="sui-el-X sui-slider">
				//     <div class="sui-slider-track">
				//       <div class="sui-slider-fill" style="width: N%" />
				//       <div class="sui-slider-thumb" style="left: N%" />
				//       <div class="sui-slider-tooltip" style="left: N%">
				//         <label>Value</label>
				//         <div class="sui-slider-tooltip-tail" />
				//       </div>
				//     </div>
				//   </div>
				// Every visual is on a class we own — no engine class chain
				// competes for specificity. Author colors apply 100%.
				Emit( depth, "position", "relative" );
				Emit( depth, "flex-direction", "column" );
				Emit( depth, "justify-content", "center" );
				var slInner = new string( ' ', depth * 2 );

				// Track — base bar (Sandbox.UI requires a height somewhere or
				// the panel collapses to 0).
				_sb.Append( slInner ).AppendLine( "> .sui-slider-track {" );
				Emit( depth + 1, "position", "relative" );
				Emit( depth + 1, "width", "100%" );
				Emit( depth + 1, "height", "8px" );
				Emit( depth + 1, "border-radius", "4px" );
				if ( !string.IsNullOrEmpty( p.SliderTrackColor ) )
					Emit( depth + 1, "background-color", p.SliderTrackColor );
				Emit( depth + 1, "cursor", "pointer" );
				Emit( depth + 1, "pointer-events", "all" );
				_sb.Append( slInner ).AppendLine( "}" );

				// Fill — width animated via inline style.
				_sb.Append( slInner ).AppendLine( "> .sui-slider-track > .sui-slider-fill {" );
				Emit( depth + 1, "position", "absolute" );
				Emit( depth + 1, "left", "0" );
				Emit( depth + 1, "top", "0" );
				Emit( depth + 1, "height", "100%" );
				Emit( depth + 1, "border-radius", "4px" );
				if ( !string.IsNullOrEmpty( p.SliderFillColor ) )
					Emit( depth + 1, "background-color", p.SliderFillColor );
				Emit( depth + 1, "pointer-events", "none" );
				_sb.Append( slInner ).AppendLine( "}" );

				// Thumb — 16x16 circle that follows the value.
				_sb.Append( slInner ).AppendLine( "> .sui-slider-track > .sui-slider-thumb {" );
				Emit( depth + 1, "position", "absolute" );
				Emit( depth + 1, "top", "50%" );
				Emit( depth + 1, "width", "16px" );
				Emit( depth + 1, "height", "16px" );
				Emit( depth + 1, "border-radius", "50%" );
				Emit( depth + 1, "transform", "translate(-50%, -50%)" );
				if ( !string.IsNullOrEmpty( p.SliderHandleColor ) )
					Emit( depth + 1, "background-color", p.SliderHandleColor );
				Emit( depth + 1, "pointer-events", "none" );
				_sb.Append( slInner ).AppendLine( "}" );

				// Tooltip pill — absolute above the thumb, fully ours.
				_sb.Append( slInner ).AppendLine( "> .sui-slider-track > .sui-slider-tooltip {" );
				Emit( depth + 1, "position", "absolute" );
				Emit( depth + 1, "bottom", "150%" );
				Emit( depth + 1, "transform", "translateX(-50%)" );
				Emit( depth + 1, "flex-direction", "column" );
				Emit( depth + 1, "align-items", "center" );
				Emit( depth + 1, "z-index", "10" );
				Emit( depth + 1, "pointer-events", "none" );
				_sb.Append( slInner ).AppendLine( "}" );
				_sb.Append( slInner ).AppendLine( "> .sui-slider-track > .sui-slider-tooltip > label {" );
				if ( !string.IsNullOrEmpty( p.SliderTooltipBgColor ) )
					Emit( depth + 1, "background-color", p.SliderTooltipBgColor );
				if ( !string.IsNullOrEmpty( p.SliderTooltipTextColor ) )
					Emit( depth + 1, "color", p.SliderTooltipTextColor );
				Emit( depth + 1, "padding", "4px 8px" );
				Emit( depth + 1, "border-radius", "6px" );
				Emit( depth + 1, "font-size", "12px" );
				_sb.Append( slInner ).AppendLine( "}" );
				_sb.Append( slInner ).AppendLine( "> .sui-slider-track > .sui-slider-tooltip > .sui-slider-tooltip-tail {" );
				Emit( depth + 1, "width", "8px" );
				Emit( depth + 1, "height", "8px" );
				if ( !string.IsNullOrEmpty( p.SliderTooltipBgColor ) )
					Emit( depth + 1, "background-color", p.SliderTooltipBgColor );
				Emit( depth + 1, "transform", "rotateZ(45deg) translateY(-4px)" );
				_sb.Append( slInner ).AppendLine( "}" );
				break;

			case SuiElementType.Toggle:
				// Checkbox engine template:
				//   <checkbox class="checkbox" [.checked]>
				//     <icon class="checkmark">check</icon>
				//     <label>LabelText</label>
				// We size the checkmark + style it visible only when .checked,
				// and align the label to the right via row flow.
				Emit( depth, "flex-direction", "row" );
				Emit( depth, "align-items", "center" );
				Emit( depth, "gap", "8px" );
				var cbInner = new string( ' ', depth * 2 );
				// Checkmark box: 20x20, hides icon when not checked.
				_sb.Append( cbInner ).AppendLine( "> .checkmark {" );
				Emit( depth + 1, "width", "20px" );
				Emit( depth + 1, "height", "20px" );
				Emit( depth + 1, "flex-shrink", "0" );
				Emit( depth + 1, "border", "1px solid rgba(255,255,255,0.3)" );
				Emit( depth + 1, "border-radius", "3px" );
				Emit( depth + 1, "background-color", "rgba(0,0,0,0.4)" );
				Emit( depth + 1, "color", "transparent" );
				Emit( depth + 1, "align-items", "center" );
				Emit( depth + 1, "justify-content", "center" );
				Emit( depth + 1, "font-size", "16px" );
				_sb.Append( cbInner ).AppendLine( "}" );
				_sb.Append( cbInner ).AppendLine( "&.checked > .checkmark {" );
				Emit( depth + 1, "background-color", "#22c55e" );
				Emit( depth + 1, "border-color", "#22c55e" );
				Emit( depth + 1, "color", "#ffffff" );
				_sb.Append( cbInner ).AppendLine( "}" );
				_sb.Append( cbInner ).AppendLine( "> label {" );
				if ( p.FontSize > 0f ) Emit( depth + 1, "font-size", Px( p.FontSize ) );
				if ( !string.IsNullOrEmpty( p.Color ) ) Emit( depth + 1, "color", p.Color );
				_sb.Append( cbInner ).AppendLine( "}" );
				break;

			case SuiElementType.DropDown:
				// DropDown extends PopupButton extends Button:
				//   <select class="dropdown button">
				//     ...button internals...
				//     <icon class="dropdown_indicator">expand_more</icon>
				// Engine `.dropdown` SCSS hard-codes `flex-grow: 1` and
				// `.button-right-column { flex-grow: 1 }` — without overriding
				// both, the authored Width is ignored and the control fills
				// the flex parent (seen in the M4 smoke test as a stretched
				// pill across the whole card width).
				Emit( depth, "flex-direction", "row" );
				Emit( depth, "align-items", "center" );
				Emit( depth, "justify-content", "space-between" );
				Emit( depth, "padding", "0 8px" );
				Emit( depth, "flex-grow", "0" );
				var ddInner = new string( ' ', depth * 2 );
				// Inner column also flex-grows by engine default; cap it so
				// the indicator stays right-aligned without forcing extra
				// width onto the dropdown.
				_sb.Append( ddInner ).AppendLine( "> .button-right-column {" );
				Emit( depth + 1, "flex-grow", "0" );
				_sb.Append( ddInner ).AppendLine( "}" );
				_sb.Append( ddInner ).AppendLine( "> .button-right-column .button-text {" );
				if ( p.FontSize > 0f ) Emit( depth + 1, "font-size", Px( p.FontSize ) );
				if ( !string.IsNullOrEmpty( p.Color ) ) Emit( depth + 1, "color", p.Color );
				_sb.Append( ddInner ).AppendLine( "}" );
				_sb.Append( ddInner ).AppendLine( "> .dropdown_indicator {" );
				Emit( depth + 1, "font-size", "16px" );
				if ( !string.IsNullOrEmpty( p.Color ) ) Emit( depth + 1, "color", p.Color );
				_sb.Append( ddInner ).AppendLine( "}" );
				break;
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  V1.5 M3.5 — Interactive states (PRD 25 § 6)
	//  Emit transition + cursor on the root selector, then nested pseudo-
	//  class / class rules for the four override states. Empty state styles
	//  emit nothing (the element inherits Normal cascade).
	// ─────────────────────────────────────────────────────────────────────

	private static bool IsInteractiveType( SuiElementType type ) =>
		type == SuiElementType.Button
		|| type == SuiElementType.InventorySlot
		|| type == SuiElementType.ItemIcon;

	/// <summary>
	/// V1.5 M3.5 — map <see cref="SuiCursor"/> to the matching CSS keyword.
	/// <see cref="SuiCursor.Default"/> returns empty so no rule is emitted
	/// (the element inherits whatever the cascade hands it).
	/// </summary>
	private static string CursorCss( SuiCursor c ) => c switch
	{
		SuiCursor.Pointer => "pointer",
		SuiCursor.NotAllowed => "not-allowed",
		SuiCursor.Wait => "wait",
		SuiCursor.Text => "text",
		SuiCursor.Move => "move",
		SuiCursor.Crosshair => "crosshair",
		SuiCursor.Help => "help",
		SuiCursor.None => "none",
		_ => "",
	};

	/// <summary>
	/// V1.5 M3.5 — translate <see cref="SuiBackgroundSize"/> + Custom dimensions
	/// into the CSS <c>background-size</c> value. Custom with both dimensions = 0
	/// degrades to Contain so the user doesn't get an invisible image.
	/// </summary>
	private static string BackgroundSizeCss( SuiStyleData s )
	{
		switch ( s.BackgroundSize )
		{
			case SuiBackgroundSize.Cover: return "cover";
			case SuiBackgroundSize.Stretch: return "100% 100%";
			case SuiBackgroundSize.Custom:
				if ( s.BackgroundWidth <= 0f && s.BackgroundHeight <= 0f )
					return "contain";
				return $"{Px( s.BackgroundWidth )} {Px( s.BackgroundHeight )}";
			case SuiBackgroundSize.Contain:
			default: return "contain";
		}
	}

	/// <summary>
	/// Resolve the effective border-radius CSS value for an element. Interactive
	/// types route through ButtonShape: Square→0, Round→50%, Pill→9999px,
	/// Rectangle/Custom fall back to Style.BorderRadius. Non-interactive types
	/// also use Style.BorderRadius. Returns null/empty when no rule should emit.
	/// </summary>
	private static string ResolveBorderRadius( SuiElement el )
	{
		var s = el.Style;
		if ( s == null ) return null;

		if ( IsInteractiveType( el.Type ) && el.Props != null )
		{
			switch ( el.Props.ButtonShape )
			{
				case SuiButtonShape.Square: return "0";
				case SuiButtonShape.Round: return "50%";
				case SuiButtonShape.Pill: return "9999px";
				// Rectangle + Custom fall through to Style.BorderRadius.
			}
		}

		return s.BorderRadius > 0f ? Px( s.BorderRadius ) : null;
	}

	private void EmitInteractiveStates( SuiElement el, int depth )
	{
		if ( !IsInteractiveType( el.Type ) ) return;
		var p = el.Props;
		if ( p == null ) return;

		// Cursor (CSS property — engine supports a limited set per anti-patterns
		// doc, "pointer" / "not-allowed" / "default" confirmed). Default enum
		// value emits nothing so the element inherits whatever the cascade
		// hands it.
		var cursorCss = CursorCss( p.Cursor );
		if ( !string.IsNullOrEmpty( cursorCss ) )
			Emit( depth, "cursor", cursorCss );

		// Transition — once enabled, every property animates over Duration.
		// "all" + "ease" matches the skill reference pattern; emitting only
		// when at least one override exists would be slightly cheaper but
		// risks a stuttery feel if the user later adds Hover and doesn't
		// know transitions need re-enabling. Default ON is safer UX.
		if ( p.TransitionEnabled && p.TransitionDuration > 0f )
		{
			var dur = p.TransitionDuration.ToString( "0.###", System.Globalization.CultureInfo.InvariantCulture );
			Emit( depth, "transition", $"all {dur}s ease" );
		}

		// Pseudo-class blocks. Order matters when specificity ties (e.g.
		// :hover vs :active both single-pseudo) — the later rule wins.
		// We also restrict :hover to ":not(:active)" so the press state
		// fully overrides hover during a click instead of cascading on top
		// of it (PRD 25 § 6.2 — author expectation Pressed > Hover > Normal).
		EmitStateBlock( depth, "&:hover:not(:active)", p.HoverStyle, p.HoverSound );
		EmitStateBlock( depth, "&:focus", p.FocusedStyle, null );
		EmitStateBlock( depth, "&:active", p.PressedStyle, p.PressSound );
		// `.disabled` always emits at least `pointer-events: none` so the
		// class actually suppresses input — the user's DisabledStyle override
		// stacks on top via EmitStateBlock when authored.
		EmitDisabledBlock( depth, p.DisabledStyle );
	}

	/// <summary>
	/// V1.5 M3.5 (PRD 25) — `.disabled` always emits `pointer-events: none`
	/// (the whole point of disabling), even when the user didn't author any
	/// visual override. Authored fields stack on top via the same body that
	/// <see cref="EmitStateBlock"/> writes.
	/// </summary>
	private void EmitDisabledBlock( int depth, SuiInteractiveStateStyle style )
	{
		var indent = new string( ' ', depth * 2 );
		_sb.Append( indent ).AppendLine( "&.disabled {" );
		Emit( depth + 1, "pointer-events", "none" );

		if ( style != null && !style.IsEmpty() )
		{
			if ( !string.IsNullOrEmpty( style.BackgroundImage ) )
				Emit( depth + 1, "background-image", $"url(\"{style.BackgroundImage}\")" );
			if ( !string.IsNullOrEmpty( style.BackgroundColor ) )
				Emit( depth + 1, "background-color", style.BackgroundColor );
			if ( !string.IsNullOrEmpty( style.BorderColor ) )
				Emit( depth + 1, "border-color", style.BorderColor );
			if ( style.BorderWidth >= 0f )
				Emit( depth + 1, "border-width", Px( style.BorderWidth ) );
			if ( style.BorderRadius >= 0f )
				Emit( depth + 1, "border-radius", Px( style.BorderRadius ) );
			if ( !string.IsNullOrEmpty( style.TextColor ) )
				Emit( depth + 1, "color", style.TextColor );
			if ( style.Scale != 1f && style.Scale > 0f )
			{
				var sc = style.Scale.ToString( "0.###", System.Globalization.CultureInfo.InvariantCulture );
				Emit( depth + 1, "transform", $"scale({sc})" );
			}
			if ( style.Opacity >= 0f )
			{
				var o = style.Opacity.ToString( "0.###", System.Globalization.CultureInfo.InvariantCulture );
				Emit( depth + 1, "opacity", o );
			}
		}

		_sb.Append( indent ).AppendLine( "}" );
	}

	/// <summary>
	/// Emit a <c>&amp;:hover { ... }</c>-style nested rule for one override
	/// state. Returns silently when both <paramref name="style"/> is null/empty
	/// and <paramref name="sound"/> is empty — nothing to write.
	/// </summary>
	private void EmitStateBlock( int depth, string selector, SuiInteractiveStateStyle style, string sound )
	{
		var hasStyle = style != null && !style.IsEmpty();
		var hasSound = !string.IsNullOrEmpty( sound );
		if ( !hasStyle && !hasSound ) return;

		var indent = new string( ' ', depth * 2 );
		_sb.Append( indent ).Append( selector ).AppendLine( " {" );

		if ( hasStyle )
		{
			if ( !string.IsNullOrEmpty( style.BackgroundImage ) )
				Emit( depth + 1, "background-image", $"url(\"{style.BackgroundImage}\")" );
			if ( !string.IsNullOrEmpty( style.BackgroundColor ) )
				Emit( depth + 1, "background-color", style.BackgroundColor );
			if ( !string.IsNullOrEmpty( style.BorderColor ) )
				Emit( depth + 1, "border-color", style.BorderColor );
			if ( style.BorderWidth >= 0f )
				Emit( depth + 1, "border-width", Px( style.BorderWidth ) );
			if ( style.BorderRadius >= 0f )
				Emit( depth + 1, "border-radius", Px( style.BorderRadius ) );
			if ( !string.IsNullOrEmpty( style.TextColor ) )
				Emit( depth + 1, "color", style.TextColor );
			if ( style.Scale != 1f && style.Scale > 0f )
			{
				var s = style.Scale.ToString( "0.###", System.Globalization.CultureInfo.InvariantCulture );
				Emit( depth + 1, "transform", $"scale({s})" );
			}
			if ( style.Opacity >= 0f )
			{
				var o = style.Opacity.ToString( "0.###", System.Globalization.CultureInfo.InvariantCulture );
				Emit( depth + 1, "opacity", o );
			}
		}

		// Sound on state ingress. SCSS-level engine feature — fires once per
		// state entry, no C# round-trip needed (confirmed in ui-razor.md and
		// engine UI/Input/PanelInput.cs).
		if ( hasSound )
			Emit( depth + 1, "sound-in", $"\"{sound}\"" );

		_sb.Append( indent ).AppendLine( "}" );
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
		var indent = new string( ' ', depth * 2 );
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

	private static string VerticalAlignToJustify( SuiVerticalAlign v ) => v switch
	{
		SuiVerticalAlign.Center => "center",
		SuiVerticalAlign.Bottom => "flex-end",
		_ => "flex-start",
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
