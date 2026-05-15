using System.Text.Json.Nodes;

namespace SboxUiDesigner.Runtime;

/// <summary>What kind of value a converter argument carries (PRD 18 § 4.2, § 11 #4).</summary>
public enum SuiConverterArgKind
{
	/// <summary>
	/// References the chain itself — <c>"Source"</c> (the binding's Variable value)
	/// or <c>"Previous"</c> (the prior converter step's output).
	/// </summary>
	ChainRef,

	/// <summary>References another document Variable by Id.</summary>
	Variable,

	/// <summary>A raw JSON literal — e.g. a constant denominator (PRD 18 § 11 #4).</summary>
	Literal,
}

/// <summary>
/// One argument fed to a <see cref="SuiBindingConverterStep"/>. A union of: a
/// chain reference (<c>"Source"</c>/<c>"Previous"</c>), a Variable reference, or
/// a JSON literal (PRD 18 § 4.2).
/// </summary>
public sealed class SuiConverterArg
{
	public SuiConverterArgKind Kind { get; set; } = SuiConverterArgKind.ChainRef;

	/// <summary>When <see cref="Kind"/> is <see cref="SuiConverterArgKind.ChainRef"/> — <c>"Source"</c> or <c>"Previous"</c>.</summary>
	public string ChainRef { get; set; }

	/// <summary>When <see cref="Kind"/> is <see cref="SuiConverterArgKind.Variable"/> — the Variable Id.</summary>
	public string VariableId { get; set; }

	/// <summary>When <see cref="Kind"/> is <see cref="SuiConverterArgKind.Literal"/> — the raw JSON value.</summary>
	public JsonNode Literal { get; set; }

	public SuiConverterArg Clone() => new()
	{
		Kind = Kind,
		ChainRef = ChainRef,
		VariableId = VariableId,
		Literal = Literal?.DeepClone(),
	};
}
