using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Resolved metadata for a <see cref="SuiElementType.SuiReference"/>'s
/// <c>SourceGuid</c>. Lets the generator emit the correct
/// <c>&lt;Namespace.ClassName Prop=val ... /&gt;</c> tag without itself
/// loading other <c>.sui</c> files. Populated by the editor side from the
/// Asset Registry + the source doc's <c>Output</c>/<c>AcceptedProps</c>.
/// </summary>
public sealed class SuiReferenceTarget
{
	public string Namespace { get; set; }
	public string ClassName { get; set; }

	/// <summary>Closed list of the source doc's AcceptedProps, preserving Type for codegen literal/expression rendering.</summary>
	public IList<SuiAcceptedProp> AcceptedProps { get; set; }
}
