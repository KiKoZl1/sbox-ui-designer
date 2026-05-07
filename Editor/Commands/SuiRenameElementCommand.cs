using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Rename an element (changes <see cref="SuiElement.Name"/> only — the stable
/// <see cref="SuiElement.Id"/> never changes on rename, by design).
/// </summary>
public sealed class SuiRenameElementCommand : ISuiCommand
{
	private readonly string _elementId;
	private readonly string _newName;
	private string _oldName;

	public string Description => $"Rename to '{_newName}'";

	public SuiRenameElementCommand( string elementId, string newName )
	{
		_elementId = elementId;
		_newName = newName;
	}

	public void Apply( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;
		_oldName = el.Name;
		el.Name = _newName;
	}

	public void Undo( SuiDocument doc )
	{
		var el = doc?.GetElement( _elementId );
		if ( el == null ) return;
		el.Name = _oldName;
	}
}
