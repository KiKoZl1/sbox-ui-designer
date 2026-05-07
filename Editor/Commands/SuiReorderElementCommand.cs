using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Move an element up or down within its parent's child list. Delta is the
/// signed offset to apply to the child's index (-1 = up, +1 = down).
/// Out-of-range moves are no-ops.
/// </summary>
public sealed class SuiReorderElementCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly int _delta;
	private bool _applied;

	public string Description => _delta < 0 ? "Move up" : "Move down";

	public SuiReorderElementCommand( string elementId, int delta )
	{
		_elementId = elementId;
		_delta = delta;
	}

	public void Apply( SuiDocument doc )
	{
		_applied = TrySwap( doc, _delta );
	}

	public void Undo( SuiDocument doc )
	{
		if ( !_applied ) return;
		TrySwap( doc, -_delta );
	}

	private bool TrySwap( SuiDocument doc, int delta )
	{
		if ( doc == null ) return false;
		var el = doc.GetElement( _elementId );
		if ( el == null || string.IsNullOrEmpty( el.ParentId ) ) return false;
		var parent = doc.GetElement( el.ParentId );
		if ( parent == null ) return false;

		var idx = parent.Children.IndexOf( _elementId );
		if ( idx < 0 ) return false;
		var newIdx = idx + delta;
		if ( newIdx < 0 || newIdx >= parent.Children.Count ) return false;

		parent.Children.RemoveAt( idx );
		parent.Children.Insert( newIdx, _elementId );
		return true;
	}
}
