using System;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Marks a <c>public static</c> method as a SUI converter — a typed transform
/// usable in a binding's converter chain (PRD 18 § 5). Applied to all built-ins
/// (for catalog uniformity) and to user-authored converter methods (for
/// TypeLibrary discovery in M1-C).
/// </summary>
[AttributeUsage( AttributeTargets.Method )]
public sealed class SuiConverterAttribute : Attribute
{
	/// <summary>User-facing converter name. Empty = fall back to the method name.</summary>
	public string DisplayName { get; }

	/// <summary>Category label for grouping in the bind-popup converter dropdown.</summary>
	public string Category { get; set; } = "User";

	/// <summary>Short description shown in the bind-popup.</summary>
	public string Description { get; set; } = "";

	/// <summary>
	/// Opt-in flag: when true and the LAST parameter is an array type, the
	/// bind-popup treats it as a C# <c>params</c> array — user can append N
	/// additional arguments at runtime via a <c>+ Add Arg</c> button. Set this
	/// alongside <c>params Foo[] args</c> in the method signature.
	/// </summary>
	public bool LastParamIsVariadic { get; set; } = false;

	public SuiConverterAttribute( string displayName = "" )
	{
		DisplayName = displayName;
	}
}
