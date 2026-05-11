using Sandbox;
using SboxUiDesigner.EditorUi.Canvas;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Change an element's anchor while preserving its visual rect (UMG / UI Builder
/// style). Naively assigning <c>Layout.Anchor = newValue</c> would make the
/// element jump because the same X/Y/W/H values mean different things under
/// different anchors. Instead we:
///
///   1. Solve the document under the OLD anchor → get the element's current rect.
///   2. Set the new anchor.
///   3. Re-derive X/Y/W/H using <see cref="SuiLayoutSolver.RectToLayoutValues"/>
///      with the NEW anchor + the parent's rect, so the element ends up at the
///      same pixel position but with values appropriate for the new reference.
///
/// Stretch variants (Stretch / StretchHorizontal / StretchVertical) skip the
/// recompute — those modes ignore X/Y/W/H or treat them as margins, so the
/// reverse pass doesn't apply cleanly. Future polish can handle those if needed.
/// </summary>
public sealed class SuiSetAnchorCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly SuiAnchor _newAnchor;

	private SuiAnchor _oldAnchor;
	private float _oldX, _oldY, _oldW, _oldH;

	public string Description => $"Set anchor to {_newAnchor}";

	public SuiSetAnchorCommand( string elementId, SuiAnchor newAnchor )
	{
		_elementId = elementId;
		_newAnchor = newAnchor;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el?.Layout == null ) return;

		_oldAnchor = el.Layout.Anchor;
		_oldX = el.Layout.X;
		_oldY = el.Layout.Y;
		_oldW = el.Layout.Width;
		_oldH = el.Layout.Height;

		// Solve under the OLD anchor first to capture the current visual rect.
		var solver = new SuiLayoutSolver( new Vector2( 1920, 1080 ) );
		solver.Solve( doc );

		if ( !solver.TryGetRect( _elementId, out var currentRect ) ||
			 string.IsNullOrEmpty( el.ParentId ) ||
			 !solver.TryGetRect( el.ParentId, out var parentRect ) )
		{
			el.Layout.Anchor = _newAnchor;
			return;
		}

		// Inverse pass — derive X/Y/W/H values for the new anchor that
		// reproduce the same on-screen rect. RectToLayoutValues now handles
		// stretch variants too (interprets X/Y/W/H as margins).
		var (x, y, w, h) = SuiLayoutSolver.RectToLayoutValues( currentRect, _newAnchor, parentRect );

		el.Layout.Anchor = _newAnchor;
		el.Layout.X = x;
		el.Layout.Y = y;
		el.Layout.Width = w;
		el.Layout.Height = h;
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el?.Layout == null ) return;

		el.Layout.Anchor = _oldAnchor;
		el.Layout.X = _oldX;
		el.Layout.Y = _oldY;
		el.Layout.Width = _oldW;
		el.Layout.Height = _oldH;
	}

	private static bool IsStretch( SuiAnchor a )
		=> a == SuiAnchor.Stretch || a == SuiAnchor.StretchHorizontal || a == SuiAnchor.StretchVertical;
}
