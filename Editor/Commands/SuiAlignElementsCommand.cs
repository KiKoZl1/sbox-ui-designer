using System.Collections.Generic;
using System.Linq;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Align multiple absolute-mode elements to a common edge or center.
///
/// Bounding box is computed from each element's Layout.X/Y/Width/Height
/// in their parent's coordinate space. For V1 this means alignment is only
/// meaningful when all selected elements share the same parent AND same
/// anchor — otherwise the X/Y values aren't comparable. The controller is
/// expected to filter the input set accordingly.
///
/// Single undo entry covers the move of every element.
/// </summary>
public sealed class SuiAlignElementsCommand : ISuiCommand
{
	public enum Mode
	{
		Left,
		HCenter,
		Right,
		Top,
		VCenter,
		Bottom,
	}

	private readonly List<string> _ids;
	private readonly Mode _mode;

	// Per-element undo state, captured during Apply.
	private readonly List<(string Id, float OldX, float OldY)> _saved = new();

	public string Description => $"Align {_mode}";

	public SuiAlignElementsCommand( IEnumerable<string> elementIds, Mode mode )
	{
		_ids = elementIds.ToList();
		_mode = mode;
	}

	public void Apply( SuiDocument doc )
	{
		if ( doc == null || _ids.Count < 2 ) return;
		_saved.Clear();

		var elements = _ids.Select( doc.GetElement ).Where( e => e?.Layout != null && e.Layout.Mode == SuiLayoutMode.Absolute ).ToList();
		if ( elements.Count < 2 ) return;

		// Bounding box — min/max of each element's own X/Y/Right/Bottom in its
		// parent-local coord space (per anchor). We restrict to single-parent
		// upstream so this is well-defined.
		var minX = elements.Min( e => e.Layout.X );
		var maxRight = elements.Max( e => e.Layout.X + e.Layout.Width );
		var minY = elements.Min( e => e.Layout.Y );
		var maxBottom = elements.Max( e => e.Layout.Y + e.Layout.Height );

		float centerX = (minX + maxRight) * 0.5f;
		float centerY = (minY + maxBottom) * 0.5f;

		foreach ( var el in elements )
		{
			_saved.Add( (el.Id, el.Layout.X, el.Layout.Y) );

			switch ( _mode )
			{
				case Mode.Left:    el.Layout.X = minX; break;
				case Mode.HCenter: el.Layout.X = centerX - el.Layout.Width * 0.5f; break;
				case Mode.Right:   el.Layout.X = maxRight - el.Layout.Width; break;
				case Mode.Top:     el.Layout.Y = minY; break;
				case Mode.VCenter: el.Layout.Y = centerY - el.Layout.Height * 0.5f; break;
				case Mode.Bottom:  el.Layout.Y = maxBottom - el.Layout.Height; break;
			}
		}
	}

	public void Undo( SuiDocument doc )
	{
		if ( doc == null ) return;
		foreach ( var s in _saved )
		{
			var el = doc.GetElement( s.Id );
			if ( el?.Layout == null ) continue;
			el.Layout.X = s.OldX;
			el.Layout.Y = s.OldY;
		}
		_saved.Clear();
	}
}
