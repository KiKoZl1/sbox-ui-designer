namespace SboxUiDesigner.Runtime;

/// <summary>
/// Where a <see cref="SuiVariable"/>'s runtime value originates. Closed set
/// — V1.5 ships ONLY <see cref="Manual"/>. Previous alpha entries
/// (<c>FromComponent</c>, <c>FromActionGraph</c>) were ripped at M4 close
/// per DEVIATION D-017 (Doo replaces ActionGraph; the Component source is
/// not a V1.5 surface). Migration removes the dead fields from on-disk
/// schemas via Force Regen; System.Text.Json silently ignores the legacy
/// <c>FromComponent</c> / <c>FromActionGraph</c> string values during load.
/// </summary>
public enum SuiVariableSourceKind
{
	/// <summary>Default. Gameplay code assigns the generated <c>[Property]</c> directly.</summary>
	Manual,
}

/// <summary>
/// The <c>Source</c> block of a <see cref="SuiVariable"/>. Manual-only in
/// V1.5 — the type stays as a holder (rather than collapsing into a bare
/// enum on the Variable) so the schema can grow new kinds without another
/// rename round.
/// </summary>
public sealed class SuiVariableSource
{
	public SuiVariableSourceKind Kind { get; set; } = SuiVariableSourceKind.Manual;

	public SuiVariableSource Clone() => new()
	{
		Kind = Kind,
	};
}
