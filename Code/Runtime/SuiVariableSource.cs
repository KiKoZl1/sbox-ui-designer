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

	/// <summary>
	/// V1.5-M2-K — DEPRECATED. Variables with this source kind are migrated to
	/// <see cref="Manual"/> on load (see
	/// <see cref="SuiDocument.MigrateAcceptedPropsToPublicVariables"/>). The
	/// enum value is kept only so JSON deserialise of legacy schema doesn't
	/// throw. New documents never write this kind. See DEVIATIONS D-005.
	/// </summary>
	FromAcceptedProp,
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

	// ---------- FromAcceptedProp ----------

	/// <summary>Id of the <see cref="SuiAcceptedProp"/> this Variable mirrors (PRD 19 § 3.2).</summary>
	public string PropId { get; set; }

	public SuiVariableSource Clone() => new()
	{
		Kind = Kind,
		ComponentVariableId = ComponentVariableId,
		PropertyPath = PropertyPath,
		ActionGraphAssetPath = ActionGraphAssetPath,
		PropId = PropId,
	};
}
