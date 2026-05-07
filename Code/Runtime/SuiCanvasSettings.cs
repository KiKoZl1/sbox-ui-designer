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

	public SuiCanvasSettings Clone() => new()
	{
		BaseWidth = BaseWidth,
		BaseHeight = BaseHeight,
		ScaleMode = ScaleMode,
		SafeArea = SafeArea?.Clone() ?? new(),
		BackgroundPreview = BackgroundPreview?.Clone() ?? new(),
	};
}
