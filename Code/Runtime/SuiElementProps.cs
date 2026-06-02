using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Per-element type-specific properties. The generator picks the relevant subset
/// based on <see cref="SuiElement.Type"/>.
///
/// Implementation note: this class is a flat bag rather than a class hierarchy so
/// that Sandbox's GameResource serializer round-trips cleanly without polymorphic
/// type discriminators. The generator and validator only read fields relevant to
/// the element's type.
/// </summary>
public sealed class SuiElementProps
{
	// ---------- Text ----------

	public string Text { get; set; } = "";
	public float FontSize { get; set; } = 16f;
	public string FontFamily { get; set; }
	public SuiFontWeight FontWeight { get; set; } = SuiFontWeight.Normal;

	/// <summary>Hex color string, e.g. "#ffffff". Null = inherit.</summary>
	public string Color { get; set; } = "#ffffff";

	public SuiTextAlign TextAlign { get; set; } = SuiTextAlign.Left;
	public float? LineHeight { get; set; }
	public float LetterSpacing { get; set; } = 0f;
	public SuiTextOverflow TextOverflow { get; set; } = SuiTextOverflow.Clip;

	/// <summary>How the Text element sizes itself (Auto / Fixed / AutoHeightWrap).</summary>
	public SuiTextSizeMode TextSizeMode { get; set; } = SuiTextSizeMode.Auto;

	/// <summary>Only used when <see cref="TextSizeMode"/> is <see cref="SuiTextSizeMode.Fixed"/>.</summary>
	public SuiVerticalAlign VerticalAlign { get; set; } = SuiVerticalAlign.Top;

	// ---------- Image ----------

	public string ImagePath { get; set; }

	/// <summary>Hex color tint multiplied into the image. "#ffffff" = no tint.</summary>
	public string Tint { get; set; } = "#ffffff";

	public SuiImageFitMode FitMode { get; set; } = SuiImageFitMode.Contain;
	public SuiBackgroundPosition BackgroundPosition { get; set; } = SuiBackgroundPosition.Center;

	// ---------- Grid / InventoryGrid / Hotbar ----------

	public int Columns { get; set; } = 1;
	public int Rows { get; set; } = 1;
	public float CellWidth { get; set; } = 64f;
	public float CellHeight { get; set; } = 64f;
	public float GridGap { get; set; } = 4f;
	public bool AutoFill { get; set; } = false;
	public SuiGridGenerationStrategy GridStrategy { get; set; } = SuiGridGenerationStrategy.WrappedFlex;

	// ---------- Button ----------

	public string ButtonText { get; set; } = "";

	/// <summary>
	/// V1.5 M3.5 (PRD 25) — shape preset overriding the BorderRadius cascade.
	/// Rectangle = no override (use Style.BorderRadius); Custom = same as
	/// Rectangle but the inspector exposes the field; Square/Round/Pill
	/// force a derived radius.
	/// </summary>
	public SuiButtonShape ButtonShape { get; set; } = SuiButtonShape.Rectangle;

	// ---------- Interactive states (V1.5 M3.5 — PRD 25)
	// Applies to: Button, InventorySlot, ItemIcon. Each per-state override is
	// optional — null means the state inherits the element's Normal visuals
	// (Layout + base style props). Codegen reads these to emit the SCSS
	// pseudo-class rules in the order :hover < :focus < :active < .disabled.

	public SuiInteractiveStateStyle HoverStyle { get; set; }
	public SuiInteractiveStateStyle PressedStyle { get; set; }
	public SuiInteractiveStateStyle DisabledStyle { get; set; }
	public SuiInteractiveStateStyle FocusedStyle { get; set; }

	/// <summary>
	/// Runtime-bindable flag — when true the generated wrapper adds the
	/// <c>.disabled</c> class to the root element and suppresses onclick.
	/// Bind a Variable to this in the SUI Designer to toggle from gameplay.
	/// </summary>
	public bool IsDisabled { get; set; } = false;

