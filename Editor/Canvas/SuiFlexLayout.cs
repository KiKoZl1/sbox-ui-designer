using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Canvas;

/// <summary>
/// Flex layout pass — Yoga-subset implementation.
///
/// Supported (Phase 3 MVP):
/// - <c>flex-direction</c>: Row, Column, RowReverse, ColumnReverse
/// - <c>justify-content</c>: FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround, SpaceEvenly
/// - <c>align-items</c>: FlexStart, Center, FlexEnd, Stretch
/// - <c>gap</c>
/// - <c>margin</c> / <c>padding</c>
///
/// NOT supported (V2):
/// - <c>flex-grow</c>, <c>flex-shrink</c>, <c>flex-basis</c> (children always use intrinsic size)
/// - <c>flex-wrap</c> (single line only)
/// - <c>align-self</c> (inherits parent's align-items)
/// - Baseline align (collapses to FlexStart)
///
/// Children of a flex container ignore their own <c>Anchor</c> / <c>Pivot</c> /
/// <c>X</c> / <c>Y</c> — position is decided by the container. Width/Height are
/// honored as the child's intrinsic size; if 0, defaults from the layout solver
/// are used (e.g. Text → 200x32).
///
/// Algorithm — single pass per flex container, called from
/// <see cref="SuiLayoutSolver"/> after the parent's rect is known:
///
/// 1. Compute available main/cross size (parent rect minus padding).
/// 2. For each child, compute its intrinsic main/cross size.
/// 3. Sum main sizes + gaps to get used main size.
/// 4. Distribute remaining space along main axis per justify-content.
/// 5. Position each child along main; size + position along cross per align-items.
/// </summary>
public static class SuiFlexLayout
{
	public readonly struct ChildLayout
	{
		public readonly string Id;
		public readonly Rect Rect;

		public ChildLayout( string id, Rect rect )
		{
			Id = id;
			Rect = rect;
		}
	}

