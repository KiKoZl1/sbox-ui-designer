using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>Add a new <see cref="SuiVariable"/> to the document (PRD 18 § 3.7).</summary>
public sealed class SuiAddVariableCommand : ISuiCommand
{
	private readonly SuiVariable _variable;

	public string Description => $"Add Variable '{_variable?.Name}'";

	public SuiAddVariableCommand( SuiVariable variable ) => _variable = variable;

	public void Apply( SuiDocument doc )
	{
		if ( doc == null || _variable == null ) return;
		doc.Variables ??= new();
		if ( !doc.Variables.Contains( _variable ) )
			doc.Variables.Add( _variable );
	}

	public void Undo( SuiDocument doc ) => doc?.Variables?.Remove( _variable );
}
