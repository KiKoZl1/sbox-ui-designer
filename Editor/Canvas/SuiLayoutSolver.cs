using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Canvas;

/// <summary>
/// The single source of truth for "where is each element on the document".
/// Given a <see cref="SuiDocument"/>, produces a flat
/// <c>Dictionary&lt;elementId, Rect&gt;</c> in <b>logical pixels</b> (the document's
/// own coord space — typically 1920x1080).
///
/// Renderer reads it to draw. HitTester reads it to pick. Selection chrome
/// reads it to position handles. <b>One math, three consumers.</b> Render fidelity
/// vs. hit-test fidelity drift is impossible by construction — they share this dict.
///
/// Phase 1: absolute layout only (anchor + pivot). The rules mirror
/// <c>SuiScssGenerator.EmitAnchorRules</c> exactly so the canvas paint matches
/// what the runtime SCSS would render.
///
/// Phase 3 will add flex layout (HorizontalBox / VerticalBox / Grid) via the
/// 3-pass Yoga subset algorithm. Until then flex children fall back to absolute
/// (rect.X/Y treated as TopLeft offsets in the parent).
/// </summary>
public sealed class SuiLayoutSolver
{
	public Vector2 PanelSize { get; set; }
	public Dictionary<string, Rect> Rects { get; } = new();
	public Dictionary<string, SuiElement> ById { get; } = new();

	/// <summary>
	/// Measured size for Text elements in <see cref="SuiTextSizeMode.Auto"/>.
	/// Populated at the start of <see cref="Solve"/> by calling
	/// <c>Editor.Paint.MeasureText</c> per element. Other modes don't appear here.
	/// </summary>
	private readonly Dictionary<string, Vector2> _autoTextSizes = new();

	public SuiLayoutSolver( Vector2 panelSize )
	{
		PanelSize = panelSize;
	}

	/// <summary>
	/// Run the solver on the document. Populates <see cref="Rects"/> and
	/// <see cref="ById"/>. Subsequent calls clear and rebuild.
	/// </summary>
	public void Solve( SuiDocument document )
	{
		Rects.Clear();
		ById.Clear();
		_autoTextSizes.Clear();
		if ( document == null ) return;

		foreach ( var el in document.Elements )
			if ( !string.IsNullOrEmpty( el.Id ) ) ById[el.Id] = el;

		// Measure all Auto-mode Text elements once before walking the tree.
		// Paint context is active here (we're called from a viewport's OnPaint),
		// so MeasureText returns valid sizes.
		MeasureAutoTexts( document );

		var root = document.GetRoot();
		if ( root == null ) return;

		// Root always fills the panel.
		Rects[root.Id] = new Rect( 0, 0, PanelSize.x, PanelSize.y );

		// Recurse — each container picks absolute or flex based on Layout.Mode.
		SolveChildren( root, Rects[root.Id] );
	}

	private void SolveChildren( SuiElement parent, Rect parentRect )
	{
		if ( parent?.Children == null || parent.Children.Count == 0 ) return;

		// Grid-like containers (InventoryGrid, Grid) use a dedicated regular-tile
		// pass driven by parent.Props (Columns/CellWidth/CellHeight/GridGap).
		// This works regardless of Layout.Mode so legacy docs (pre-ApplyTypeDefaults
		// fix where InventoryGrid defaulted to Absolute) still tile correctly.
		// Hotbar is intentionally NOT in this list — it's a single row of slots
		// laid out by regular flex (Row + NoWrap), which honors each slot's own
		// Width/Height and the parent's Gap directly. Routing it through SolveGrid
		// would force a Columns×Rows grid and (with default Columns=1) collapse it
		// into a vertical stack.
		var isGrid = parent.Type == SuiElementType.InventoryGrid
			|| parent.Type == SuiElementType.Grid;
		if ( isGrid )
		{
			var laidGrid = SuiFlexLayout.SolveGrid( parent, parentRect, ById );
			foreach ( var item in laidGrid )
			{
				Rects[item.Id] = item.Rect;
				if ( ById.TryGetValue( item.Id, out var c ) )
					SolveChildren( c, item.Rect );
			}
			return;
		}

		// Flex container? Run the flex layout pass; children's rects come from there.
		// Hotbar always falls through here (Mode=Flex set by ApplyTypeDefaults).
		if ( parent.Layout?.Mode == SuiLayoutMode.Flex || parent.Type == SuiElementType.Hotbar )
		{
			// Capture _autoTextSizes via closure so flex children that are Auto-Text
			// use measured size as their intrinsic size.
			Vector2 IntrinsicWithOverride( SuiElement el )
			{
				if ( _autoTextSizes.TryGetValue( el.Id, out var measured ) ) return measured;
				return IntrinsicSizeOf( el );
			}
			var laid = SuiFlexLayout.Solve( parent, parentRect, ById, IntrinsicWithOverride );
			foreach ( var item in laid )
			{
				Rects[item.Id] = item.Rect;
				if ( ById.TryGetValue( item.Id, out var c ) )
					SolveChildren( c, item.Rect );
			}
			return;
		}

		// Absolute container — each child gets ResolveAbsoluteRectWithOverride
		// (which applies the Auto-Text size measurement override transparently).
		foreach ( var childId in parent.Children )
		{
			if ( !ById.TryGetValue( childId, out var child ) ) continue;
			var rect = ResolveAbsoluteRectWithOverride( child, parentRect );
			Rects[child.Id] = rect;
			SolveChildren( child, rect );
		}
	}

