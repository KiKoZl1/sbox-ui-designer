namespace SboxUiDesigner.Runtime;

/// <summary>
/// Where a <see cref="SuiVariable"/>'s runtime value originates. Closed set
/// (PRD 18 § 3.5) — additions require a schema migration.
/// </summary>
public enum SuiVariableSourceKind
{
	/// <summary>Default. Gameplay code assigns the generated <c>[Property]</c> directly.</summary>
	Manual,

	/// <summary>Value is pulled from a sibling Component property each refresh (PRD 18 § 3.5.2).</summary>
	FromComponent,

	/// <summary>Value is computed by an ActionGraph each <c>BuildHash()</c> evaluation (PRD 18 § 3.5.3).</summary>
	FromActionGraph,
}

/// <summary>
/// The <c>Source</c> block of a <see cref="SuiVariable"/>. Only the fields
/// relevant to <see cref="Kind"/> are populated; the rest stay null.
/// </summary>
public sealed class SuiVariableSource
{
	public SuiVariableSourceKind Kind { get; set; } = SuiVariableSourceKind.Manual;

	// ---------- FromComponent ----------

	/// <summary>Id of the Component-typed Variable this one reads from.</summary>
	public string ComponentVariableId { get; set; }

	/// <summary>Single-segment property name on the source Component (multi-segment is V1.6).</summary>
	public string PropertyPath { get; set; }

	// ---------- FromActionGraph ----------

	/// <summary>Project-relative path to the <c>.action</c> asset that computes the value.</summary>
	public string ActionGraphAssetPath { get; set; }

	public SuiVariableSource Clone() => new()
	{
		Kind = Kind,
		ComponentVariableId = ComponentVariableId,
		PropertyPath = PropertyPath,
		ActionGraphAssetPath = ActionGraphAssetPath,
	};
}
