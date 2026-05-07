namespace SboxUiDesigner.Runtime;

/// <summary>
/// Per-document designer preferences. Stored inside the .sui so the
/// editor reopens with the same UI state.
/// </summary>
public sealed class SuiDocumentSettings
{
	public bool AutoPreview { get; set; } = true;

	/// <summary>Debounce in milliseconds for preview regeneration during edits.</summary>
	public int PreviewDebounceMs { get; set; } = 350;

	public bool SnapToGrid { get; set; } = true;

	public int GridSize { get; set; } = 8;

	public bool ShowRulers { get; set; } = true;

	public bool ShowAnchors { get; set; } = true;

	public bool ShowSafeArea { get; set; } = false;

	public SuiDocumentSettings Clone() => new()
	{
		AutoPreview = AutoPreview,
		PreviewDebounceMs = PreviewDebounceMs,
		SnapToGrid = SnapToGrid,
		GridSize = GridSize,
		ShowRulers = ShowRulers,
		ShowAnchors = ShowAnchors,
		ShowSafeArea = ShowSafeArea,
	};
}
