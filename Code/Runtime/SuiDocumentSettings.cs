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

	public bool ShowAnchors { get; set; } = false;

	public bool ShowSafeArea { get; set; } = false;

	/// <summary>Show the grid overlay (dots/lines at GridSize intervals) over the canvas.</summary>
	public bool ShowGrid { get; set; } = false;

	/// <summary>Show smart alignment guides (sibling/parent edges) during drag.</summary>
	public bool ShowAlignmentGuides { get; set; } = true;

	/// <summary>Outline every element on the canvas (debug aid — Image 2 mockup).</summary>
	public bool ShowLayoutBounds { get; set; } = true;

	/// <summary>Show small label per element with name + size (debug aid).</summary>
	public bool ShowWidgetInfo { get; set; } = false;

	/// <summary>Responsive Debug mode — surfaces layout issues + safe area
	/// + breakpoint warnings (mockup Image 2).</summary>
	public bool ResponsiveDebug { get; set; } = false;

	/// <summary>Persisted view state of the design canvas — restored on reopen.</summary>
	public float CanvasZoom { get; set; } = 1.0f;
	public float CanvasPanX { get; set; } = 0f;
	public float CanvasPanY { get; set; } = 0f;

	public SuiDocumentSettings Clone() => new()
	{
		AutoPreview = AutoPreview,
		PreviewDebounceMs = PreviewDebounceMs,
		SnapToGrid = SnapToGrid,
		GridSize = GridSize,
		ShowRulers = ShowRulers,
		ShowAnchors = ShowAnchors,
		ShowSafeArea = ShowSafeArea,
		ShowGrid = ShowGrid,
		ShowAlignmentGuides = ShowAlignmentGuides,
		ShowLayoutBounds = ShowLayoutBounds,
		ShowWidgetInfo = ShowWidgetInfo,
		ResponsiveDebug = ResponsiveDebug,
		CanvasZoom = CanvasZoom,
		CanvasPanX = CanvasPanX,
		CanvasPanY = CanvasPanY,
	};
}
