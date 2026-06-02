using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.EditorUi;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Canvas;

/// <summary>
/// Paints a <see cref="SuiDocument"/> tree directly via the editor Paint API.
/// Operates in <b>logical pixel space</b> (1920x1080 default) — the caller is
/// expected to have already applied a Paint.Translate + Paint.Scale to transform
/// logical→widget pixels.
///
/// Reads element rects from a populated <see cref="SuiLayoutSolver"/>. Per
/// element type, emits the equivalent of what the Razor + SCSS would render.
/// Keeping these rules in sync with <c>SuiScssGenerator</c> is what makes the
/// canvas a 1:1 preview of the runtime output.
///
/// Render rules per element type — Phase 1 implementation:
///
/// | Type            | Visual                                                         |
/// |-----------------|----------------------------------------------------------------|
/// | Canvas (root)   | Faint outline only (document boundary)                         |
/// | Panel           | Background-color fill + border (with optional radius)          |
/// | Text            | Text drawn with FontFamily/Size/Weight/Color/Align/Overflow    |
/// | Image           | Background fill + image via Paint.SetBrush(Pixmap) + tint      |
/// | Button          | Composed: Panel (bg + border) + centered Text label            |
/// | ProgressBar     | Outer Panel + inner filled bar at PreviewValue/(Max-Min)       |
/// | HorizontalBox   | No own visual (layout container)                               |
/// | VerticalBox     | No own visual (layout container)                               |
/// | Grid            | No own visual (layout container) — children handled in Phase 3 |
/// | Overlay         | No own visual (z-stacking container)                           |
/// | ScrollPanel     | Panel + clip-rect (V2 — for now just renders as Panel)         |
/// | InventorySlot   | Panel with subdued bg                                          |
/// | InventoryGrid   | No own visual                                                  |
/// | ItemIcon        | Image rendering of <c>PreviewIconPath</c>                      |
/// | Tooltip         | Hidden in canvas (runtime-only)                                |
/// | Hotbar          | No own visual                                                  |
/// </summary>
public sealed class SuiCanvasRenderer
{
	private readonly SuiLayoutSolver _solver;
	private readonly string _projectAssetsRoot;

	/// <summary>
	/// Current canvas zoom factor — set by the viewport before each paint pass.
	/// Used by image rendering to decide whether the native pixmap needs a
	/// CPU pre-resize before the GPU draws it (avoids heavy aliasing when the
	/// effective display size is much smaller than native).
	/// </summary>
	public float Zoom { get; set; } = 1.0f;

	/// <summary>
	/// V1.5 — when true, the Canvas-root element's faint outline is suppressed
	/// (children still render). Set on the throwaway child renderer spawned by
	/// <see cref="PaintSuiReference"/> so the child's canvas frame doesn't
	/// bleed past the SuiReference bounds.
	/// </summary>
	public bool SkipRootFrame { get; set; } = false;

	public SuiCanvasRenderer( SuiLayoutSolver solver, string projectAssetsRoot )
	{
		_solver = solver;
		_projectAssetsRoot = projectAssetsRoot;
	}

	/// <summary>
	/// Paint the entire document. Caller must have already pushed the
	/// logical→widget transform onto Paint.
	/// </summary>
	public void Paint( SuiDocument document )
	{
		if ( document == null ) return;

		var root = document.GetRoot();
		if ( root == null ) return;

		// Pre-pass: ensure antialiasing for smooth borders + text.
		Editor.Paint.Antialiasing = true;
		Editor.Paint.TextAntialiasing = true;

		PaintElement( root );
	}

