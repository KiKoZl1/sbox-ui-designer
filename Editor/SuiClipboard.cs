using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Process-local clipboard for Cut/Copy/Paste of element subtrees inside the
/// designer. NOT integrated with the OS clipboard — same-process only.
///
/// Holds DEEP CLONES so the clipboard isn't invalidated when the source
/// element gets deleted (Cut does Copy → Delete, which would otherwise leave
/// dangling refs).
/// </summary>
public static class SuiClipboard
{
	private static SuiClipboardPayload _payload;

	public static bool HasContent => _payload != null && _payload.Root != null;

	public static void Set( SuiClipboardPayload payload )
	{
		_payload = payload;
	}

	public static SuiClipboardPayload Get() => _payload;

	public static void Clear()
	{
		_payload = null;
	}
}

/// <summary>
/// A self-contained subtree snapshot. <see cref="Root"/> is the top of the
/// subtree (its <c>ParentId</c> may still reference the original parent — the
/// paste logic ignores that and reattaches under the chosen new parent).
/// <see cref="All"/> contains every element in the subtree with parent/child
/// links valid relative to each other.
/// </summary>
public sealed class SuiClipboardPayload
{
	public SuiElement Root { get; set; }
	public List<SuiElement> All { get; set; } = new();
}
