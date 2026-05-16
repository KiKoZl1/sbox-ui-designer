using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Design-time preview values for Variables and AcceptedProps (PRD 19 § 3.6).
/// Runtime ignores this block; only the canvas renderer reads it so the user
/// designs against realistic data.
///
/// <para><b>Resolution order at design time</b> (PRD 19 § 6.2):
/// <c>PreviewData.Variables[id]</c> &gt; <see cref="SuiVariable.Default"/> &gt;
/// type-default. For embedded children, parent's <c>SuiReference.Props</c>
/// always wins over the child's <see cref="AcceptedProps"/>.</para>
/// </summary>
public sealed class SuiPreviewData
{
	/// <summary>Overrides keyed by <see cref="SuiVariable.Id"/>.</summary>
	public Dictionary<string, JsonNode> Variables { get; set; } = new();

	/// <summary>
	/// Standalone-preview values for this doc's AcceptedProps, keyed by
	/// <see cref="SuiAcceptedProp.PropId"/>. Used only when the doc is opened
	/// directly in the Designer; embedded contexts use the parent's Props instead.
	/// </summary>
	public Dictionary<string, JsonNode> AcceptedProps { get; set; } = new();

	public SuiPreviewData Clone()
	{
		var c = new SuiPreviewData();
		foreach ( var kv in Variables ) c.Variables[kv.Key] = kv.Value?.DeepClone();
		foreach ( var kv in AcceptedProps ) c.AcceptedProps[kv.Key] = kv.Value?.DeepClone();
		return c;
	}
}
