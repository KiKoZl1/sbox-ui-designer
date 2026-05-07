using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// One reversible mutation against a <see cref="SuiDocument"/>. The controller
/// applies a command via <see cref="Apply"/> and stores it on the undo stack;
/// <see cref="Undo"/> reverses it.
///
/// Commands must be self-contained: capture every value they need to undo their
/// effect at construction time. They must not hold references to UI widgets.
/// </summary>
public interface ISuiCommand
{
	/// <summary>Short user-facing description shown in Undo/Redo menu items.</summary>
	string Description { get; }

	void Apply( SuiDocument doc );
	void Undo( SuiDocument doc );
}
