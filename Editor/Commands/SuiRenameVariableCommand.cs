using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Change a <see cref="SuiVariable"/>'s display Name. The Variable's Id is stable
/// and never touched, so existing bindings keep working (PRD 18 § 8.4).
/// </summary>
public sealed class SuiRenameVariableCommand : ISuiCommand
{
	private readonly SuiVariable _variable;
	private readonly string _newName;
	private readonly string _oldName;

	public string Description => $"Rename Variable to '{_newName}'";

	public SuiRenameVariableCommand( SuiVariable variable, string newName )
	{
		_variable = variable;
		_newName = newName;
		_oldName = variable?.Name;
	}

	public void Apply( SuiDocument doc )
	{
		if ( _variable != null ) _variable.Name = _newName;
	}

	public void Undo( SuiDocument doc )
	{
		if ( _variable != null ) _variable.Name = _oldName;
	}
}
