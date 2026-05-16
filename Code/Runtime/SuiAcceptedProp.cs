using System;
using System.Text.Json.Nodes;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// One typed parameter a <c>.sui</c> document exposes to parents that embed it
/// via <see cref="SuiElementType.SuiReference"/> (PRD 19 § 3.1). The pair
/// (<see cref="PropId"/>, <see cref="Type"/>) is the external contract; the
/// internal data flow that bindings inside the document read from is a
/// matching Variable with <c>Source.Kind = FromAcceptedProp</c> (PRD 19 § 3.2).
///
/// <para>Refactor safety: parents address props by <see cref="PropId"/>, never
/// by <see cref="Name"/>. Renaming <c>Hp</c> → <c>Health</c> updates the
/// label everywhere without invalidating any reference.</para>
/// </summary>
public sealed class SuiAcceptedProp
{
	/// <summary>Stable GUID <c>prop_XXXXXXXX</c>. Never changes on rename.</summary>
	public string PropId { get; set; }

	/// <summary>Display label. Must be a valid C# identifier; unique within the doc's AcceptedProps.</summary>
	public string Name { get; set; }

	/// <summary>TypeRef string — same closed set as <see cref="SuiVariable.Type"/> (PRD 18 § 3.3).</summary>
	public string Type { get; set; }

	/// <summary>Compile-time default emitted as the property initializer when the parent omits a value.</summary>
	public JsonNode Default { get; set; }

	/// <summary>When true the validator fails compile if the parent omits this prop.</summary>
	public bool Required { get; set; }

	/// <summary>Surfaced as the tooltip in the parent's SuiReference props editor.</summary>
	public string Description { get; set; }

	/// <summary>Optional category label for grouping in the parent's editor.</summary>
	public string Group { get; set; }

	public static string NewPropId()
		=> "prop_" + Guid.NewGuid().ToString( "N" ).Substring( 0, 8 );

	public SuiAcceptedProp Clone() => new()
	{
		PropId = PropId,
		Name = Name,
		Type = Type,
		Default = Default?.DeepClone(),
		Required = Required,
		Description = Description,
		Group = Group,
	};
}
