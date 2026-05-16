namespace SboxUiDesigner.Runtime;

/// <summary>
/// ForEach block on a <see cref="SuiElementType.SuiReference"/> element
/// (PRD 19 § 3.5). When set, the reference repeats once per item in the bound
/// <c>List&lt;T&gt;</c> Variable. Each iteration passes the item (and optionally
/// the index) as AcceptedProp values to the child instance.
/// </summary>
public sealed class SuiForEachData
{
	/// <summary>Id of a <c>List&lt;T&gt;</c> Variable on the host document.</summary>
	public string SourceVariableId { get; set; }

	/// <summary>Child AcceptedProp that receives the current item. Type must match <c>T</c>.</summary>
	public string ItemPropId { get; set; }

	/// <summary>Optional child AcceptedProp that receives the iteration index. Must be <c>int</c>.</summary>
	public string IndexPropId { get; set; }

	public SuiForEachData Clone() => new()
	{
		SourceVariableId = SourceVariableId,
		ItemPropId = ItemPropId,
		IndexPropId = IndexPropId,
	};
}
