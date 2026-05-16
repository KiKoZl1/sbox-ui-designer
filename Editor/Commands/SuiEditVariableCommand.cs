using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Apply edited fields to an existing <see cref="SuiVariable"/> (from the edit
/// dialog). Captures a before/after snapshot of every editable field so undo
/// restores cleanly. The Variable's <see cref="SuiVariable.Id"/> is never touched
/// — bindings reference it and must keep working (PRD 18 § 4 forward-compat).
/// </summary>
public sealed class SuiEditVariableCommand : ISuiCommand
{
	private readonly SuiVariable _target;
	private readonly SuiVariable _before;
	private readonly SuiVariable _after;

	public string Description => $"Edit Variable '{_after?.Name}'";

	/// <param name="target">The live Variable in the document.</param>
	/// <param name="editedSnapshot">A detached Variable carrying the new field values.</param>
	public SuiEditVariableCommand( SuiVariable target, SuiVariable editedSnapshot )
	{
		_target = target;
		_before = target?.Clone();
		_after = editedSnapshot?.Clone();
	}

	public void Apply( SuiDocument doc ) => CopyInto( _after, _target );

	public void Undo( SuiDocument doc ) => CopyInto( _before, _target );

	private static void CopyInto( SuiVariable src, SuiVariable dst )
	{
		if ( src == null || dst == null ) return;
		dst.Name = src.Name;
		dst.Type = src.Type;
		dst.Default = src.Default?.DeepClone();
		dst.Source = src.Source?.Clone() ?? new();
		dst.Description = src.Description;
		dst.IsAdvanced = src.IsAdvanced;
		dst.IsPublic = src.IsPublic;
		dst.Group = src.Group;
		dst.ResourceType = src.ResourceType;
		// Id intentionally left untouched — it is the stable identity.
	}
}
