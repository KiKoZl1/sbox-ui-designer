using System.Collections.Generic;
using System.Linq;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Distribute multiple absolute-mode elements with equal spacing along a single
/// axis. With N=3 elements: leftmost and rightmost stay put; the middle ones
/// are repositioned so the gaps between consecutive elements are equal.
///
/// V1 requires N >= 3 (for N=2 there's nothing to distribute). All elements
/// must share a parent (controller filters this).
/// </summary>
public sealed class SuiDistributeElementsCommand : ISuiCommand
{
	public enum Axis
	{
		Horizontal,
		Vertical,
	}

	private readonly List<string> _ids;
	private readonly Axis _axis;
	private readonly List<(string Id, float OldX, float OldY)> _saved = new();

	public string Description => $"Distribute {_axis}";

	public SuiDistributeElementsCommand( IEnumerable<string> elementIds, Axis axis )
	{
		_ids = elementIds.ToList();
		_axis = axis;
	}

	public void Apply( SuiDocument doc )
	{
		if ( doc == null || _ids.Count < 3 ) return;
		_saved.Clear();

		var elements = _ids.Select( doc.GetElement )
			.Where( e => e?.Layout != null && e.Layout.Mode == SuiLayoutMode.Absolute )
			.ToList();
		if ( elements.Count < 3 ) return;

		// Save originals.
		foreach ( var el in elements )
			_saved.Add( (el.Id, el.Layout.X, el.Layout.Y) );

		if ( _axis == Axis.Horizontal )
		{
			elements.Sort( ( a, b ) => a.Layout.X.CompareTo( b.Layout.X ) );
			float minStart = elements.First().Layout.X;
			float maxEnd = elements.Last().Layout.X + elements.Last().Layout.Width;
			float totalWidth = elements.Sum( e => e.Layout.Width );
			float gap = (maxEnd - minStart - totalWidth) / (elements.Count - 1);

			float cursor = minStart;
			foreach ( var el in elements )
			{
				el.Layout.X = cursor;
				cursor += el.Layout.Width + gap;
			}
		}
		else
		{
			elements.Sort( ( a, b ) => a.Layout.Y.CompareTo( b.Layout.Y ) );
			float minStart = elements.First().Layout.Y;
			float maxEnd = elements.Last().Layout.Y + elements.Last().Layout.Height;
			float totalHeight = elements.Sum( e => e.Layout.Height );
			float gap = (maxEnd - minStart - totalHeight) / (elements.Count - 1);

			float cursor = minStart;
			foreach ( var el in elements )
			{
				el.Layout.Y = cursor;
				cursor += el.Layout.Height + gap;
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
