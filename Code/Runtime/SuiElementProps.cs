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
	};
}

/// <summary>
/// Designer-only flags attached to every element. Not generated to runtime SCSS.
/// </summary>
public sealed class SuiElementFlags
{
	/// <summary>V1.5 — expose this element to the generated C# class as a [Property].</summary>
	public bool IsVariable { get; set; } = false;

	/// <summary>Locked elements cannot be moved/edited in the canvas (still selectable).</summary>
	public bool Locked { get; set; } = false;

	/// <summary>Hidden in the designer canvas/preview (still in document).</summary>
	public bool HiddenInDesigner { get; set; } = false;

	public SuiElementFlags Clone() => new()
	{
		IsVariable = IsVariable,
		Locked = Locked,
		HiddenInDesigner = HiddenInDesigner,
	};
}