	/// <summary>
	/// Return <paramref name="parent"/>'s children in paint order: ascending
	/// <c>Layout.ZIndex</c>, with hierarchy order as a stable tie-break (so
	/// elements with equal ZIndex preserve their authoring order). Render
	/// pass calls this so high-Z elements end up drawn LAST = visually on top.
	/// Hit-test consumes the same order so visual ↔ click parity holds.
	/// </summary>
	public static List<SuiElement> GetRenderOrderedChildren(
		SuiElement parent,
		IReadOnlyDictionary<string, SuiElement> byId )
	{
		var list = new List<SuiElement>();
		if ( parent?.Children == null ) return list;
		for ( int i = 0; i < parent.Children.Count; i++ )
		{
			if ( byId.TryGetValue( parent.Children[i], out var c ) )
				list.Add( c );
		}
		// Sort with stable insertion-order tie-break.
		var indexed = new List<(SuiElement el, int idx)>( list.Count );
		for ( int i = 0; i < list.Count; i++ ) indexed.Add( (list[i], i) );
		indexed.Sort( ( a, b ) =>
		{
			var az = a.el.Layout?.ZIndex ?? 0;
			var bz = b.el.Layout?.ZIndex ?? 0;
			if ( az != bz ) return az.CompareTo( bz );
			return a.idx.CompareTo( b.idx );
		} );
		var result = new List<SuiElement>( indexed.Count );
		foreach ( var (el, _) in indexed ) result.Add( el );
		return result;
	}

