using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
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
				PaintCanvasRoot( el, rect );
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
		}

		PaintChildren( el );
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

		if ( !hasBg && !hasBorder ) return;

		var radius = MathF.Max( 0, s.BorderRadius );

		if ( hasBg )
		{
			Editor.Paint.SetBrush( bg.Value.WithAlpha( bg.Value.a * opacity ) );
			Editor.Paint.ClearPen();
			DrawRect( rect, radius );
		}

		if ( hasBorder )
		{
			Editor.Paint.SetPen( border.Value.WithAlpha( border.Value.a * opacity ), s.BorderWidth );
			Editor.Paint.ClearBrush();
			DrawRect( rect, radius );
		}
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
