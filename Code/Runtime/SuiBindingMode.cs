namespace SboxUiDesigner.Runtime;

/// <summary>
/// How a binding flows data between a Variable and a UI property (PRD 18 § 4.4).
/// Closed set — new modes may be added, existing names are never renamed
/// (forward-compat invariant C3, PRD 17 § 2.3).
/// </summary>
public enum SuiBindingMode
{
	/// <summary>Read once at first render, never again. Excluded from <c>BuildHash()</c>.</summary>
	OneTime,

	/// <summary>Read on every <c>BuildHash()</c> change. The default for most bindings.</summary>
	OneWay,

	/// <summary>Read on render, write on UI change. Input widgets only — native Sandbox.UI <c>Property:bind=</c>.</summary>
	TwoWay,

	/// <summary>Write-only (UI change → Variable). Reserved for V1.6.</summary>
	OneWayToSource,
}
