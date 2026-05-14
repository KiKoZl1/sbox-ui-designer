using System;
using System.Text.Json.Nodes;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// A typed, named slot of UI-local state declared on a <see cref="SuiDocument"/>
/// (PRD 18 § 3). The generator emits one <c>[Property]</c> per Variable on the
/// generated PanelComponent class; UI element properties bind to Variables.
///
/// Identity is the <see cref="Id"/> GUID — it never changes on rename, so
/// bindings reference Variables by Id and survive display-name edits.
/// </summary>
public sealed class SuiVariable
{
	/// <summary>Stable GUID <c>var_XXXXXXXX</c>. Never changes on rename.</summary>
	public string Id { get; set; }

	/// <summary>Display label. Must be a valid C# identifier, unique within the document.</summary>
	public string Name { get; set; }

	/// <summary>
	/// TypeRef string from the closed set (PRD 18 § 3.3): primitives (<c>string</c>,
	/// <c>int</c>, <c>float</c>, …), engine types (<c>Color</c>, <c>Vector3</c>, …),
	/// asset refs (<c>Texture</c>, <c>Resource</c>, …), <c>Enum:Full.Type</c>,
	/// <c>Component:Full.Type</c>, or <c>List&lt;T&gt;</c>.
	/// </summary>
	public string Type { get; set; } = "string";

	/// <summary>
	/// Compile-time default value as a raw JSON node — shape must match <see cref="Type"/>.
	/// May be null for reference types.
	/// </summary>
	public JsonNode Default { get; set; }

	/// <summary>Where the runtime value comes from. Defaults to <see cref="SuiVariableSourceKind.Manual"/>.</summary>
	public SuiVariableSource Source { get; set; } = new();

	/// <summary>Optional user-facing description; surfaced in tooltips + as a <c>///</c> summary on the generated property.</summary>
	public string Description { get; set; }

	/// <summary>If true, the Variables panel groups this under an "Advanced" collapsible section.</summary>
	public bool IsAdvanced { get; set; } = false;

	/// <summary>Optional category label for grouping in the Variables panel (e.g. "Stats").</summary>
	public string Group { get; set; }

	/// <summary>
	/// Only meaningful when <see cref="Type"/> is <c>Resource</c> — the concrete
	/// resource type name (e.g. <c>ModelResource</c>). PRD 18 § 3.3.
	/// </summary>
	public string ResourceType { get; set; }

	/// <summary>Generate a stable variable id like "var_a3f9b21c" derived from a guid.</summary>
	public static string NewVariableId()
	{
		var g = Guid.NewGuid();
		return "var_" + g.ToString( "N" ).Substring( 0, 8 );
	}

	public SuiVariable Clone() => new()
	{
		Id = Id,
		Name = Name,
		Type = Type,
		Default = Default?.DeepClone(),
		Source = Source?.Clone() ?? new(),
		Description = Description,
		IsAdvanced = IsAdvanced,
		Group = Group,
		ResourceType = ResourceType,
	};
}
