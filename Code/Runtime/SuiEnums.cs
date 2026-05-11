namespace SboxUiDesigner.Runtime;

/// <summary>
/// Element types supported by Sbox UI Designer.
/// Logical types — the generator decides how each maps to the Yoga flexbox runtime.
/// (e.g. <see cref="Grid"/> is implemented as a wrapped flex container, NOT CSS Grid.)
/// </summary>
public enum SuiElementType
{
	Canvas,
	Panel,
	Overlay,
	Text,
	Image,
	Button,
	HorizontalBox,
	VerticalBox,
	Grid,
	ScrollPanel,
	ProgressBar,
	InventoryGrid,
	InventorySlot,
	ItemIcon,
	Tooltip,
	Hotbar,
}

/// <summary>
/// Layout modes the schema persists. Real CSS only has these two — Grid is a logical
/// element type implemented via wrapped flex (see <see cref="SuiElementType.Grid"/>).
/// </summary>
public enum SuiLayoutMode
{
	Absolute,
	Flex,
}

/// <summary>
/// Anchor presets for absolute layout. Translated to real left/top/right/bottom
/// (or transform fallback for centered) by the generator.
/// </summary>
public enum SuiAnchor
{
	TopLeft,
	TopCenter,
	TopRight,
	MiddleLeft,
	MiddleCenter,
	MiddleRight,
	BottomLeft,
	BottomCenter,
	BottomRight,
	Stretch,
	StretchHorizontal,
	StretchVertical,
}

public enum SuiFlexDirection
{
	Row,
	Column,
	RowReverse,
	ColumnReverse,
}

public enum SuiJustifyContent
{
	FlexStart,
	Center,
	FlexEnd,
	SpaceBetween,
	SpaceAround,
	SpaceEvenly,
}

public enum SuiAlignItems
{
	FlexStart,
	Center,
	FlexEnd,
	Stretch,
	Baseline,
}

public enum SuiFlexWrap
{
	NoWrap,
	Wrap,
	WrapReverse,
}

/// <summary>
/// Designer-level visibility. Mapped to runtime CSS by the generator:
/// Visible -> no rule
/// Hidden -> opacity: 0 (still occupies layout space — there is no visibility:hidden in s&box runtime)
/// Collapsed -> display: none (removed from layout)
/// </summary>
public enum SuiVisibility
{
	Visible,
	Hidden,
	Collapsed,
}

/// <summary>
/// Pointer-events policy. Runtime default is None (no clicks/hovers received) —
/// interactive elements opt in via All.
/// </summary>
public enum SuiPointerEvents
{
	None,
	All,
}

public enum SuiOverflow
{
	Visible,
	Hidden,
	Scroll,
}

/// <summary>
/// Canvas scaling strategy. Mapped to ScreenPanel.AutoScreenScale / Scale at preview/runtime.
/// MVP supports ScreenHeight1080 and FixedResolution. Stretch and DesktopResolution
/// are reserved for V1.
/// </summary>
public enum SuiScaleMode
{
	ScreenHeight1080,
	FixedResolution,
	Stretch,
	DesktopResolution,
}

public enum SuiTextAlign
{
	Left,
	Center,
	Right,
	Justify,
}

public enum SuiFontWeight
{
	Normal,
	Bold,
	Light,
	Medium,
	SemiBold,
	ExtraBold,
}

/// <summary>Matches the runtime enum: None, Ellipsis, Clip.</summary>
public enum SuiTextOverflow
{
	None,
	Ellipsis,
	Clip,
}

/// <summary>
/// How a Text element resolves its width and height.
///
/// - <see cref="Auto"/> (default) — W/H derived from text content. Single line,
///   no wrap. Text rect == text size, so vertical alignment is moot.
/// - <see cref="Fixed"/> — user-specified W/H. Supports <see cref="SuiTextAlign"/> +
///   <see cref="SuiVerticalAlign"/> for positioning the text inside the box.
/// - <see cref="AutoHeightWrap"/> — user specifies max-width; height grows with
///   wrapped lines. Horizontal align supported, vertical not.
/// </summary>
public enum SuiTextSizeMode
{
	Auto,
	Fixed,
	AutoHeightWrap,
}

public enum SuiVerticalAlign
{
	Top,
	Center,
	Bottom,
}

/// <summary>
/// Fill direction for ProgressBar — which edge the bar starts from.
/// LeftToRight is the most common (HP bars in HUDs). RightToLeft for RTL UIs
/// or specific designs. BottomToTop / TopToBottom for vertical progress.
/// </summary>
public enum SuiProgressDirection
{
	LeftToRight,
	RightToLeft,
	BottomToTop,
	TopToBottom,
}

public enum SuiImageFitMode
{
	Contain,
	Cover,
	Stretch,
	None,
}

public enum SuiBackgroundPosition
{
	Center,
	Top,
	Bottom,
	Left,
	Right,
	TopLeft,
	TopRight,
	BottomLeft,
	BottomRight,
}

/// <summary>
/// Strategy used to generate Grid-like elements (InventoryGrid, Hotbar, generic Grid).
/// WrappedFlex is the recommended default — see PRD doc 08.
/// </summary>
public enum SuiGridGenerationStrategy
{
	WrappedFlex,
	AbsoluteSlots,
}

public enum SuiGeneratedFileKind
{
	Razor,
	Scss,
	GeneratedCs,
	UserCs,
	CustomScss,
}

/// <summary>Event binding mode (V1.5+). Reserved for future use.</summary>
public enum SuiEventMode
{
	None,
	Code,
	Graph,
	CodeAndGraph,
}

/// <summary>Animation easing curves (V2). Reserved for future use.</summary>
public enum SuiAnimationEasing
{
	Linear,
	EaseIn,
	EaseOut,
	EaseInOut,
}

/// <summary>Background preview type for the design-time canvas (NOT generated).</summary>
public enum SuiBackgroundPreviewType
{
	Color,
	Image,
	None,
}
