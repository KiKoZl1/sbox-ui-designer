using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Apply edited fields to an existing <see cref="SuiAcceptedProp"/>. PropId is
/// preserved — references from parents continue to resolve.
/// </summary>
public sealed class SuiEditAcceptedPropCommand : ISuiCommand
{
	private readonly SuiAcceptedProp _target;
	private readonly SuiAcceptedProp _before;
	private readonly SuiAcceptedProp _after;

	public string Description => $"Edit Accepted Prop '{_after?.Name}'";

	public SuiEditAcceptedPropCommand( SuiAcceptedProp target, SuiAcceptedProp editedSnapshot )
	{
		_target = target;
		_before = target?.Clone();
		_after = editedSnapshot?.Clone();
	}

	public void Apply( SuiDocument doc ) => CopyInto( _after, _target );
	public void Undo( SuiDocument doc ) => CopyInto( _before, _target );

	private static void CopyInto( SuiAcceptedProp src, SuiAcceptedProp dst )
	{
		if ( src == null || dst == null ) return;
		dst.Name = src.Name;
		dst.Type = src.Type;
		dst.Default = src.Default?.DeepClone();
		dst.Required = src.Required;
		dst.Description = src.Description;
		dst.Group = src.Group;
		// PropId intentionally left untouched.
	}
}
