using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Records a duplicate-<c>DocumentId</c> situation — two or more .sui files
/// declaring the same GUID (PRD 22 § 3.4). Conflicted GUIDs are deliberately
/// NOT resolvable: <see cref="SuiAssetRegistry.Resolve"/> returns null until the
/// user removes the duplication or regenerates one document's GUID.
/// </summary>
public sealed class SuiAssetConflict
{
	/// <summary>The duplicated <c>DocumentId</c> GUID.</summary>
	public string Guid { get; set; }

	/// <summary>Every project-relative path found declaring this GUID (2+).</summary>
	public List<string> Paths { get; set; } = new();

	/// <summary>ISO-8601 UTC timestamp of when the conflict was first detected.</summary>
	public string FirstSeen { get; set; }
}
