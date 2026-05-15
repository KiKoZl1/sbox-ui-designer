using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Remove a <see cref="SuiVariable"/> from the document, remembering its list
/// index so undo re-inserts it in place (PRD 18 § 3.7).
/// </summary>
public sealed class SuiDeleteVariableCommand : ISuiCommand
{
	private readonly SuiVariable _variable;
	private int _index = -1;

	public string Description => $"Delete Variable '{_variable?.Name}'";

	public SuiDeleteVariableCommand( SuiVariable variable ) => _variable = variable;

	public void Apply( SuiDocument doc )
	{
		if ( doc?.Variables == null || _variable == null ) return;
		_index = doc.Variables.IndexOf( _variable );
		if ( _index >= 0 ) doc.Variables.RemoveAt( _index );
	}

	public void Undo( SuiDocument doc )
	{
		if ( doc?.Variables == null || _variable == null || _index < 0 ) return;
		var i = _index > doc.Variables.Count ? doc.Variables.Count : _index;
		doc.Variables.Insert( i, _variable );
	}
}
