using System;
using System.Text.Json.Nodes;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// V1.5-M2-K — DEPRECATED schema POCO kept ONLY so old <c>.sui</c> JSON files
/// authored before AcceptedProps were merged into <see cref="SuiVariable.IsPublic"/>
/// still deserialize. Conversion happens in
/// <see cref="SuiDocument.MigrateAcceptedPropsToPublicVariables"/> on load —
/// after migration runs once and the doc is saved, the <c>AcceptedProps</c>
/// list is empty and this type leaves the file. The next major version of
/// the schema (V3) can drop this file entirely.
///
/// <para>See DEVIATIONS D-005 for the design rationale.</para>
/// </summary>
[Obsolete( "V1.5-M2-K: use SuiVariable.IsPublic instead. This POCO exists only for legacy-schema deserialization." )]
public sealed class SuiAcceptedProp
{
	public string PropId { get; set; }
	public string Name { get; set; }
	public string Type { get; set; }
	public JsonNode Default { get; set; }
	public bool Required { get; set; }
	public string Description { get; set; }
	public string Group { get; set; }

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
