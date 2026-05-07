using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Reparent an element to a new parent at a specific child index. Refuses to
/// move root and refuses to create cycles (new parent cannot be a descendant
/// of the moved element). Both checks fail silently — the validator surfaces
/// any inconsistency via the standard validate flow.
/// </summary>
public sealed class SuiReparentElementCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly string _newParentId;
	private readonly int _newInsertIndex;

	private bool _applied;
	private string _oldParentId;
	private int _oldIndex;

	public string Description => "Reparent element";

	public SuiReparentElementCommand( string elementId, string newParentId, int insertIndex )
	{
		_elementId = elementId;
		_newParentId = newParentId;
		_newInsertIndex = insertIndex;
	}

	public void Apply( SuiDocument doc )
	{
		if ( doc == null ) return;
		var el = doc.GetElement( _elementId );
		if ( el == null || string.IsNullOrEmpty( el.ParentId ) ) return; // refuse root
		if ( _elementId == _newParentId ) return; // refuse self-parent

		var oldParent = doc.GetElement( el.ParentId );
		var newParent = doc.GetElement( _newParentId );
		if ( oldParent == null || newParent == null ) return;

		// Refuse cycle: new parent cannot be a descendant of the moved element.
		if ( IsDescendantOf( newParent.Id, el.Id, doc ) ) return;

		_oldParentId = oldParent.Id;
		_oldIndex = oldParent.Children.IndexOf( _elementId );

		oldParent.Children.Remove( _elementId );

		var idx = _newInsertIndex;
		if ( idx < 0 ) idx = 0;
		if ( idx > newParent.Children.Count ) idx = newParent.Children.Count;
		newParent.Children.Insert( idx, _elementId );

		el.ParentId = _newParentId;
		_applied = true;
	}

	public void Undo( SuiDocument doc )
	{
		if ( !_applied || doc == null ) return;
		var el = doc.GetElement( _elementId );
		if ( el == null ) return;
		var newParent = doc.GetElement( _newParentId );
		var oldParent = doc.GetElement( _oldParentId );
		if ( newParent == null || oldParent == null ) return;

		newParent.Children.Remove( _elementId );
		var idx = _oldIndex;
		if ( idx < 0 ) idx = 0;
		if ( idx > oldParent.Children.Count ) idx = oldParent.Children.Count;
		oldParent.Children.Insert( idx, _elementId );

		el.ParentId = _oldParentId;
	}

	/// <summary>Returns true if <paramref name="candidateId"/> is the same as or a descendant of <paramref name="ancestorId"/>.</summary>
	private static bool IsDescendantOf( string candidateId, string ancestorId, SuiDocument doc )
	{
		var safety = 1024;
		var currentId = candidateId;
		while ( !string.IsNullOrEmpty( currentId ) && --safety > 0 )
		{
			if ( currentId == ancestorId ) return true;
			var current = doc.GetElement( currentId );
			if ( current == null ) return false;
			currentId = current.ParentId;
		}
		return false;
	}
}
