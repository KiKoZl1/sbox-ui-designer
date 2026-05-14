namespace SboxUiDesigner.Runtime;

/// <summary>
/// One record in the <see cref="SuiAssetRegistry"/> — maps a .sui document's
/// stable <c>DocumentId</c> GUID to its location on disk (PRD 22 § 3.2).
/// </summary>
public sealed class SuiAssetEntry
{
	/// <summary>Project-relative path, forward slashes (cross-platform safe).</summary>
	public string Path { get; set; }

	/// <summary>The document's <c>Name</c> field — cached for fast Find-Usages display.</summary>
	public string Name { get; set; }

	/// <summary>ISO-8601 UTC timestamp of when this entry was last confirmed against disk.</summary>
	public string LastSeen { get; set; }
}
