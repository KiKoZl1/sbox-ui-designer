using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Add a new element under a parent. Stores the freshly-created element so
/// undo can pop it back out cleanly without losing the user's edits.
/// </summary>
public sealed class SuiAddElementCommand : ISuiCommand
{
	private readonly SuiElement _element;
	private readonly string _parentId;
	private int _insertionIndex = -1;

	public string Description => $"Add {_element?.Type} '{_element?.Name}'";

	public SuiAddElementCommand( SuiElement element, string parentId )
	{
		_element = element;
		_parentId = parentId;
	}

	public void Apply( SuiDocument doc )
	{
		if ( doc == null || _element == null ) return;

		var parent = doc.GetElement( _parentId );
		if ( parent == null ) return;

		_element.ParentId = _parentId;
		doc.Elements.Add( _element );

		if ( _insertionIndex < 0 || _insertionIndex > parent.Children.Count )
			_insertionIndex = parent.Children.Count;
		parent.Children.Insert( _insertionIndex, _element.Id );
	}

	public void Undo( SuiDocument doc )
	{
		if ( doc == null || _element == null ) return;

		doc.Elements.Remove( _element );
		var parent = doc.GetElement( _parentId );
		parent?.Children.Remove( _element.Id );
	}
}