	/// <summary>Emit <c>transition: all Ns ease</c> on the root selector. Default ON.</summary>
	public bool TransitionEnabled { get; set; } = true;

	/// <summary>Seconds. 0.15s is the Material Design "fast" baseline.</summary>
	public float TransitionDuration { get; set; } = 0.15f;

	/// <summary>SoundEvent asset path played on <c>:hover</c> ingress (SCSS <c>sound-in</c>). Empty = no sound.</summary>
	public string HoverSound { get; set; } = "";

	/// <summary>SoundEvent asset path played on <c>:active</c> ingress (press). Empty = no sound.</summary>
	public string PressSound { get; set; } = "";

	/// <summary>
	/// CSS cursor preset. <see cref="SuiCursor.Default"/> = no emit (inherit).
	/// Other values map to the matching CSS keyword via the SCSS generator.
	/// </summary>
	public SuiCursor Cursor { get; set; } = SuiCursor.Default;

	// ---------- ProgressBar ----------

	public float ProgressMin { get; set; } = 0f;
	public float ProgressMax { get; set; } = 100f;
	public float ProgressPreviewValue { get; set; } = 50f;
	public string ProgressFillColor { get; set; } = "#4ade80";
	public SuiProgressDirection ProgressDirection { get; set; } = SuiProgressDirection.LeftToRight;

	// ---------- Text wrap (Auto Wrap) ----------

	/// <summary>When true, Text wraps automatically. Same effect as TextSizeMode.AutoHeightWrap.</summary>
	public bool AutoWrapText { get; set; } = false;

	/// <summary>Pixel width to wrap at when AutoWrapText is true. 0 = use the element's Width.</summary>
	public float WrapTextAt { get; set; } = 0f;

	// ---------- Inventory slot / item ----------

	public int SlotIndex { get; set; } = 0;
	public string PreviewIconPath { get; set; }
	public int PreviewCount { get; set; } = 0;

	// ---------- V1.5 M4 — Input widgets (PRD 21) ----------

	// TextEntry
	public string PlaceholderText { get; set; } = "";
	public int MaxLength { get; set; } = 0;          // 0 = unbounded
	public bool ReadOnly { get; set; } = false;
	public string PreviewValue { get; set; } = "";   // Design-time preview text

	// Slider — backed by Sandbox.UI.SliderControl. No orientation property on
	// the engine type; SuiSliderOrientation is kept for future Vertical support
	// but currently emitted as horizontal-only (PRD 21 § 11 #2).
	public float SliderMin { get; set; } = 0f;
	public float SliderMax { get; set; } = 100f;
	public float SliderStep { get; set; } = 1f;
	public SuiSliderOrientation SliderOrientation { get; set; } = SuiSliderOrientation.Horizontal;
	public string SliderTrackColor { get; set; } = "#22222288";
	public string SliderFillColor { get; set; } = "#4ade80";
	public string SliderHandleColor { get; set; } = "#ffffff";
	public bool SliderShowValue { get; set; } = false;
	public float SliderValue { get; set; } = 50f;    // Design-time preview position
	// V1.5 M4 — custom tooltip pill rendered alongside the engine SliderControl
	// (engine tooltip is hidden via display:none). Codegen emits our own
	// <div class="sui-slider-tooltip"> so author colors actually apply — the
	// engine's own tooltip can't be recolored from user SCSS because Sandbox.UI
	// parser doesn't honor compound class selectors needed to beat its built-in
	// rule specificity.
	public string SliderTooltipBgColor { get; set; } = "#000000";
	public string SliderTooltipTextColor { get; set; } = "#ffffff";

	// Toggle — backed by Sandbox.UI.Checkbox.
	public bool ToggleChecked { get; set; } = false;
	public string ToggleLabelText { get; set; } = "";

	// DropDown — backed by Sandbox.UI.DropDown.
	public System.Collections.Generic.List<string> DropDownOptions { get; set; } = new();
	public int DropDownSelectedIndex { get; set; } = 0;  // Design-time preview which option is selected

