using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Data block attached to an element of <see cref="SuiElementType.SuiReference"/>
/// (PRD 19 § 3.3). Points at another <c>.sui</c> document by stable GUID and
/// supplies values for the source doc's AcceptedProps (literal or bound).
///
/// <para>Embedded subtree is laid out within the host element's rectangle; the
/// host's <see cref="SuiElement.Layout"/> / <see cref="SuiElement.Style"/> /
/// <see cref="SuiElement.Flags"/> blocks behave like a normal container.</para>
/// </summary>
public sealed class SuiReferenceData
{
	/// <summary>DocumentId of the referenced <c>.sui</c> (resolved via Asset Registry).</summary>
	public string SourceGuid { get; set; }

	/// <summary>
	/// V1.5-M2-K: per-prop value map keyed by <see cref="SuiVariable.Id"/>
	/// (of the child doc's public Variables — those with IsPublic=true).
	/// Value is
	/// either a JSON literal or a binding object (<c>{ "$bind": { ... } }</c> shape,
	/// PRD 19 § 3.4). Missing keys fall back to the child's <c>Default</c>.
	/// </summary>
	public Dictionary<string, JsonNode> Props { get; set; } = new();

	/// <summary>When non-null, the reference iterates a list Variable (PRD 19 § 3.5).</summary>
	public SuiForEachData ForEach { get; set; }

	public SuiReferenceData Clone()
	{
		var c = new SuiReferenceData
		{
			SourceGuid = SourceGuid,
			ForEach = ForEach?.Clone(),
		};
		foreach ( var kv in Props ) c.Props[kv.Key] = kv.Value?.DeepClone();
		return c;
	}
}