	/// <summary>
	/// Solve layout for one flex container's children. Returns a list of
	/// (childId, rect) pairs in the same coord space as <paramref name="parentRect"/>.
	/// </summary>
	public static List<ChildLayout> Solve(
		SuiElement parent,
		Rect parentRect,
		IReadOnlyDictionary<string, SuiElement> byId,
		Func<SuiElement, Vector2> intrinsicSize )
	{
		var result = new List<ChildLayout>();
		var l = parent.Layout;
		if ( l == null || l.Mode != SuiLayoutMode.Flex || parent.Children == null || parent.Children.Count == 0 )
			return result;

		// Apply padding to get the inner content rect.
		var pad = l.Padding ?? new SuiSpacing();
		var inner = new Rect(
			parentRect.Left + pad.Left,
			parentRect.Top + pad.Top,
			MathF.Max( 0, parentRect.Width - pad.Left - pad.Right ),
			MathF.Max( 0, parentRect.Height - pad.Top - pad.Bottom )
		);

		var dir = l.FlexDirection;
		var mainAxisHorizontal = dir == SuiFlexDirection.Row || dir == SuiFlexDirection.RowReverse;
		var reversed = dir == SuiFlexDirection.RowReverse || dir == SuiFlexDirection.ColumnReverse;

		// Resolve children with intrinsic sizes + per-child margins.
		var visibleChildren = new List<SuiElement>();
		var sizes = new List<Vector2>();
		var margins = new List<SuiSpacing>();
		foreach ( var childId in parent.Children )
		{
			if ( !byId.TryGetValue( childId, out var child ) ) continue;
			if ( child.Style?.Visibility == SuiVisibility.Collapsed ) continue;
			if ( child.Flags?.HiddenInDesigner == true ) continue;
			visibleChildren.Add( child );
			sizes.Add( intrinsicSize( child ) );
			margins.Add( child.Layout?.Margin ?? new SuiSpacing() );
		}

		if ( reversed ) { visibleChildren.Reverse(); sizes.Reverse(); margins.Reverse(); }

		var n = visibleChildren.Count;
		if ( n == 0 ) return result;

		// Compute total main-axis used size (children sizes + margins + gaps between).
		var gap = MathF.Max( 0, l.Gap );
		float usedMain = 0;
		for ( int i = 0; i < n; i++ )
		{
			var s = sizes[i];
			var m = margins[i];
			var childMain = mainAxisHorizontal ? s.x + m.Left + m.Right : s.y + m.Top + m.Bottom;
			usedMain += childMain;
		}
		usedMain += gap * (n - 1);

		var availMain = mainAxisHorizontal ? inner.Width : inner.Height;
		var freeMain = MathF.Max( 0, availMain - usedMain );

		// Compute initial offset + per-gap spacing per justify-content.
		float offsetMain = 0;
		float perGap = gap;
		switch ( l.JustifyContent )
		{
			case SuiJustifyContent.FlexStart:
				offsetMain = 0;
				break;
			case SuiJustifyContent.Center:
				offsetMain = freeMain * 0.5f;
				break;
			case SuiJustifyContent.FlexEnd:
				offsetMain = freeMain;
				break;
			case SuiJustifyContent.SpaceBetween:
				perGap = n > 1 ? gap + freeMain / (n - 1) : 0;
				offsetMain = 0;
				break;
			case SuiJustifyContent.SpaceAround:
			{
				var space = n > 0 ? freeMain / n : 0;
				perGap = gap + space;
				offsetMain = space * 0.5f;
				break;
			}
			case SuiJustifyContent.SpaceEvenly:
			{
				var space = n > 0 ? freeMain / (n + 1) : 0;
				perGap = gap + space;
				offsetMain = space;
				break;
			}
		}

		// Cross axis layout: each child's cross size + position depends on
		// align-items. Stretch fills the inner cross size minus margins.
		var availCross = mainAxisHorizontal ? inner.Height : inner.Width;

		var cursor = mainAxisHorizontal ? inner.Left + offsetMain : inner.Top + offsetMain;
		for ( int i = 0; i < n; i++ )
		{
			var child = visibleChildren[i];
			var s = sizes[i];
			var m = margins[i];

			var marginMainStart = mainAxisHorizontal ? m.Left : m.Top;
			var marginMainEnd   = mainAxisHorizontal ? m.Right : m.Bottom;
			var marginCrossStart= mainAxisHorizontal ? m.Top : m.Left;
			var marginCrossEnd  = mainAxisHorizontal ? m.Bottom : m.Right;

			cursor += marginMainStart;

			float crossSize, crossOffset;
			switch ( l.AlignItems )
			{
				case SuiAlignItems.FlexEnd:
					crossSize = mainAxisHorizontal ? s.y : s.x;
					crossOffset = availCross - crossSize - marginCrossEnd;
					break;
				case SuiAlignItems.Center:
					crossSize = mainAxisHorizontal ? s.y : s.x;
					crossOffset = (availCross - crossSize - marginCrossStart - marginCrossEnd) * 0.5f + marginCrossStart;
					break;
				case SuiAlignItems.Stretch:
					// Mirror CSS: align-items: stretch only stretches items whose
					// cross-axis size is `auto`. Items with an explicit cross-axis
					// size (Layout.Width > 0 or Layout.Height > 0 depending on
					// main axis) KEEP their size — otherwise canvas would render
					// 96×96 slots as 96×N where N = parent height, breaking
					// parity with the actual runtime CSS render.
					var hasExplicitCross = mainAxisHorizontal
						? (child.Layout?.Height ?? 0f) > 0f
						: (child.Layout?.Width ?? 0f) > 0f;
					if ( hasExplicitCross )
					{
						crossSize = mainAxisHorizontal ? s.y : s.x;
						crossOffset = marginCrossStart;
					}
					else
					{
						crossSize = MathF.Max( 0, availCross - marginCrossStart - marginCrossEnd );
						crossOffset = marginCrossStart;
					}
					break;
				case SuiAlignItems.Baseline: // not really baseline; treat as flex-start
				case SuiAlignItems.FlexStart:
				default:
					crossSize = mainAxisHorizontal ? s.y : s.x;
					crossOffset = marginCrossStart;
					break;
			}

			var childMainSize = mainAxisHorizontal ? s.x : s.y;
			Rect childRect;
			if ( mainAxisHorizontal )
			{
				childRect = new Rect( cursor, inner.Top + crossOffset, childMainSize, crossSize );
			}
			else
			{
				childRect = new Rect( inner.Left + crossOffset, cursor, crossSize, childMainSize );
			}

			result.Add( new ChildLayout( child.Id, childRect ) );

			cursor += childMainSize + marginMainEnd;
			if ( i < n - 1 ) cursor += perGap;
		}

		return result;
	}

	/// <summary>
	/// Grid layout for InventoryGrid / Grid. Uses parent's <c>Props.Columns</c>,
	/// <c>CellWidth</c>, <c>CellHeight</c>, <c>GridGap</c> to tile children in a
	/// regular grid (col-major flow). Children are positioned at fixed cell
	/// sizes regardless of their own Width/Height — semantically the slot grid
	/// owns the layout. Honors parent padding.
	/// </summary>
	public static List<ChildLayout> SolveGrid(
		SuiElement parent,
		Rect parentRect,
		IReadOnlyDictionary<string, SuiElement> byId )
	{
		var result = new List<ChildLayout>();
		var p = parent.Props ?? new SuiElementProps();
		var cols = Math.Max( 1, p.Columns );
		var cellW = p.CellWidth > 0 ? p.CellWidth : 64;
		var cellH = p.CellHeight > 0 ? p.CellHeight : 64;
		var gap = MathF.Max( 0, p.GridGap );

		var pad = parent.Layout?.Padding ?? new SuiSpacing();
		var originX = parentRect.Left + pad.Left;
		var originY = parentRect.Top + pad.Top;

		int idx = 0;
		foreach ( var childId in parent.Children ?? new List<string>() )
		{
			if ( !byId.TryGetValue( childId, out var child ) ) continue;
			if ( child.Style?.Visibility == SuiVisibility.Collapsed ) continue;
			if ( child.Flags?.HiddenInDesigner == true ) continue;

			var col = idx % cols;
			var row = idx / cols;
			var x = originX + col * (cellW + gap);
			var y = originY + row * (cellH + gap);
			result.Add( new ChildLayout( child.Id, new Rect( x, y, cellW, cellH ) ) );
			idx++;
		}
		return result;
	}
}