	public SuiElementProps Clone() => new()
	{
		Text = Text,
		FontSize = FontSize,
		FontFamily = FontFamily,
		FontWeight = FontWeight,
		Color = Color,
		TextAlign = TextAlign,
		LineHeight = LineHeight,
		LetterSpacing = LetterSpacing,
		TextOverflow = TextOverflow,
		TextSizeMode = TextSizeMode,
		VerticalAlign = VerticalAlign,
		ImagePath = ImagePath,
		Tint = Tint,
		FitMode = FitMode,
		BackgroundPosition = BackgroundPosition,
		Columns = Columns,
		Rows = Rows,
		CellWidth = CellWidth,
		CellHeight = CellHeight,
		GridGap = GridGap,
		AutoFill = AutoFill,
		GridStrategy = GridStrategy,
		ButtonText = ButtonText,
		ButtonShape = ButtonShape,
		HoverStyle = HoverStyle?.Clone(),
		PressedStyle = PressedStyle?.Clone(),
		DisabledStyle = DisabledStyle?.Clone(),
		FocusedStyle = FocusedStyle?.Clone(),
		IsDisabled = IsDisabled,
		TransitionEnabled = TransitionEnabled,
		TransitionDuration = TransitionDuration,
		HoverSound = HoverSound,
		PressSound = PressSound,
		Cursor = Cursor,
		ProgressMin = ProgressMin,
		ProgressMax = ProgressMax,
		ProgressPreviewValue = ProgressPreviewValue,
		ProgressFillColor = ProgressFillColor,
		ProgressDirection = ProgressDirection,
		AutoWrapText = AutoWrapText,
		WrapTextAt = WrapTextAt,
		SlotIndex = SlotIndex,
		PreviewIconPath = PreviewIconPath,
		PreviewCount = PreviewCount,
		// M4 input widgets
		PlaceholderText = PlaceholderText,
		MaxLength = MaxLength,
		ReadOnly = ReadOnly,
		PreviewValue = PreviewValue,
		SliderMin = SliderMin,
		SliderMax = SliderMax,
		SliderStep = SliderStep,
		SliderOrientation = SliderOrientation,
		SliderTrackColor = SliderTrackColor,
		SliderFillColor = SliderFillColor,
		SliderHandleColor = SliderHandleColor,
		SliderShowValue = SliderShowValue,
		SliderValue = SliderValue,
		SliderTooltipBgColor = SliderTooltipBgColor,
		SliderTooltipTextColor = SliderTooltipTextColor,
		ToggleChecked = ToggleChecked,
		ToggleLabelText = ToggleLabelText,
		DropDownOptions = new System.Collections.Generic.List<string>( DropDownOptions ?? new() ),
		DropDownSelectedIndex = DropDownSelectedIndex,
	};
}

/// <summary>
/// Designer-only flags attached to every element. Not generated to runtime SCSS.
/// </summary>
public sealed class SuiElementFlags
{
	/// <summary>Locked elements cannot be moved/edited in the canvas (still selectable).</summary>
	public bool Locked { get; set; } = false;

	/// <summary>Hidden in the designer canvas/preview (still in document).</summary>
	public bool HiddenInDesigner { get; set; } = false;

	/// <summary>
	/// V1.5 M3 (PRD 20 § 5) — emit a Razor <c>@ref="&lt;ElementName&gt;"</c> on this
	/// element and declare a typed field on the generated renderer Panel class so
	/// the user's <c>&lt;Name&gt;.partial.cs</c> can poke the live element imperatively.
	/// Field name = sanitized element <see cref="SuiElement.Name"/>.
	/// </summary>
	public bool ExposeAsVariable { get; set; } = false;

	public SuiElementFlags Clone() => new()
	{
		Locked = Locked,
		HiddenInDesigner = HiddenInDesigner,
		ExposeAsVariable = ExposeAsVariable,
	};
}