	private void PaintElement( SuiElement el )
	{
		if ( el == null ) return;
		if ( el.Flags?.HiddenInDesigner == true ) return;
		if ( el.Style?.Visibility == SuiVisibility.Collapsed ) return;
		if ( !_solver.TryGetRect( el.Id, out var rect ) ) return;

		var opacity = ResolveOpacity( el );
		if ( opacity <= 0f ) { PaintChildren( el ); return; }

		switch ( el.Type )
		{
			case SuiElementType.Canvas:
				if ( !SkipRootFrame ) PaintCanvasRoot( el, rect );
				break;
			case SuiElementType.Panel:
			case SuiElementType.Overlay:
			case SuiElementType.HorizontalBox:
			case SuiElementType.VerticalBox:
			case SuiElementType.Grid:
			case SuiElementType.ScrollPanel:
			case SuiElementType.InventoryGrid:
			case SuiElementType.Hotbar:
				PaintPanelLike( el, rect, opacity );
				break;
			case SuiElementType.InventorySlot:
				// Slot frame + optional preview icon + count overlay (so designer
				// sees what an occupied slot will look like at runtime).
				PaintPanelLike( el, rect, opacity );
				PaintItemIcon( el, rect, opacity );
				break;
			case SuiElementType.Text:
				PaintPanelLike( el, rect, opacity );  // bg if any
				PaintText( el, rect, opacity );
				break;
			case SuiElementType.Image:
				PaintPanelLike( el, rect, opacity );  // bg if any
				PaintImage( el, rect, opacity );
				break;
			case SuiElementType.Button:
				PaintPanelLike( el, rect, opacity );
				PaintButtonLabel( el, rect, opacity );
				break;
			case SuiElementType.ProgressBar:
				PaintPanelLike( el, rect, opacity );
				PaintProgressFill( el, rect, opacity );
				break;
			case SuiElementType.ItemIcon:
				PaintPanelLike( el, rect, opacity );
				PaintItemIcon( el, rect, opacity );
				break;
			case SuiElementType.Tooltip:
				// Runtime-only. Skip rendering.
				break;
			case SuiElementType.SuiReference:
				PaintSuiReference( el, rect, opacity );
				break;

			// V1.5 M4 — Input widgets (PRD 21 § 3). All four call PaintPanelLike
			// FIRST so the user-authored Style (background color / border /
			// background image / radius) actually appears on the canvas in the
			// same place where the engine paints it at runtime — otherwise the
			// canvas hides authored visuals that show up as "surprises" in Play.
			case SuiElementType.TextEntry:
				PaintPanelLike( el, rect, opacity );
				PaintTextEntry( el, rect, opacity );
				break;
			case SuiElementType.Slider:
				PaintPanelLike( el, rect, opacity );
				PaintSlider( el, rect, opacity );
				break;
			case SuiElementType.Toggle:
				PaintPanelLike( el, rect, opacity );
				PaintToggle( el, rect, opacity );
				break;
			case SuiElementType.DropDown:
				PaintPanelLike( el, rect, opacity );
				PaintDropDown( el, rect, opacity );
				break;
		}

		PaintChildren( el );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  SuiReference — recursive paint of the embedded child's subtree
	//  (M2-K0 / D-010 reversal — was originally a placeholder rectangle).
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>Cache of resolved child documents keyed by SourceGuid. Lifetime = this renderer instance.</summary>
	private readonly Dictionary<string, SuiDocument> _childDocCache = new();

	private void PaintSuiReference( SuiElement el, Rect rect, float opacity )
	{
		// Dashed-border affordance so the user knows this rect is a sub-UI, not
		// a regular container they can fill with children directly.
		DrawSuiReferenceBorder( rect );

		var data = el.SuiReference;
		if ( data == null || string.IsNullOrEmpty( data.SourceGuid ) )
		{
			DrawSuiReferencePlaceholder( rect, "no source set" );
			return;
		}

		var childDoc = ResolveChildDoc( data.SourceGuid );
		if ( childDoc == null )
		{
			DrawSuiReferencePlaceholder( rect, "source not found" );
			return;
		}

		var childRoot = childDoc.GetRoot();
		if ( childRoot == null )
		{
			DrawSuiReferencePlaceholder( rect, "(child empty)" );
			return;
		}

		var childCanvasW = childDoc.Canvas?.BaseWidth ?? 1920;
		var childCanvasH = childDoc.Canvas?.BaseHeight ?? 1080;
		if ( childCanvasW <= 0 || childCanvasH <= 0 )
		{
			DrawSuiReferencePlaceholder( rect, "(invalid canvas)" );
			return;
		}

		// Layout the child in its OWN canvas space first so the solver produces
		// proper rects for every element.
		var childSolver = new SuiLayoutSolver( new Vector2( childCanvasW, childCanvasH ) );
		childSolver.Solve( childDoc );

		// Bounding-box scale: compute the actual content extent (excludes the
		// Canvas root frame, which is always full-canvas), then scale that to
		// fit the SuiReference bounds. Result — content fills the rectangle
		// regardless of how much of the child's logical canvas it occupies.
		// UMG-style behaviour: drop a 250x80 widget into a 700x200 ref and you
		// see it big, not tiny in the corner.
		var bb = ComputeContentBoundingBox( childDoc, childSolver );
		if ( bb.Width <= 0 || bb.Height <= 0 )
		{
			DrawSuiReferencePlaceholder( rect, "(empty content)" );
			return;
		}

		var scaleX = rect.Width / bb.Width;
		var scaleY = rect.Height / bb.Height;

		// Map child-local rect r → parent-logical:
		//   newX = refRect.x + (r.x - bb.x) * scaleX
		//   newY = refRect.y + (r.y - bb.y) * scaleY
		//   newW = r.w * scaleX, newH = r.h * scaleY
		// Subtracting bb.x/bb.y shifts content so its top-left aligns with the
		// SuiReference's top-left before scaling.
		var ids = new List<string>( childSolver.Rects.Keys );
		foreach ( var id in ids )
		{
			var r = childSolver.Rects[id];
			childSolver.Rects[id] = new Rect(
				rect.Left + ( r.Left - bb.Left ) * scaleX,
				rect.Top + ( r.Top - bb.Top ) * scaleY,
				r.Width * scaleX,
				r.Height * scaleY );
		}

		// Recurse with a child renderer using the transformed solver. The child
		// renderer respects opacity by reading each element's own Style, so we
		// don't need to inject the outer opacity (it would multiply twice — the
		// child elements are independently authored).
		//
		// SkipRootFrame suppresses the child Canvas's faint-white outline (which
		// would otherwise extend past the SuiReference bounds because the root's
		// rect after scale+offset lands outside the ref). PaintChildren still
		// fires for the root so the actual content draws normally — the dashed
		// border we already paint around the SuiReference is the right
		// "this is a Sub-UI" affordance.
		var childRenderer = new SuiCanvasRenderer( childSolver, _projectAssetsRoot )
		{
			Zoom = Zoom,
			SkipRootFrame = true,
		};
		childRenderer.Paint( childDoc );
	}

	private SuiDocument ResolveChildDoc( string sourceGuid )
	{
		// Cache only successful resolves so a transient miss (registry not yet
		// initialized when the canvas first paints) doesn't poison the cache
		// for the rest of the session.
		if ( _childDocCache.TryGetValue( sourceGuid, out var cached ) && cached != null )
			return cached;

		try
		{
			var registry = SuiAssetRegistryService.Instance;
			registry.EnsureInitialized();

			var relPath = registry.Registry.Resolve( sourceGuid );
			if ( string.IsNullOrEmpty( relPath ) )
			{
				Log.Warning( $"[SUI] canvas: registry has no entry for '{sourceGuid}' — child render skipped." );
				return null;
			}

			// Use the engine's asset loader — not System.Text.Json. The s&box
			// serializer registers custom converters for engine enums
			// (SuiScaleMode etc) and Component/Resource refs that plain JSON
			// can't deserialize. AssetSystem.FindByPath takes a path RELATIVE
			// TO Assets/ (no "Assets/" prefix) — the registry stores the full
			// project-relative path so we strip the prefix here.
			var assetPath = StripAssetsPrefix( relPath );
			var asset = AssetSystem.FindByPath( assetPath );
			if ( asset == null )
			{
				Log.Warning( $"[SUI] canvas: AssetSystem couldn't find '{assetPath}' (registry path '{relPath}') for GUID '{sourceGuid}'." );
				return null;
			}

			var loaded = asset.LoadResource<SuiAsset>();
			var doc = loaded?.Document;
			if ( doc != null ) _childDocCache[sourceGuid] = doc;
			else Log.Warning( $"[SUI] canvas: LoadResource<SuiAsset> returned null for '{assetPath}'." );
			return doc;
		}
		catch ( Exception e )
		{
			Log.Warning( $"[SUI] canvas resolve of '{sourceGuid}' failed: {e.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Bounding box of the child document's NON-root elements — the visible
	/// content's actual extent in the child's local canvas space. Excludes the
	/// Canvas root (which always occupies the full BaseWidth × BaseHeight and
	/// would defeat the "scale to fit" behaviour). Empty rect if the child has
	/// no real elements.
	/// </summary>
	private static Rect ComputeContentBoundingBox( SuiDocument doc, SuiLayoutSolver solver )
	{
		if ( doc?.Elements == null ) return new Rect( 0, 0, 0, 0 );
		var rootId = doc.GetRoot()?.Id;

		float minL = float.MaxValue, minT = float.MaxValue;
		float maxR = float.MinValue, maxB = float.MinValue;
		bool any = false;

		foreach ( var el in doc.Elements )
		{
			if ( el?.Id == null ) continue;
			if ( el.Id == rootId ) continue; // root frame is the canvas itself, skip
			if ( el.Flags?.HiddenInDesigner == true ) continue;
			if ( !solver.Rects.TryGetValue( el.Id, out var r ) ) continue;
			if ( r.Width <= 0 || r.Height <= 0 ) continue;

			if ( r.Left < minL ) minL = r.Left;
			if ( r.Top < minT ) minT = r.Top;
			if ( r.Right > maxR ) maxR = r.Right;
			if ( r.Bottom > maxB ) maxB = r.Bottom;
			any = true;
		}

		if ( !any ) return new Rect( 0, 0, 0, 0 );
		return new Rect( minL, minT, maxR - minL, maxB - minT );
	}

	/// <summary>
	/// The Asset Registry stores project-relative paths like
	/// <c>"Assets/SuiSamples/foo.sui"</c>, but <see cref="AssetSystem.FindByPath"/>
	/// expects paths RELATIVE TO Assets/ (e.g. <c>"SuiSamples/foo.sui"</c>).
	/// Strip the prefix if present; otherwise return unchanged.
	/// </summary>
	private static string StripAssetsPrefix( string projectRelative )
	{
		if ( string.IsNullOrEmpty( projectRelative ) ) return projectRelative;
		const string prefix = "Assets/";
		if ( projectRelative.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) )
			return projectRelative.Substring( prefix.Length );
		return projectRelative;
	}

	private static void DrawSuiReferenceBorder( Rect rect )
	{
		// Purple dashed border — matches the V1.5 composition accent (same hue
		// the bind icon uses). Painted as 4 sides of short segments so it works
		// without a dashed-line primitive.
		var c = new Color( 167 / 255f, 139 / 255f, 250 / 255f, 0.8f );
		Editor.Paint.SetPen( c, 1.5f );
		const float dashLen = 6f;
		const float gapLen = 4f;
		DrawDashedLine( rect.Left, rect.Top, rect.Right, rect.Top, dashLen, gapLen );
		DrawDashedLine( rect.Right, rect.Top, rect.Right, rect.Bottom, dashLen, gapLen );
		DrawDashedLine( rect.Right, rect.Bottom, rect.Left, rect.Bottom, dashLen, gapLen );
		DrawDashedLine( rect.Left, rect.Bottom, rect.Left, rect.Top, dashLen, gapLen );
	}

	private static void DrawDashedLine( float x1, float y1, float x2, float y2, float dashLen, float gapLen )
	{
		var dx = x2 - x1;
		var dy = y2 - y1;
		var len = MathF.Sqrt( dx * dx + dy * dy );
		if ( len < 0.5f ) return;
		var nx = dx / len;
		var ny = dy / len;
		var pos = 0f;
		while ( pos < len )
		{
			var end = MathF.Min( pos + dashLen, len );
			Editor.Paint.DrawLine(
				new Vector2( x1 + nx * pos, y1 + ny * pos ),
				new Vector2( x1 + nx * end, y1 + ny * end ) );
			pos = end + gapLen;
		}
	}

	private static void DrawSuiReferencePlaceholder( Rect rect, string label )
	{
		Editor.Paint.SetBrushAndPen( new Color( 0.1f, 0.1f, 0.12f, 0.4f ) );
		Editor.Paint.DrawRect( rect, 4 );
		Editor.Paint.SetPen( new Color( 167 / 255f, 139 / 255f, 250 / 255f ) );
		Editor.Paint.SetDefaultFont( 11 );
		Editor.Paint.DrawText( rect, "Sub-UI — " + label, TextFlag.Center );
	}

	private void PaintChildren( SuiElement el )
	{
		// Render in ZIndex order (ascending) so high-Z elements end up on top.
		// Stable on hierarchy order — elements with equal ZIndex keep authoring sequence.
		var ordered = SuiLayoutSolver.GetRenderOrderedChildren( el, _solver.ById );
		foreach ( var child in ordered )
			PaintElement( child );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Element renderers
	// ─────────────────────────────────────────────────────────────────────

	private void PaintCanvasRoot( SuiElement el, Rect rect )
	{
		// Faint outline so the user sees where the document edges are even
		// when nothing else is rendered. Not a real visual element.
		Editor.Paint.SetPen( Color.White.WithAlpha( 0.08f ), 1f );
		Editor.Paint.ClearBrush();
		Editor.Paint.DrawRect( rect );

		// If the canvas has its own background-color (Panel-like behavior), honor it.
		var bg = ParseColor( el.Style?.BackgroundColor );
		if ( bg.HasValue && bg.Value.a > 0 )
		{
			Editor.Paint.SetBrush( bg.Value );
			Editor.Paint.ClearPen();
			Editor.Paint.DrawRect( rect );
		}
	}

	private void PaintPanelLike( SuiElement el, Rect rect, float opacity )
	{
		var s = el.Style;
		if ( s == null ) return;

		var bg = ParseColor( s.BackgroundColor );
		var border = ParseColor( s.BorderColor );
		var hasBg = bg.HasValue && bg.Value.a > 0;
		var hasBorder = border.HasValue && border.Value.a > 0 && s.BorderWidth > 0;
		var hasBgImage = !string.IsNullOrEmpty( s.BackgroundImage );

		if ( !hasBg && !hasBorder && !hasBgImage ) return;

		// V1.5 M3.5 (PRD 25) — Shape preset drives the radius for interactive
		// types. Mirror the SCSS generator's ResolveBorderRadius logic so the
		// canvas paint matches what the runtime engine renders.
		var radius = ResolveCanvasRadius( el, rect );

		if ( hasBg )
		{
			Editor.Paint.SetBrush( bg.Value.WithAlpha( bg.Value.a * opacity ) );
			Editor.Paint.ClearPen();
			DrawRect( rect, radius );
		}

		// V1.5 M3.5 — Normal-state background image. Honor Style.BackgroundSize
		// for visual parity with SCSS (Cover/Contain/Stretch/Custom).
		if ( hasBgImage )
		{
			var abs = ResolveProjectPath( s.BackgroundImage );
			if ( !string.IsNullOrEmpty( abs ) )
			{
				Pixmap pm = null;
				try { pm = Editor.Paint.LoadImage( abs ); }
				catch { }
				if ( pm != null && pm.Width > 1 && pm.Height > 1 )
				{
					var imgSize = new Vector2( pm.Width, pm.Height );
					var fitMode = MapBackgroundSize( s.BackgroundSize );
					Rect fitRect;
					if ( s.BackgroundSize == SuiBackgroundSize.Custom
						&& (s.BackgroundWidth > 0f || s.BackgroundHeight > 0f) )
					{
						// Honor explicit px W/H; center within element rect.
						var w = s.BackgroundWidth > 0f ? s.BackgroundWidth : pm.Width;
						var h = s.BackgroundHeight > 0f ? s.BackgroundHeight : pm.Height;
						var cx = rect.Left + rect.Width * 0.5f - w * 0.5f;
						var cy = rect.Top + rect.Height * 0.5f - h * 0.5f;
						fitRect = new Rect( cx, cy, w, h );
					}
					else
					{
						fitRect = ApplyFitMode( rect, imgSize, fitMode, SuiBackgroundPosition.Center );
					}
					if ( fitRect.Width >= 1 && fitRect.Height >= 1 )
						DrawPixmapInRect( pm, abs, fitRect, opacity, radius );
				}
			}
		}

		if ( hasBorder )
		{
			Editor.Paint.SetPen( border.Value.WithAlpha( border.Value.a * opacity ), s.BorderWidth );
			Editor.Paint.ClearBrush();
			DrawRect( rect, radius );
		}
	}

	/// <summary>
	/// Translate <see cref="SuiBackgroundSize"/> to the <see cref="SuiImageFitMode"/>
	/// the canvas helper <c>ApplyFitMode</c> already understands. Custom is
	/// handled separately in the caller (explicit px W/H).
	/// </summary>
	private static SuiImageFitMode MapBackgroundSize( SuiBackgroundSize size ) => size switch
	{
		SuiBackgroundSize.Cover => SuiImageFitMode.Cover,
		SuiBackgroundSize.Stretch => SuiImageFitMode.Stretch,
		SuiBackgroundSize.Custom => SuiImageFitMode.Contain, // fallback when W/H both 0
		_ => SuiImageFitMode.Contain,
	};

	/// <summary>
	/// Resolve canvas paint radius from element shape preset (interactive
	/// types) or Style.BorderRadius. Round/Pill collapse to half the smaller
	/// dimension — Qt's drawRect radius clamps to that anyway, this keeps the
	/// visual identical to what background-radius: 50%/9999px produces.
	/// </summary>
	private static float ResolveCanvasRadius( SuiElement el, Rect rect )
	{
		var s = el?.Style;
		if ( s == null ) return 0f;

		if ( el.Props != null
			&& (el.Type == SuiElementType.Button
				|| el.Type == SuiElementType.InventorySlot
				|| el.Type == SuiElementType.ItemIcon) )
		{
			switch ( el.Props.ButtonShape )
			{
				case SuiButtonShape.Square: return 0f;
				case SuiButtonShape.Round:
				case SuiButtonShape.Pill:
					return MathF.Min( rect.Width, rect.Height ) * 0.5f;
				// Rectangle + Custom fall through to Style.BorderRadius.
			}
		}

		return MathF.Max( 0f, s.BorderRadius );
	}

	private static void DrawRect( Rect rect, float radius )
	{
		if ( radius > 0 ) Editor.Paint.DrawRect( rect, radius );
		else Editor.Paint.DrawRect( rect );
	}

	private void PaintText( SuiElement el, Rect rect, float opacity )
	{
		var p = el.Props;
		if ( p == null || string.IsNullOrEmpty( p.Text ) ) return;

		var color = ParseColor( p.Color ) ?? Color.White;
		var weight = MapFontWeight( p.FontWeight );
		var fontName = string.IsNullOrEmpty( p.FontFamily ) ? Theme.DefaultFont : p.FontFamily;

		Editor.Paint.SetFont( fontName, p.FontSize, weight );
		Editor.Paint.SetPen( color.WithAlpha( color.a * opacity ) );
		Editor.Paint.ClearBrush();

		// 2D alignment — horizontal from TextAlign, vertical from VerticalAlign
		// (only meaningful when TextSizeMode is Fixed or AutoHeightWrap).
		// In Auto mode the rect IS the text size, so all flags collapse to LeftTop.
		var flag = ResolveTextFlag( p );

		var displayText = p.Text;
		if ( p.TextOverflow == SuiTextOverflow.Ellipsis )
			displayText = Editor.Paint.GetElidedText( p.Text, rect.Width, ElideMode.Right, flag );

		Editor.Paint.DrawText( rect, displayText, flag );
	}

	private static TextFlag ResolveTextFlag( SuiElementProps p )
	{
		// Auto mode: rect == text size → align top-left so text fills the box.
		if ( p.TextSizeMode == SuiTextSizeMode.Auto ) return TextFlag.LeftTop;

		// AutoHeightWrap: width fixed, height auto → top is the natural anchor.
		if ( p.TextSizeMode == SuiTextSizeMode.AutoHeightWrap )
		{
			return p.TextAlign switch
			{
				SuiTextAlign.Center => TextFlag.CenterTop,
				SuiTextAlign.Right => TextFlag.RightTop,
				_ => TextFlag.LeftTop,
			};
		}

		// Fixed: full 2D matrix from TextAlign × VerticalAlign.
		return (p.TextAlign, p.VerticalAlign) switch
		{
			(SuiTextAlign.Left, SuiVerticalAlign.Top) => TextFlag.LeftTop,
			(SuiTextAlign.Left, SuiVerticalAlign.Center) => TextFlag.LeftCenter,
			(SuiTextAlign.Left, SuiVerticalAlign.Bottom) => TextFlag.LeftBottom,
			(SuiTextAlign.Center, SuiVerticalAlign.Top) => TextFlag.CenterTop,
			(SuiTextAlign.Center, SuiVerticalAlign.Center) => TextFlag.Center,
			(SuiTextAlign.Center, SuiVerticalAlign.Bottom) => TextFlag.CenterBottom,
			(SuiTextAlign.Right, SuiVerticalAlign.Top) => TextFlag.RightTop,
			(SuiTextAlign.Right, SuiVerticalAlign.Center) => TextFlag.RightCenter,
			(SuiTextAlign.Right, SuiVerticalAlign.Bottom) => TextFlag.RightBottom,
			(SuiTextAlign.Justify, _) => TextFlag.LeftCenter, // Paint API has no justify
			_ => TextFlag.LeftTop,
		};
	}

	private void PaintImage( SuiElement el, Rect rect, float opacity )
	{
		var p = el.Props;
		if ( p == null || string.IsNullOrEmpty( p.ImagePath ) ) return;

		var abs = ResolveProjectPath( p.ImagePath );
		if ( string.IsNullOrEmpty( abs ) ) return;

		// Load at NATIVE resolution. Pre-resizing via LoadImage(path, w, h) does a
		// CPU-side downsample whose quality (especially for big shrink ratios like
		// 512→120) loses sharpness compared to the runtime GPU sampler. By keeping
		// the pixmap native-size and letting Paint.Scale handle the fit, the GPU's
		// bilinear filter does the resample — same path the runtime preview uses.
		Pixmap nativePixmap;
		try { nativePixmap = Editor.Paint.LoadImage( abs ); }
		catch { return; }
		if ( nativePixmap == null || nativePixmap.Width <= 1 || nativePixmap.Height <= 1 ) return;

		var fitRect = ApplyFitMode( rect, new Vector2( nativePixmap.Width, nativePixmap.Height ), p.FitMode, p.BackgroundPosition );
		if ( fitRect.Width < 1 || fitRect.Height < 1 ) return;

		// Honor border-radius so the canvas clips the image the same way SCSS does
		// at runtime (s&box's Panel applies border-radius as an alpha mask).
		var borderRadius = MathF.Max( 0, el.Style?.BorderRadius ?? 0 );
		DrawPixmapInRect( nativePixmap, abs, fitRect, opacity, borderRadius );

		// Tint approximation: overlay a colored rect when tint != white.
		// Runtime CSS background-image-tint uses a shader multiply; this is
		// visually close enough for strong tints.
		var tint = ParseColor( p.Tint );
		if ( tint.HasValue && (tint.Value.r < 0.99f || tint.Value.g < 0.99f || tint.Value.b < 0.99f) )
		{
			var tintColor = tint.Value.WithAlpha( opacity * 0.5f );
			Editor.Paint.SetBrush( tintColor );
			Editor.Paint.ClearPen();
			Editor.Paint.DrawRect( fitRect );
		}
	}

	/// <summary>
	/// Draws a pixmap into <paramref name="targetRect"/> using
	/// <c>Editor.Paint.Draw(Rect, Pixmap, float alpha, float borderRadius)</c>
	/// — the same Qt-backed GPU path Facepunch's editor uses internally. Qt's
	/// <c>drawPixmap</c> stretches source→target with bilinear smoothing in one
	/// pass, no tiling/brush dance.
	///
	/// We still pre-resize for heavy downsamples. Qt's drawPixmap is bilinear
	/// without mipmaps, so when source is much larger than display
	/// (e.g. native 512px → 60px on screen at zoom 0.5×), one sample per
	/// output pixel misses detail = aliasing. We pre-resize via
	/// <c>LoadImage(path, w, h)</c> at 2× display to give the GPU a
	/// well-conditioned source for its final bilinear sample.
	/// </summary>
	private void DrawPixmapInRect( Pixmap pixmap, string absPath, Rect targetRect, float alpha = 1.0f, float borderRadius = 0f )
	{
		if ( pixmap == null || pixmap.Width < 1 || pixmap.Height < 1 ) return;
		if ( targetRect.Width < 1 || targetRect.Height < 1 ) return;

		// Heavy-downsample guard.
		var displayW = MathF.Max( 1, targetRect.Width * Zoom );
		var displayH = MathF.Max( 1, targetRect.Height * Zoom );
		var oversampleW = (int)MathF.Round( displayW * 2f );
		var oversampleH = (int)MathF.Round( displayH * 2f );

		var toUse = pixmap;
		if ( oversampleW >= 1 && oversampleH >= 1
			&& (pixmap.Width > oversampleW * 2 || pixmap.Height > oversampleH * 2)
			&& !string.IsNullOrEmpty( absPath ) )
		{
			try { toUse = Editor.Paint.LoadImage( absPath, oversampleW, oversampleH ); }
			catch { toUse = pixmap; }
			if ( toUse == null || toUse.Width < 1 || toUse.Height < 1 ) toUse = pixmap;
		}

		// Editor.Paint.Draw( Rect, Pixmap, alpha, borderRadius ) — Qt's drawPixmap
		// with rounded clip mask. See reference_sbox_paint_image_api memory note.
		Editor.Paint.Draw( targetRect, toUse, alpha, borderRadius );
	}

	private void PaintButtonLabel( SuiElement el, Rect rect, float opacity )
	{
		var p = el.Props;
		if ( p == null || string.IsNullOrEmpty( p.ButtonText ) ) return;

		var color = ParseColor( p.Color ) ?? Color.White;
		var fontName = string.IsNullOrEmpty( p.FontFamily ) ? Theme.DefaultFont : p.FontFamily;
		Editor.Paint.SetFont( fontName, p.FontSize > 0 ? p.FontSize : 14, MapFontWeight( p.FontWeight ) );
		Editor.Paint.SetPen( color.WithAlpha( color.a * opacity ) );
		Editor.Paint.ClearBrush();
		Editor.Paint.DrawText( rect, p.ButtonText, TextFlag.Center );
	}

	private void PaintProgressFill( SuiElement el, Rect rect, float opacity )
	{
		var p = el.Props;
		if ( p == null ) return;
		var range = p.ProgressMax - p.ProgressMin;
		if ( range <= 0 ) return;

		var t = MathF.Max( 0, MathF.Min( 1, (p.ProgressPreviewValue - p.ProgressMin) / range ) );
		var fillColor = ParseColor( p.ProgressFillColor ) ?? new Color( 0.29f, 0.87f, 0.5f );

		var fillRect = new Rect( rect.Left, rect.Top, rect.Width * t, rect.Height );
		Editor.Paint.SetBrush( fillColor.WithAlpha( fillColor.a * opacity ) );
		Editor.Paint.ClearPen();
		var radius = MathF.Max( 0, el.Style?.BorderRadius ?? 0 );
		DrawRect( fillRect, radius );
	}

	private void PaintItemIcon( SuiElement el, Rect rect, float opacity )
	{
		var p = el.Props;
		if ( p == null || string.IsNullOrEmpty( p.PreviewIconPath ) ) return;

		var abs = ResolveProjectPath( p.PreviewIconPath );
		if ( string.IsNullOrEmpty( abs ) ) return;

		Pixmap nativePixmap;
		try { nativePixmap = Editor.Paint.LoadImage( abs ); }
		catch { return; }
		if ( nativePixmap == null || nativePixmap.Width <= 1 || nativePixmap.Height <= 1 ) return;

		var fitRect = ApplyFitMode( rect, new Vector2( nativePixmap.Width, nativePixmap.Height ),
			SuiImageFitMode.Contain, SuiBackgroundPosition.Center );
		if ( fitRect.Width < 1 || fitRect.Height < 1 ) return;

		var iconRadius = MathF.Max( 0, el.Style?.BorderRadius ?? 0 );
		DrawPixmapInRect( nativePixmap, abs, fitRect, opacity, iconRadius );

		// Stack count overlay
		if ( p.PreviewCount > 1 )
		{
			Editor.Paint.SetFont( Theme.DefaultFont, 11, 700 );
			Editor.Paint.SetPen( Color.White );
			Editor.Paint.ClearBrush();
			Editor.Paint.DrawText( rect.Shrink( 4 ), p.PreviewCount.ToString(), TextFlag.RightBottom );
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Helpers
	// ─────────────────────────────────────────────────────────────────────

	private float ResolveOpacity( SuiElement el )
	{
		var s = el.Style;
		if ( s == null ) return 1f;
		var op = s.Opacity;
		if ( s.Visibility == SuiVisibility.Hidden ) op = 0f;
		// Walk up — element opacity multiplies through ancestors. We don't
		// recurse here because the caller renders top-down; ancestor's opacity
		// is already baked into Paint.SetBrush at the parent's draw step. For
		// simplicity we use just the element's own opacity for now (matches
		// CSS opacity per element semantics).
		return MathF.Max( 0, MathF.Min( 1, op ) );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  V1.5 M4 — Input widget canvas paint (PRD 21 § 3)
	// ─────────────────────────────────────────────────────────────────────

	private void PaintTextEntry( SuiElement el, Rect rect, float opacity )
	{
		var p = el.Props;
		if ( p == null ) return;

		// Inner padding so text doesn't kiss the edge.
		var inner = new Rect( rect.Left + 8, rect.Top + 4, MathF.Max( 0, rect.Width - 16 ), MathF.Max( 0, rect.Height - 8 ) );

		var hasValue = !string.IsNullOrEmpty( p.PreviewValue );
		var displayText = hasValue ? p.PreviewValue : (p.PlaceholderText ?? "");
		var baseColor = ParseColor( p.Color ) ?? Color.White;
		var color = hasValue ? baseColor : baseColor.WithAlpha( baseColor.a * 0.5f );

		var fontName = string.IsNullOrEmpty( p.FontFamily ) ? Theme.DefaultFont : p.FontFamily;
		Editor.Paint.SetFont( fontName, p.FontSize > 0 ? p.FontSize : 14, MapFontWeight( p.FontWeight ) );
		Editor.Paint.SetPen( color.WithAlpha( color.a * opacity ) );
		Editor.Paint.ClearBrush();
		Editor.Paint.DrawText( inner, displayText, TextFlag.LeftCenter );

		// Disabled visual — overlay 60% alpha black to dim the widget.
		if ( p.ReadOnly )
		{
			Editor.Paint.SetBrush( new Color( 0, 0, 0, 0.4f * opacity ) );
			Editor.Paint.ClearPen();
			var radius = MathF.Max( 0, el.Style?.BorderRadius ?? 0 );
			DrawRect( rect, radius );
		}
	}

	private void PaintSlider( SuiElement el, Rect rect, float opacity )
	{
		var p = el.Props;
		if ( p == null ) return;

		// Engine SliderControl defaults (sbox-public/Controls/SliderControl.razor.scss):
		//   track height 7px, border-radius 4px, margin 8px
		//   thumb 16x16 circular
		var trackColor = ParseColor( p.SliderTrackColor ) ?? new Color( 0.53f, 0.53f, 0.53f );
		var fillColor = ParseColor( p.SliderFillColor ) ?? Color.White;
		var handleColor = ParseColor( p.SliderHandleColor ) ?? Color.White;

		var midY = rect.Top + rect.Height * 0.5f;
		const float TrackH = 7f;
		const float TrackPad = 8f;          // engine .track margin
		const float ThumbSize = 16f;
		var trackRect = new Rect( rect.Left + TrackPad, midY - TrackH * 0.5f, MathF.Max( 0, rect.Width - TrackPad * 2 ), TrackH );

		Editor.Paint.SetBrush( trackColor.WithAlpha( trackColor.a * opacity ) );
		Editor.Paint.ClearPen();
		DrawRect( trackRect, 4f );

		var range = p.SliderMax - p.SliderMin;
		var t = range <= 0 ? 0f : MathF.Max( 0, MathF.Min( 1, (p.SliderValue - p.SliderMin) / range ) );

		var fillRect = new Rect( trackRect.Left, trackRect.Top, trackRect.Width * t, TrackH );
		Editor.Paint.SetBrush( fillColor.WithAlpha( fillColor.a * opacity ) );
		DrawRect( fillRect, 4f );

		// Thumb (16x16 circular, centered on the value position).
		var thumbCx = trackRect.Left + trackRect.Width * t;
		var thumbRect = new Rect( thumbCx - ThumbSize * 0.5f, midY - ThumbSize * 0.5f, ThumbSize, ThumbSize );
		Editor.Paint.SetBrush( handleColor.WithAlpha( handleColor.a * opacity ) );
		DrawRect( thumbRect, ThumbSize * 0.5f );

		// Tooltip preview — match the engine pill exactly so canvas == Play.
		// Honors SliderTooltipBgColor / SliderTooltipTextColor; falls back
		// to engine defaults (black bg + white text) when the author left
		// the color fields empty.
		if ( p.SliderShowValue )
		{
			var tipBg = ParseColor( p.SliderTooltipBgColor ) ?? Color.Black;
			var tipFg = ParseColor( p.SliderTooltipTextColor ) ?? Color.White;
			var label = p.SliderValue.ToString( "0.##" );

			Editor.Paint.SetFont( Theme.DefaultFont, 12, 400 );
			var textSize = Editor.Paint.MeasureText( new Rect( 0, 0, 9999, 9999 ), label, TextFlag.LeftTop );
			const float padX = 8f, padY = 4f;
			var pillW = textSize.Width + padX * 2;
			var pillH = textSize.Height + padY * 2;
			var pillTop = rect.Top - pillH - 6;       // 150% above thumb (~16*1.5 -> ~24, we use 6 + pillH gap)
			var pillRect = new Rect( thumbCx - pillW * 0.5f, pillTop, pillW, pillH );

			// Pill bg.
			Editor.Paint.SetBrush( tipBg.WithAlpha( tipBg.a * opacity ) );
			Editor.Paint.ClearPen();
			DrawRect( pillRect, pillH * 0.4f );

			// Pill text.
			Editor.Paint.SetPen( tipFg.WithAlpha( tipFg.a * opacity ) );
			Editor.Paint.ClearBrush();
			Editor.Paint.DrawText( pillRect, label, TextFlag.Center );
		}
	}

	private void PaintToggle( SuiElement el, Rect rect, float opacity )
	{
		var p = el.Props;
		if ( p == null ) return;

		// Engine Checkbox: 20x20 square checkmark box (left), optional label
		// (right). The wrapper `<checkbox>` has no width constraint — engine
		// flexes row, items kept their natural size by our SCSS emit.
		const float BoxSize = 20f;
		var hasLabel = !string.IsNullOrEmpty( p.ToggleLabelText );

		// Center vertically inside the element rect.
		var boxRect = new Rect( rect.Left, rect.Top + (rect.Height - BoxSize) * 0.5f, BoxSize, BoxSize );

		// Box visuals — uncheck = dark fill + dim border; checked = green fill.
		var radius = 3f;
		if ( p.ToggleChecked )
		{
			var fill = new Color( 0.13f, 0.77f, 0.37f );    // #22c55e
			Editor.Paint.SetBrush( fill.WithAlpha( fill.a * opacity ) );
			Editor.Paint.ClearPen();
			DrawRect( boxRect, radius );

			// Checkmark — Material "check" icon. We approximate via two
			// connected segments rendered as filled rects for the canvas
			// (the engine uses the real font icon).
			var checkColor = Color.White.WithAlpha( opacity );
			Editor.Paint.SetPen( checkColor, 2.2f );
			Editor.Paint.ClearBrush();
			var pad = BoxSize * 0.22f;
			var p1 = new Vector2( boxRect.Left + pad, boxRect.Top + BoxSize * 0.55f );
			var p2 = new Vector2( boxRect.Left + BoxSize * 0.42f, boxRect.Bottom - pad );
			var p3 = new Vector2( boxRect.Right - pad, boxRect.Top + pad );
			Editor.Paint.DrawLine( p1, p2 );
			Editor.Paint.DrawLine( p2, p3 );
		}
		else
		{
			var bg = new Color( 0, 0, 0, 0.4f );
			Editor.Paint.SetBrush( bg.WithAlpha( bg.a * opacity ) );
			Editor.Paint.ClearPen();
			DrawRect( boxRect, radius );

			var border = new Color( 1, 1, 1, 0.3f );
			Editor.Paint.SetPen( border.WithAlpha( border.a * opacity ), 1 );
			Editor.Paint.ClearBrush();
			DrawRect( boxRect, radius );
		}

		// Label right of the box, 8px gap (engine gap-emitted match).
		if ( hasLabel && rect.Width > BoxSize + 8 )
		{
			var labelRect = new Rect( boxRect.Right + 8, rect.Top, rect.Width - BoxSize - 8, rect.Height );
			var labelColor = ParseColor( p.Color ) ?? Color.White;
			Editor.Paint.SetFont( Theme.DefaultFont, p.FontSize > 0 ? p.FontSize : 14, MapFontWeight( p.FontWeight ) );
			Editor.Paint.SetPen( labelColor.WithAlpha( labelColor.a * opacity ) );
			Editor.Paint.ClearBrush();
			Editor.Paint.DrawText( labelRect, p.ToggleLabelText, TextFlag.LeftCenter );
		}
	}

	private void PaintDropDown( SuiElement el, Rect rect, float opacity )
	{
		var p = el.Props;
		if ( p == null ) return;

		// Inner area = element width minus 24px reserved on the right for the
		// indicator (engine icon). 8px left padding to match SCSS emit.
		const float IndicatorWidth = 24f;
		var inner = new Rect( rect.Left + 8, rect.Top, MathF.Max( 0, rect.Width - IndicatorWidth - 8 ), rect.Height );

		string label;
		if ( p.DropDownOptions != null && p.DropDownOptions.Count > 0
			&& p.DropDownSelectedIndex >= 0 && p.DropDownSelectedIndex < p.DropDownOptions.Count )
		{
			label = p.DropDownOptions[p.DropDownSelectedIndex] ?? "";
		}
		else
		{
			label = "";
		}

		var color = ParseColor( p.Color ) ?? Color.White;
		Editor.Paint.SetFont( Theme.DefaultFont, p.FontSize > 0 ? p.FontSize : 14, MapFontWeight( p.FontWeight ) );
		Editor.Paint.SetPen( color.WithAlpha( color.a * opacity ) );
		Editor.Paint.ClearBrush();
		Editor.Paint.DrawText( inner, label, TextFlag.LeftCenter );

		// Right side: chevron "v" approximating Material expand_more icon.
		// Drawn as two diagonal strokes meeting at a point.
		var indCx = rect.Right - IndicatorWidth * 0.5f;
		var indCy = rect.Top + rect.Height * 0.5f;
		Editor.Paint.SetPen( color.WithAlpha( color.a * opacity ), 1.8f );
		Editor.Paint.ClearBrush();
		const float chevW = 8f;
		const float chevH = 4f;
		var a = new Vector2( indCx - chevW * 0.5f, indCy - chevH * 0.5f );
		var b = new Vector2( indCx, indCy + chevH * 0.5f );
		var c = new Vector2( indCx + chevW * 0.5f, indCy - chevH * 0.5f );
		Editor.Paint.DrawLine( a, b );
		Editor.Paint.DrawLine( b, c );
	}

	private static Color? ParseColor( string raw )
	{
		if ( string.IsNullOrEmpty( raw ) ) return null;
		var s = raw.Trim();

		// rgba(r,g,b,a) / rgb(r,g,b) — same format the runtime Sandbox.UI
		// parser accepts (see ui-razor reference). Lenient on spacing. Without
		// this branch, canvas falls back to "no bg painted" for any rgba value
		// while the runtime renders it correctly, causing a canvas/preview gap.
		if ( s.StartsWith( "rgb", StringComparison.OrdinalIgnoreCase ) )
		{
			var open = s.IndexOf( '(' );
			var close = s.IndexOf( ')', open + 1 );
			if ( open > 0 && close > open )
			{
				var inside = s.Substring( open + 1, close - open - 1 );
				var parts = inside.Split( ',' );
				if ( parts.Length >= 3 )
				{
					try
					{
						int r = int.Parse( parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture );
						int g = int.Parse( parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture );
						int b = int.Parse( parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture );
						float a = 1f;
						if ( parts.Length >= 4 )
							a = float.Parse( parts[3].Trim(), System.Globalization.CultureInfo.InvariantCulture );
						r = System.Math.Clamp( r, 0, 255 );
						g = System.Math.Clamp( g, 0, 255 );
						b = System.Math.Clamp( b, 0, 255 );
						a = System.Math.Clamp( a, 0f, 1f );
						return new Color( r / 255f, g / 255f, b / 255f, a );
					}
					catch { return null; }
				}
			}
			return null;
		}

		// #hex (3/6/8 digits).
		var hex = s.StartsWith( "#" ) ? s.Substring( 1 ) : s;
		try
		{
			byte r, g, b, ab = 255;
			if ( hex.Length == 6 )
			{
				r = (byte)Convert.ToInt32( hex.Substring( 0, 2 ), 16 );
				g = (byte)Convert.ToInt32( hex.Substring( 2, 2 ), 16 );
				b = (byte)Convert.ToInt32( hex.Substring( 4, 2 ), 16 );
			}
			else if ( hex.Length == 8 )
			{
				r = (byte)Convert.ToInt32( hex.Substring( 0, 2 ), 16 );
				g = (byte)Convert.ToInt32( hex.Substring( 2, 2 ), 16 );
				b = (byte)Convert.ToInt32( hex.Substring( 4, 2 ), 16 );
				ab = (byte)Convert.ToInt32( hex.Substring( 6, 2 ), 16 );
			}
			else if ( hex.Length == 3 )
			{
				r = (byte)(Convert.ToInt32( hex.Substring( 0, 1 ), 16 ) * 17);
				g = (byte)(Convert.ToInt32( hex.Substring( 1, 1 ), 16 ) * 17);
				b = (byte)(Convert.ToInt32( hex.Substring( 2, 1 ), 16 ) * 17);
			}
			else return null;

			return new Color( r / 255f, g / 255f, b / 255f, ab / 255f );
		}
		catch
		{
			return null;
		}
	}

	private static int MapFontWeight( SuiFontWeight w ) => w switch
	{
		SuiFontWeight.Light => 300,
		SuiFontWeight.Normal => 400,
		SuiFontWeight.Medium => 500,
		SuiFontWeight.SemiBold => 600,
		SuiFontWeight.Bold => 700,
		SuiFontWeight.ExtraBold => 800,
		_ => 400,
	};

	private static TextFlag MapTextAlign( SuiTextAlign a ) => a switch
	{
		SuiTextAlign.Left => TextFlag.LeftCenter,
		SuiTextAlign.Center => TextFlag.Center,
		SuiTextAlign.Right => TextFlag.RightCenter,
		SuiTextAlign.Justify => TextFlag.LeftCenter, // Paint API has no justify; fall back to left
		_ => TextFlag.LeftCenter,
	};

	private string ResolveProjectPath( string projectRelativePath )
	{
		if ( string.IsNullOrEmpty( projectRelativePath ) ) return null;
		if ( string.IsNullOrEmpty( _projectAssetsRoot ) ) return null;

		var rel = projectRelativePath.Replace( '\\', '/' ).TrimStart( '/' );
		return System.IO.Path.Combine( _projectAssetsRoot, rel ).Replace( '\\', '/' );
	}

	/// <summary>
	/// Compute the rect into which an image should be drawn based on its native
	/// pixel size and the FitMode/BackgroundPosition. Mirrors how CSS
	/// background-size + background-position would lay out the same image.
	/// </summary>
	private static Rect ApplyFitMode( Rect container, Vector2 imageSize, SuiImageFitMode mode, SuiBackgroundPosition pos )
	{
		if ( mode == SuiImageFitMode.Stretch || mode == SuiImageFitMode.None )
			return container;

		var imageAspect = imageSize.x / imageSize.y;
		var containerAspect = container.Width / container.Height;

		float w, h;
		if ( mode == SuiImageFitMode.Contain )
		{
			if ( imageAspect > containerAspect ) { w = container.Width; h = w / imageAspect; }
			else { h = container.Height; w = h * imageAspect; }
		}
		else // Cover
		{
			if ( imageAspect > containerAspect ) { h = container.Height; w = h * imageAspect; }
			else { w = container.Width; h = w / imageAspect; }
		}

		// Position the fitted image within the container.
		float x, y;
		switch ( pos )
		{
			case SuiBackgroundPosition.TopLeft:     x = container.Left;                      y = container.Top; break;
			case SuiBackgroundPosition.Top:         x = container.Left + (container.Width - w) * 0.5f; y = container.Top; break;
			case SuiBackgroundPosition.TopRight:    x = container.Right - w;                 y = container.Top; break;
			case SuiBackgroundPosition.Left:        x = container.Left;                      y = container.Top + (container.Height - h) * 0.5f; break;
			case SuiBackgroundPosition.Right:       x = container.Right - w;                 y = container.Top + (container.Height - h) * 0.5f; break;
			case SuiBackgroundPosition.BottomLeft:  x = container.Left;                      y = container.Bottom - h; break;
			case SuiBackgroundPosition.Bottom:      x = container.Left + (container.Width - w) * 0.5f; y = container.Bottom - h; break;
			case SuiBackgroundPosition.BottomRight: x = container.Right - w;                 y = container.Bottom - h; break;
			case SuiBackgroundPosition.Center:
			default:                                x = container.Left + (container.Width - w) * 0.5f; y = container.Top + (container.Height - h) * 0.5f; break;
		}

		return new Rect( x, y, w, h );
	}
}
