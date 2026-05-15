using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// One converter applied in a binding's chain (PRD 18 § 4.2, § 5). The chain runs
/// in list order: <c>Variable → step₀ → step₁ → … → UI property</c>.
/// </summary>
public sealed class SuiBindingConverterStep
{
	/// <summary>
	/// Converter identity — <c>builtin.&lt;Name&gt;</c>, <c>user.&lt;FullType&gt;.&lt;Method&gt;</c>,
	/// or <c>graph.&lt;RelativeAssetPath&gt;</c> (PRD 18 § 5.1).
	/// </summary>
	public string ConverterRef { get; set; }

	/// <summary>
	/// Ordered arguments fed to the converter. An empty list means the single
	/// implicit input is <c>"Previous"</c> (the prior chain output).
	/// </summary>
	public List<SuiConverterArg> Args { get; set; } = new();

	public SuiBindingConverterStep Clone()
	{
		var c = new SuiBindingConverterStep { ConverterRef = ConverterRef };
		foreach ( var a in Args ) c.Args.Add( a?.Clone() );
		return c;
	}
}
