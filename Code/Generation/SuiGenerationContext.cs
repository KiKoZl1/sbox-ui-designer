using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Input bag for the generator. The same generator drives both Preview mode
/// (writes to the hot-loadable preview cache, doesn't touch manifest) and
/// Final mode (writes to the user's chosen output folder, updates manifest).
/// </summary>
public sealed class SuiGenerationContext
{
	public SuiDocument Document { get; set; }

	public SuiGenerationMode Mode { get; set; } = SuiGenerationMode.Preview;

	/// <summary>
	/// Project-relative folder that produced files should land in. For Preview
	/// mode this is the preview cache root (eg `Code/_sui_preview/InventoryUI`);
	/// for Final mode it's the user's <c>Output.RootFolder</c>.
	/// </summary>
	public string OutputFolder { get; set; }

	/// <summary>
	/// Generated PanelComponent class name. Defaults to <c>Document.Output.ClassName</c>
	/// (or sanitized <c>Document.Name</c>) — the SCSS outer selector will use
	/// this as the type name.
	/// </summary>
	public string ClassName { get; set; }

	/// <summary>Generated namespace. Defaults to <c>Document.Output.Namespace</c>.</summary>
	public string Namespace { get; set; }

	/// <summary>
	/// V1.5 — resolves a SuiReference SourceGuid to a
	/// <see cref="SuiReferenceTarget"/> carrying the embedded child's generated
	/// <c>(Namespace, ClassName, AcceptedProps)</c>. Lets the generator emit
	/// the correct <c>&lt;Game.UI.InventorySlot ... /&gt;</c> tag without
	/// touching the Asset Registry directly. Editor populates this from
	/// <c>SuiAssetRegistryService.Instance</c> + reading each referenced doc's
	/// <c>Output</c> block. Null = unresolved (warning emitted in markup).
	/// </summary>
	public System.Func<string, SuiReferenceTarget> ResolveReferencedClass { get; set; }
}

public enum SuiGenerationMode
{
	Preview,
	Final,
}
