using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Resolved metadata for a <see cref="SuiElementType.SuiReference"/>'s
/// <c>SourceGuid</c>. Lets the generator emit the correct
/// <c>&lt;Namespace.ClassName Prop=val ... /&gt;</c> tag without itself
/// loading other <c>.sui</c> files. Populated by the editor side from the
/// Asset Registry + the source doc's <c>Output</c>/<c>Variables</c> (the
/// IsPublic-flagged ones — V1.5-M2-K).
/// </summary>
public sealed class SuiReferenceTarget
{
	public string Namespace { get; set; }
	public string ClassName { get; set; }

	/// <summary>
	/// The source doc's public Variables — every Variable with
	/// <see cref="SuiVariable.IsPublic"/> true. These become parent-settable
	/// props at codegen time; Variable.Id is the key used in the parent's
	/// <c>SuiReferenceData.Props</c> map.
	/// </summary>
	public IList<SuiVariable> PublicVariables { get; set; }
}
