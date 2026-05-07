using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Duplicate an element together with its descendants. The clone gets fresh
/// ids throughout the subtree so it does not collide with the original. The
/// duplicate is inserted as a sibling immediately after the source.
///
/// Cannot duplicate the root element (it has no parent).
/// </summary>
public sealed class SuiDuplicateElementCommand : ISuiCommand
{
	private readonly string _sourceElementId;

	private List<SuiElement> _addedElements;
	private string _parentId;
	private int _insertIndex = -1;
	private string _newRootId;

	public string Description => "Duplicate";

	/// <summary>The id of the cloned root element after Apply has run. Useful so the
	/// controller can select the duplicate.</summary>
	public string ResultingElementId => _newRootId;

	public SuiDuplicateElementCommand( string sourceElementId )
	{
		_sourceElementId = sourceElementId;
	}

	public void Apply( SuiDocument doc )
	{
		if ( doc == null ) return;
		var source = doc.GetElement( _sourceElementId );
		if ( source == null || string.IsNullOrEmpty( source.ParentId ) )
			return; // cannot duplicate root or unknown

		_parentId = source.ParentId;
		var parent = doc.GetElement( _parentId );
		if ( parent == null ) return;

		var byId = new Dictionary<string, SuiElement>();
		foreach ( var el in doc.Elements )
			if ( !string.IsNullOrEmpty( el.Id ) ) byId[el.Id] = el;

		_addedElements = new List<SuiElement>();
		var clonedRoot = CloneSubtree( source, byId, _addedElements );
		clonedRoot.ParentId = _parentId;
		_newRootId = clonedRoot.Id;

		var sourceIdx = parent.Children.IndexOf( _sourceElementId );
		_insertIndex = sourceIdx >= 0 ? sourceIdx + 1 : parent.Children.Count;
		parent.Children.Insert( _insertIndex, clonedRoot.Id );

		foreach ( var el in _addedElements ) doc.Elements.Add( el );
	}

	public void Undo( SuiDocument doc )
	{
		if ( doc == null || _addedElements == null ) return;

		var parent = doc.GetElement( _parentId );
		if ( parent != null && _insertIndex >= 0 && _insertIndex < parent.Children.Count )
			parent.Children.RemoveAt( _insertIndex );

		foreach ( var el in _addedElements ) doc.Elements.Remove( el );
		_addedElements = null;
	}

	private static SuiElement CloneSubtree(
		SuiElement source,
		Dictionary<string, SuiElement> byId,
		List<SuiElement> output )
	{
		var clone = source.Clone();
		clone.Id = SuiDocument.NewElementId();
		clone.Children = new List<string>();
		output.Add( clone );

		foreach ( var childId in source.Children )
		{
			if ( !byId.TryGetValue( childId, out var child ) ) continue;
			var clonedChild = CloneSubtree( child, byId, output );
			clonedChild.ParentId = clone.Id;
			clone.Children.Add( clonedChild.Id );
		}

		return clone;
	}
}
