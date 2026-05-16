namespace SboxUiDesigner.Runtime;

/// <summary>
/// V1.5 — what bootstrap code the generator emits beyond the PanelComponent class
/// (PRD 17 § 5). <see cref="Manual"/> is value 0 so a V1 document (which predates
/// this field) deserialises to the correct conservative default automatically.
/// </summary>
public enum SuiOutputMode
{
	/// <summary>Generator emits only the class + [Property] slots; the developer handles lifetime. V1 behaviour.</summary>
	Manual,

	/// <summary>Generator emits a static factory (Show/Hide) — for menus, modals, overlays.</summary>
	Singleton,

	/// <summary>
	/// V1.5 Instance mode (PRD 22 V1.5 revised). Generator emits BOTH the
	/// PanelComponent renderer (<c>&lt;Name&gt;Panel</c>) AND a user-facing
	/// wrapper class <c>&lt;Name&gt; : SuiPanel&lt;&lt;Name&gt;Panel&gt;</c>
	/// the dev declares as a <c>[Property]</c> field on their own Component
	/// and drives via <c>Add()/Show()/Hide()/Remove()</c>.
	/// </summary>
	Instance,

	/// <summary>
	/// DEPRECATED — replaced by <see cref="Instance"/> in V1.5. Older docs
	/// auto-migrate on load. Kept as an enum value so existing
	/// <c>.sui</c> JSON deserialises cleanly; the generator routes it to
	/// Instance emit.
	/// </summary>
	[System.Obsolete( "Use Instance instead — same intent, dev-controlled lifetime instead of auto-mount." )]
	PerLocalPlayer,
}

/// <summary>
/// Output configuration for the compile step. Stored inside the .sui so subsequent
/// compiles do not re-prompt the user.
/// </summary>
public sealed class SuiOutputSettings
{
	/// <summary>True once the user has chosen an output folder at least once.</summary>
	public bool Configured { get; set; } = false;

	/// <summary>
	/// V1.5 — output mode (PRD 17 § 5). Defaults to <see cref="SuiOutputMode.Manual"/>;
	/// the V1 → V2 migration sets it explicitly for legacy documents.
	/// </summary>
	public SuiOutputMode Mode { get; set; } = SuiOutputMode.Manual;

	/// <summary>
	/// Project-relative output folder for generated files. e.g.
	/// "Code/UI/Generated/Inventory" or "Code/UI". Final mode writes here;
	/// preview mode ignores this and writes to the preview cache root.
	/// </summary>
	public string RootFolder { get; set; } = null;

	/// <summary>C# namespace to use for generated panel components.</summary>
	public string Namespace { get; set; } = "Game.UI";

	/// <summary>Generated PanelComponent class name. Defaults to the document name on first compile.</summary>
	public string ClassName { get; set; } = null;

	public bool GenerateRazor { get; set; } = true;
	public bool GenerateScss { get; set; } = true;

	/// <summary>V1.5 — generate paired .generated.cs partial.</summary>
	public bool GenerateGeneratedCs { get; set; } = false;

	/// <summary>V1.5 — create paired .User.cs partial only if missing.</summary>
	public bool GenerateUserCsIfMissing { get; set; } = false;

	/// <summary>V1+ — create paired .custom.scss only if missing.</summary>
	public bool GenerateCustomScssIfMissing { get; set; } = false;

	public SuiOutputSettings Clone() => new()
	{
		Configured = Configured,
		Mode = Mode,
		RootFolder = RootFolder,
		Namespace = Namespace,
		ClassName = ClassName,
		GenerateRazor = GenerateRazor,
		GenerateScss = GenerateScss,
		GenerateGeneratedCs = GenerateGeneratedCs,
		GenerateUserCsIfMissing = GenerateUserCsIfMissing,
		GenerateCustomScssIfMissing = GenerateCustomScssIfMissing,
	};
}
