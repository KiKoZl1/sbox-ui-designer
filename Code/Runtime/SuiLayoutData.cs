namespace SboxUiDesigner.Runtime;

/// <summary>
/// Per-element layout block. Carries either Absolute or Flex fields depending on
/// <see cref="Mode"/>. Fields not used in the chosen mode are still serialized
/// (preserves user values when toggling).
/// </summary>
public sealed class SuiLayoutData
{
	public SuiLayoutMode Mode { get; set; } = SuiLayoutMode.Absolute;

	// ---------- Absolute mode ----------

	public float X { get; set; } = 0f;
	public float Y { get; set; } = 0f;
	public float Width { get; set; } = 0f;
	public float Height { get; set; } = 0f;

	public float? MinWidth { get; set; }
	public float? MinHeight { get; set; }
	public float? MaxWidth { get; set; }
	public float? MaxHeight { get; set; }

	public SuiAnchor Anchor { get; set; } = SuiAnchor.TopLeft;

	/// <summary>0 = left/top, 0.5 = center, 1 = right/bottom.</summary>
	public float PivotX { get; set; } = 0f;
	public float PivotY { get; set; } = 0f;

	public int ZIndex { get; set; } = 0;

	// ---------- Flex mode ----------

	public SuiFlexDirection FlexDirection { get; set; } = SuiFlexDirection.Row;
	public SuiJustifyContent JustifyContent { get; set; } = SuiJustifyContent.FlexStart;

	/// <summary>
	/// Note: the s&box runtime default is Stretch. We keep that as the schema default
	/// to match engine behaviour out of the box. Users can override per element.
	/// </summary>
	public SuiAlignItems AlignItems { get; set; } = SuiAlignItems.Stretch;

	public SuiFlexWrap FlexWrap { get; set; } = SuiFlexWrap.NoWrap;
	public float Gap { get; set; } = 0f;

	// ---------- Shared ----------

	public SuiSpacing Margin { get; set; } = new();
	public SuiSpacing Padding { get; set; } = new();

	public SuiLayoutData Clone() => new()
	{
		Mode = Mode,
		X = X,
		Y = Y,
		Width = Width,
		Height = Height,
		MinWidth = MinWidth,
		MinHeight = MinHeight,
		MaxWidth = MaxWidth,
		MaxHeight = MaxHeight,
		Anchor = Anchor,
		PivotX = PivotX,
		PivotY = PivotY,
		ZIndex = ZIndex,
		FlexDirection = FlexDirection,
		JustifyContent = JustifyContent,
		AlignItems = AlignItems,
		FlexWrap = FlexWrap,
		Gap = Gap,
		Margin = Margin?.Clone() ?? new(),
		Padding = Padding?.Clone() ?? new(),
	};
}
