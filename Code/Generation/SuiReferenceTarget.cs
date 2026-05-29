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

	/// <summary>
	/// Sanitized field names of every <see cref="SuiElementType.SuiReference"/>
	/// element inside the target doc. The Razor parent needs these so it can
	/// pass its own grandchild wrapper references INTO the nested
	/// <c>&lt;ChildPanel ChildField=@(field?.ChildField) /&gt;</c> tag —
	/// without that, the freshly Razor-constructed child Panel starts with
	/// default child wrappers and never reflects the grandparent's state
	/// (the "TestGrand Slot1/Slot2 don't propagate" bug).
	/// </summary>
	public IList<string> ChildReferenceFieldNames { get; set; }
}
