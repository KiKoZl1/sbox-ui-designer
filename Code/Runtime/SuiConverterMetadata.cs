using System;

namespace SboxUiDesigner.Runtime;

/// <summary>One input parameter of a converter — drives the bind-popup chain editor.</summary>
public sealed class SuiConverterParam
{
	public string Name { get; set; }

	/// <summary>C# type name of the parameter (e.g. <c>Single</c>, <c>Int32</c>, <c>String</c>, <c>Color</c>).</summary>
	public string Type { get; set; }

	/// <summary>True if the parameter has a C# default and may be omitted in the chain.</summary>
	public bool HasDefault { get; set; }
}

/// <summary>
/// Editor-facing description of a converter — drives the bind-popup converter
/// dropdown and chain validation (PRD 18 § 5.3). Built once per converter by
/// <see cref="SuiConverterCatalog"/>.
/// </summary>
public sealed class SuiConverterMetadata
{
	/// <summary>
	/// Stable identity — <c>builtin.&lt;Name&gt;</c>, <c>user.&lt;FullType&gt;.&lt;Method&gt;</c>,
	/// or <c>graph.&lt;path&gt;</c> (PRD 18 § 5.1).
	/// </summary>
	public string Ref { get; set; }

	public string DisplayName { get; set; }
	public string Category { get; set; }
	public string Description { get; set; }

	public SuiConverterParam[] Inputs { get; set; } = Array.Empty<SuiConverterParam>();

	/// <summary>C# type name of the converter's return value.</summary>
	public string ReturnType { get; set; }

	/// <summary>True for the shipped <see cref="SuiBuiltinConverters"/>; false for user / ActionGraph converters.</summary>
	public bool IsBuiltin { get; set; }
}
