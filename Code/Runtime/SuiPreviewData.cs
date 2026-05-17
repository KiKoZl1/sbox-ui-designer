using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Design-time preview values for Variables (PRD 19 § 3.6).
/// Runtime ignores this block; only the canvas renderer reads it so the user
/// designs against realistic data.
///
/// <para><b>Resolution order at design time</b> (PRD 19 § 6.2):
/// <c>PreviewData.Variables[id]</c> &gt; <see cref="SuiVariable.Default"/> &gt;
/// type-default. For embedded children, parent's <c>SuiReference.Props</c>
/// always wins over the child's own preview overrides.</para>
/// </summary>
public sealed class SuiPreviewData
{
	/// <summary>Overrides keyed by <see cref="SuiVariable.Id"/>.</summary>
	public Dictionary<string, JsonNode> Variables { get; set; } = new();

	public SuiPreviewData Clone()
	{
		var c = new SuiPreviewData();
		foreach ( var kv in Variables ) c.Variables[kv.Key] = kv.Value?.DeepClone();
		return c;
	}
}
