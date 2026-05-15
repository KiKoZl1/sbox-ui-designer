namespace SboxUiDesigner.Runtime;

/// <summary>
/// The data origin of a <see cref="SuiBinding"/> (PRD 18 § 4.3). V1.5 supports a
/// single source kind — a document <see cref="SuiVariable"/>, referenced by its
/// stable GUID. Cross-document refs / scene globals are reached by first exposing
/// them as a Variable (FromComponent / FromActionGraph source).
/// </summary>
public sealed class SuiBindingSource
{
	/// <summary>Id of the source <see cref="SuiVariable"/> on the same document.</summary>
	public string VariableId { get; set; }

	public SuiBindingSource Clone() => new() { VariableId = VariableId };
}
