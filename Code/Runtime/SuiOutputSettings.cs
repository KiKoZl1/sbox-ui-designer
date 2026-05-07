namespace SboxUiDesigner.Runtime;

/// <summary>
/// Output configuration for the compile step. Stored inside the .sui so subsequent
/// compiles do not re-prompt the user.
/// </summary>
public sealed class SuiOutputSettings
{
	/// <summary>True once the user has chosen an output folder at least once.</summary>
	public bool Configured { get; set; } = false;

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
