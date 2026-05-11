namespace SboxUiDesigner.Runtime;

/// <summary>
/// Canvas / scale / safe-area / preview-background settings for a .sui document.
/// Maps to ScreenPanel host properties at preview/runtime time.
/// </summary>
public sealed class SuiCanvasSettings
{
	/// <summary>Design-time reference width in pixels.</summary>
	public int BaseWidth { get; set; } = 1920;

	/// <summary>Design-time reference height in pixels.</summary>
	public int BaseHeight { get; set; } = 1080;

	/// <summary>Scaling strategy. Maps to ScreenPanel.AutoScreenScale / Scale.</summary>
	public SuiScaleMode ScaleMode { get; set; } = SuiScaleMode.ScreenHeight1080;

	/// <summary>Optional safe-area overlay (designer aid only).</summary>
	public SuiSafeArea SafeArea { get; set; } = new();

	/// <summary>Design-only canvas background (NOT generated to runtime SCSS).</summary>
	public SuiBackgroundPreview BackgroundPreview { get; set; } = new();

	/// <summary>
	/// Preview/test target resolution width. Distinct from <see cref="BaseWidth"/> —
	/// BaseWidth drives the design-time logical pixel space (always 1920 by convention),
	/// while PreviewWidth/Height let the user simulate different viewport sizes
	/// (1280×720, 2560×1440, ultrawide) in the canvas without rebuilding the doc.
	/// 0 / unset means "match BaseWidth/Height".
	/// </summary>
	public int PreviewWidth { get; set; } = 0;
	public int PreviewHeight { get; set; } = 0;

	public SuiCanvasSettings Clone() => new()
	{
		BaseWidth = BaseWidth,
		BaseHeight = BaseHeight,
		ScaleMode = ScaleMode,
		SafeArea = SafeArea?.Clone() ?? new(),
		BackgroundPreview = BackgroundPreview?.Clone() ?? new(),
		PreviewWidth = PreviewWidth,
		PreviewHeight = PreviewHeight,
	};
}
