namespace SboxUiDesigner.Runtime;

/// <summary>
/// The data origin of a <see cref="SuiBinding"/>. V1.5 supports a single source
/// kind — a document <see cref="SuiVariable"/>, referenced by its stable GUID.
/// Cross-document refs / scene globals are reached by writing to the wrapper's
/// public <c>[Property]</c> from gameplay code (the wrapper auto-pushes into
/// the view via SyncFieldsTo).
/// </summary>
public sealed class SuiBindingSource
{
	/// <summary>Id of the source <see cref="SuiVariable"/> on the same document.</summary>
	public string VariableId { get; set; }

	public SuiBindingSource Clone() => new() { VariableId = VariableId };
}
