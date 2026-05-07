using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Delete an element and its descendants. Stores the removed elements so undo
/// reinstates the entire subtree in original order.
/// </summary>
public sealed class SuiDeleteElementCommand : ISuiCommand
{
	private readonly string _elementId;

	private string _parentId;
	private int _siblingIndex;
	private List<SuiElement> _removedSubtree;

	public string Description => "Delete element";

	public SuiDeleteElementCommand( string elementId )
	{
		_elementId = elementId;
	}

	public void Apply( SuiDocument doc )
	{
		if ( doc == null ) return;
		var target = doc.GetElement( _elementId );
		if ( target == null || string.IsNullOrEmpty( target.ParentId ) )
			return; // cannot delete root or unknown

		_parentId = target.ParentId;

		// Capture the subtree (target + descendants) and the sibling index.
		var byId = new Dictionary<string, SuiElement>();
		foreach ( var el in doc.Elements )
			if ( !string.IsNullOrEmpty( el.Id ) ) byId[el.Id] = el;

		_removedSubtree = new List<SuiElement>();
		CollectSubtree( target, byId, _removedSubtree );

		var parent = doc.GetElement( _parentId );
		_siblingIndex = parent != null ? parent.Children.IndexOf( _elementId ) : -1;

		// Remove from parent's children list and the document's flat element list.
		parent?.Children.Remove( _elementId );
		foreach ( var el in _removedSubtree ) doc.Elements.Remove( el );
	}

	public void Undo( SuiDocument doc )
	{
		if ( doc == null || _removedSubtree == null ) return;

		// Re-add elements in original order.
		foreach ( var el in _removedSubtree )
			doc.Elements.Add( el );

		var parent = doc.GetElement( _parentId );
		if ( parent != null )
		{
			var idx = _siblingIndex < 0 ? parent.Children.Count : _siblingIndex;
			parent.Children.Insert( idx, _elementId );
		}
	}

	private static void CollectSubtree( SuiElement root, Dictionary<string, SuiElement> byId, List<SuiElement> output )
	{
		output.Add( root );
		foreach ( var childId in root.Children )
		{
			if ( byId.TryGetValue( childId, out var child ) )
				CollectSubtree( child, byId, output );
		}
	}
}
