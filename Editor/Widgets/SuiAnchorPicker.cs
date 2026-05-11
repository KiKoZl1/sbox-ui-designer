using System;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Visual anchor preset picker — 3×3 grid representing the 9 standard anchor
/// positions (TopLeft / TopCenter / TopRight / MiddleLeft / MiddleCenter /
/// MiddleRight / BottomLeft / BottomCenter / BottomRight). Replaces the
/// dropdown for the common case; stretch variants stay in the dropdown.
///
/// Click a cell → fire <see cref="AnchorSelected"/>. Currently selected cell
/// highlighted in accent blue.
/// </summary>
public sealed class SuiAnchorPicker : Widget
{
	private SuiAnchor _current;
	private Rect _hoverRect;

	public Action<SuiAnchor> AnchorSelected;

	public SuiAnchorPicker( Widget parent ) : base( parent )
	{
		FixedSize = new Vector2( 78, 78 );
		MouseTracking = true;
		Cursor = CursorShape.Finger;
	}

	public void SetCurrent( SuiAnchor a )
	{
		_current = a;
		Update();
	}

	protected override void OnPaint()
	{
		var rect = LocalRect;
		var cellSize = (rect.Width - 8) / 3f;

		var darkSquare = new Color( 70 / 255f, 72 / 255f, 78 / 255f );
		var blueSquare = new Color( 0f, 0.55f, 1.0f );
		var selectedBg = new Color( 0f, 0.55f, 1.0f, 0.18f );
		var hoverBg = new Color( 1f, 1f, 1f, 0.06f );
		var selectedOutline = new Color( 0f, 0.55f, 1.0f );

		for ( int cellRow = 0; cellRow < 3; cellRow++ )
		{
			for ( int cellCol = 0; cellCol < 3; cellCol++ )
			{
				var cellRect = CellRect( cellRow, cellCol, cellSize );
				var anchor = AnchorAt( cellRow, cellCol );
				var isCurrent = _current == anchor;
				var isHover = _hoverRect == cellRect;

				// Cell background — selected cell gets a soft blue tint +
				// a 1px blue outline; hover gets a faint white tint.
				if ( isCurrent )
				{
					Paint.SetBrush( selectedBg );
					Paint.SetPen( selectedOutline, 1f );
					Paint.DrawRect( cellRect, 3f );
				}
				else if ( isHover )
				{
					Paint.SetBrushAndPen( hoverBg );
					Paint.DrawRect( cellRect, 3f );
				}

				// Mini 3×3 grid inside this cell. The mini-square at
				// position (cellRow, cellCol) is blue — its position within
				// the mini grid mirrors the cell's position in the outer
				// grid, so the icon visually says "this is the X anchor".
				const float pad = 4f;
				const float gap = 1f;
				var miniSize = (cellRect.Width - pad * 2 - gap * 2) / 3f;

				for ( int mr = 0; mr < 3; mr++ )
				{
					for ( int mc = 0; mc < 3; mc++ )
					{
						var miniRect = new Rect(
							cellRect.Left + pad + mc * (miniSize + gap),
							cellRect.Top + pad + mr * (miniSize + gap),
							miniSize, miniSize );

						bool isAnchorIndicator = (mr == cellRow && mc == cellCol);
						Paint.SetBrush( isAnchorIndicator ? blueSquare : darkSquare );
						Paint.ClearPen();
						Paint.DrawRect( miniRect, 1f );
					}
				}
			}
		}
	}

	private Rect CellRect( int row, int col, float cellSize )
	{
		var x = LocalRect.Left + 2 + col * (cellSize + 2);
		var y = LocalRect.Top + 2 + row * (cellSize + 2);
		return new Rect( x, y, cellSize, cellSize );
	}

	private static SuiAnchor AnchorAt( int row, int col )
	{
		return (row, col) switch
		{
			(0, 0) => SuiAnchor.TopLeft,
			(0, 1) => SuiAnchor.TopCenter,
			(0, 2) => SuiAnchor.TopRight,
			(1, 0) => SuiAnchor.MiddleLeft,
			(1, 1) => SuiAnchor.MiddleCenter,
			(1, 2) => SuiAnchor.MiddleRight,
			(2, 0) => SuiAnchor.BottomLeft,
			(2, 1) => SuiAnchor.BottomCenter,
			(2, 2) => SuiAnchor.BottomRight,
			_ => SuiAnchor.TopLeft,
		};
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		var cellSize = (LocalRect.Width - 8) / 3f;
		Rect newHover = default;
		for ( int row = 0; row < 3; row++ )
		{
			for ( int col = 0; col < 3; col++ )
			{
				var cellRect = CellRect( row, col, cellSize );
				if ( PointInside( cellRect, e.LocalPosition ) ) { newHover = cellRect; break; }
			}
		}
		if ( newHover != _hoverRect )
		{
			_hoverRect = newHover;
			Update();
		}
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();
		_hoverRect = default;
		Update();
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( !e.LeftMouseButton ) return;
		var cellSize = (LocalRect.Width - 8) / 3f;
		for ( int row = 0; row < 3; row++ )
		{
			for ( int col = 0; col < 3; col++ )
			{
				var cellRect = CellRect( row, col, cellSize );
				if ( PointInside( cellRect, e.LocalPosition ) )
				{
					var picked = AnchorAt( row, col );
					_current = picked;
					AnchorSelected?.Invoke( picked );
					Update();
					return;
				}
			}
		}
	}

	private static bool PointInside( Rect r, Vector2 p )
		=> p.x >= r.Left && p.x <= r.Right && p.y >= r.Top && p.y <= r.Bottom;
}