	/// <summary>
	/// Measure Text elements that are in Auto mode. Used by absolute layout pass
	/// to override Layout.Width/Height with the actual rendered text size.
	/// </summary>
	private void MeasureAutoTexts( SuiDocument doc )
	{
		foreach ( var el in doc.Elements )
		{
			if ( el.Type != SuiElementType.Text ) continue;
			if ( el.Props == null ) continue;
			if ( el.Props.TextSizeMode != SuiTextSizeMode.Auto ) continue;
			if ( string.IsNullOrEmpty( el.Props.Text ) ) continue;

			var fontName = string.IsNullOrEmpty( el.Props.FontFamily ) ? Theme.DefaultFont : el.Props.FontFamily;
			var fontSize = el.Props.FontSize > 0 ? el.Props.FontSize : 14f;
			var weight = MapFontWeight( el.Props.FontWeight );

			Editor.Paint.SetFont( fontName, fontSize, weight );
			var size = Editor.Paint.MeasureText( el.Props.Text );
			_autoTextSizes[el.Id] = size;
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

	/// <summary>
	/// Resolve absolute rect for a child, applying Text-Auto size override
	/// if the element is a Text in Auto mode (uses measured size as W/H).
	/// </summary>
	private Rect ResolveAbsoluteRectWithOverride( SuiElement el, Rect parentRect )
	{
		if ( _autoTextSizes.TryGetValue( el.Id, out var measured ) )
		{
			// Temporarily swap layout W/H for the static math, restore after.
			var origW = el.Layout.Width;
			var origH = el.Layout.Height;
			el.Layout.Width = measured.x;
			el.Layout.Height = measured.y;
			try { return ResolveAbsoluteRect( el, parentRect ); }
			finally
			{
				el.Layout.Width = origW;
				el.Layout.Height = origH;
			}
		}
		return ResolveAbsoluteRect( el, parentRect );
	}

	/// <summary>
	/// Intrinsic size for flex children. Uses the layout's W/H if &gt; 0, else
	/// per-type defaults from <see cref="DefaultWidthFor"/> / <see cref="DefaultHeightFor"/>.
	/// </summary>
	private static Vector2 IntrinsicSizeOf( SuiElement el )
	{
		var l = el.Layout;
		var w = l != null && l.Width > 0 ? l.Width : DefaultWidthFor( el );
		var h = l != null && l.Height > 0 ? l.Height : DefaultHeightFor( el );
		return new Vector2( w, h );
	}

	/// <summary>
	/// True if the solver has computed a rect for the given element.
	/// </summary>
	public bool TryGetRect( string elementId, out Rect rect )
	{
		if ( !string.IsNullOrEmpty( elementId ) && Rects.TryGetValue( elementId, out rect ) )
			return true;
		rect = default;
		return false;
	}

	/// <summary>
	/// Compute an element's rect inside its parent's rect, applying anchor +
	/// pivot the same way <c>SuiScssGenerator.EmitAnchorRules</c> does. The
	/// implicit pivot per anchor matches the SCSS transform: TopLeft→(0,0),
	/// MiddleCenter→(0.5,0.5), BottomRight→(1,1), etc.
	///
	/// Returns the rect in the SAME coord space as <paramref name="parentRect"/>
	/// (i.e. relative to the panel root, not the parent's local space).
	/// </summary>
	public static Rect ResolveAbsoluteRect( SuiElement el, Rect parentRect )
	{
		var l = el.Layout;
		var w = l.Width > 0 ? l.Width : DefaultWidthFor( el );
		var h = l.Height > 0 ? l.Height : DefaultHeightFor( el );

		var pw = parentRect.Width;
		var ph = parentRect.Height;

		// Anchor → reference point in parent + implicit pivot on element + axis sign.
		// signX/signY flip when the anchor is on a "right"/"bottom" edge, because
		// in our schema X/Y on those anchors mean "offset INWARD" (matches CSS
		// `right: Xpx` / `bottom: Ypx`).
		float refX, refY, pivotX, pivotY, signX, signY;
		switch ( l.Anchor )
		{
			case SuiAnchor.TopLeft:      refX = 0;       refY = 0;       pivotX = 0;   pivotY = 0;   signX =  1; signY =  1; break;
			case SuiAnchor.TopCenter:    refX = pw*0.5f; refY = 0;       pivotX = 0.5f;pivotY = 0;   signX =  1; signY =  1; break;
			case SuiAnchor.TopRight:     refX = pw;      refY = 0;       pivotX = 1;   pivotY = 0;   signX = -1; signY =  1; break;
			case SuiAnchor.MiddleLeft:   refX = 0;       refY = ph*0.5f; pivotX = 0;   pivotY = 0.5f;signX =  1; signY =  1; break;
			case SuiAnchor.MiddleCenter: refX = pw*0.5f; refY = ph*0.5f; pivotX = 0.5f;pivotY = 0.5f;signX =  1; signY =  1; break;
			case SuiAnchor.MiddleRight:  refX = pw;      refY = ph*0.5f; pivotX = 1;   pivotY = 0.5f;signX = -1; signY =  1; break;
			case SuiAnchor.BottomLeft:   refX = 0;       refY = ph;      pivotX = 0;   pivotY = 1;   signX =  1; signY = -1; break;
			case SuiAnchor.BottomCenter: refX = pw*0.5f; refY = ph;      pivotX = 0.5f;pivotY = 1;   signX =  1; signY = -1; break;
			case SuiAnchor.BottomRight:  refX = pw;      refY = ph;      pivotX = 1;   pivotY = 1;   signX = -1; signY = -1; break;
			case SuiAnchor.Stretch:
				// X = left margin, Y = top margin, W = right margin, H = bottom margin.
				// Element fills the parent minus those four offsets.
				return new Rect(
					parentRect.Left + l.X,
					parentRect.Top + l.Y,
					System.Math.Max( 0, pw - l.X - l.Width ),
					System.Math.Max( 0, ph - l.Y - l.Height ) );

			case SuiAnchor.StretchHorizontal:
				// Horizontally stretched between X (left margin) and W (right margin).
				// Y = top offset (from parent.Top), H = element height.
				return new Rect(
					parentRect.Left + l.X,
					parentRect.Top + l.Y,
					System.Math.Max( 0, pw - l.X - l.Width ),
					h );

			case SuiAnchor.StretchVertical:
				// Vertically stretched between Y (top margin) and H (bottom margin).
				// X = left offset, W = element width.
				return new Rect(
					parentRect.Left + l.X,
					parentRect.Top + l.Y,
					w,
					System.Math.Max( 0, ph - l.Y - l.Height ) );
			default:                     refX = 0;       refY = 0;       pivotX = 0;   pivotY = 0;   signX =  1; signY =  1; break;
		}

		var posX = refX + l.X * signX;
		var posY = refY + l.Y * signY;
		var topLeftX = posX - pivotX * w;
		var topLeftY = posY - pivotY * h;

		return new Rect( parentRect.Left + topLeftX, parentRect.Top + topLeftY, w, h );
	}

	/// <summary>
	/// Inverse of the layout pass — converts a target visual rect (logical-pixel
	/// space) back into the X/Y/W/H values that, given the same anchor/pivot,
	/// would produce that rect. Used by drag-to-move + resize handles to write
	/// values back into the document.
	/// </summary>
	public static (float x, float y, float w, float h) RectToLayoutValues(
		Rect targetRect, SuiAnchor anchor, Rect parentRect )
	{
		var pw = parentRect.Width;
		var ph = parentRect.Height;
		var w = targetRect.Width;
		var h = targetRect.Height;

		// Stretch variants — X/Y/W/H are margins, not absolute positions.
		// (See solver Layout pass for the forward mapping.)
		switch ( anchor )
		{
			case SuiAnchor.Stretch:
			{
				var leftMargin = targetRect.Left - parentRect.Left;
				var topMargin = targetRect.Top - parentRect.Top;
				var rightMargin = parentRect.Right - targetRect.Right;
				var bottomMargin = parentRect.Bottom - targetRect.Bottom;
				return (leftMargin, topMargin, rightMargin, bottomMargin);
			}
			case SuiAnchor.StretchHorizontal:
			{
				var leftMargin = targetRect.Left - parentRect.Left;
				var topOffset = targetRect.Top - parentRect.Top;
				var rightMargin = parentRect.Right - targetRect.Right;
				return (leftMargin, topOffset, rightMargin, h);
			}
			case SuiAnchor.StretchVertical:
			{
				var leftOffset = targetRect.Left - parentRect.Left;
				var topMargin = targetRect.Top - parentRect.Top;
				var bottomMargin = parentRect.Bottom - targetRect.Bottom;
				return (leftOffset, topMargin, w, bottomMargin);
			}
		}

		float refX, refY, pivotX, pivotY, signX, signY;
		switch ( anchor )
		{
			case SuiAnchor.TopLeft:      refX = 0;       refY = 0;       pivotX = 0;   pivotY = 0;   signX =  1; signY =  1; break;
			case SuiAnchor.TopCenter:    refX = pw*0.5f; refY = 0;       pivotX = 0.5f;pivotY = 0;   signX =  1; signY =  1; break;
			case SuiAnchor.TopRight:     refX = pw;      refY = 0;       pivotX = 1;   pivotY = 0;   signX = -1; signY =  1; break;
			case SuiAnchor.MiddleLeft:   refX = 0;       refY = ph*0.5f; pivotX = 0;   pivotY = 0.5f;signX =  1; signY =  1; break;
			case SuiAnchor.MiddleCenter: refX = pw*0.5f; refY = ph*0.5f; pivotX = 0.5f;pivotY = 0.5f;signX =  1; signY =  1; break;
			case SuiAnchor.MiddleRight:  refX = pw;      refY = ph*0.5f; pivotX = 1;   pivotY = 0.5f;signX = -1; signY =  1; break;
			case SuiAnchor.BottomLeft:   refX = 0;       refY = ph;      pivotX = 0;   pivotY = 1;   signX =  1; signY = -1; break;
			case SuiAnchor.BottomCenter: refX = pw*0.5f; refY = ph;      pivotX = 0.5f;pivotY = 1;   signX =  1; signY = -1; break;
			case SuiAnchor.BottomRight:  refX = pw;      refY = ph;      pivotX = 1;   pivotY = 1;   signX = -1; signY = -1; break;
			default:                     refX = 0;       refY = 0;       pivotX = 0;   pivotY = 0;   signX =  1; signY =  1; break;
		}

		// Target top-left in parent-local coords (subtract parentRect origin).
		var topLeftX = targetRect.Left - parentRect.Left;
		var topLeftY = targetRect.Top - parentRect.Top;

		// Reverse: posX = topLeftX + pivotX * w; X = (posX - refX) * signX (since signX in {-1,+1})
		var posX = topLeftX + pivotX * w;
		var posY = topLeftY + pivotY * h;
		var x = (posX - refX) * signX;
		var y = (posY - refY) * signY;

		return (x, y, w, h);
	}

	private static float DefaultWidthFor( SuiElement el ) => el.Type switch
	{
		SuiElementType.Text => 200,
		SuiElementType.Button => 120,
		SuiElementType.Image => 100,
		SuiElementType.ItemIcon => 64,
		SuiElementType.InventorySlot => 64,
		_ => 100,
	};

	private static float DefaultHeightFor( SuiElement el ) => el.Type switch
	{
		SuiElementType.Text => 32,
		SuiElementType.Button => 36,
		SuiElementType.Image => 100,
		SuiElementType.ItemIcon => 64,
		SuiElementType.InventorySlot => 64,
		_ => 32,
	};
}
