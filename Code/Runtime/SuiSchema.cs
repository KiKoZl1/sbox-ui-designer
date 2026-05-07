namespace SboxUiDesigner.Runtime;

/// <summary>
/// Schema version constants for .sui document migration.
/// </summary>
public static class SuiSchemaVersion
{
	/// <summary>Current schema version emitted by this generator.</summary>
	public const int Current = 1;

	/// <summary>Earliest schema version this codebase can load (for migration).</summary>
	public const int MinimumSupported = 1;

	/// <summary>Designer version string written to the document on save.</summary>
	public const string DesignerVersion = "0.1.0";

	/// <summary>Tool name written to the document on save.</summary>
	public const string CreatedWithTag = "Sbox UI Designer";
}

/// <summary>
/// Box-style spacing — used for both margin and padding.
/// All values are in design-space pixels.
/// </summary>
public sealed class SuiSpacing
{
	public float Left { get; set; }
	public float Top { get; set; }
	public float Right { get; set; }
	public float Bottom { get; set; }

	public SuiSpacing() { }

	public SuiSpacing( float all )
	{
		Left = Top = Right = Bottom = all;
	}

	public SuiSpacing( float left, float top, float right, float bottom )
	{
		Left = left;
		Top = top;
		Right = right;
		Bottom = bottom;
	}

	public bool IsZero => Left == 0f && Top == 0f && Right == 0f && Bottom == 0f;

	public bool IsUniform => Left == Top && Top == Right && Right == Bottom;

	public SuiSpacing Clone() => new( Left, Top, Right, Bottom );
}

/// <summary>
/// Optional safe-area inset for the canvas (overlay only — designer aid).
/// </summary>
public sealed class SuiSafeArea
{
	public bool Enabled { get; set; } = false;
	public float Left { get; set; } = 0f;
	public float Top { get; set; } = 0f;
	public float Right { get; set; } = 0f;
	public float Bottom { get; set; } = 0f;

	public SuiSafeArea Clone() => new()
	{
		Enabled = Enabled,
		Left = Left,
		Top = Top,
		Right = Right,
		Bottom = Bottom,
	};
}

/// <summary>
/// Design-time canvas background (NOT emitted to generated SCSS — only shown in the editor).
/// </summary>
public sealed class SuiBackgroundPreview
{
	public SuiBackgroundPreviewType Type { get; set; } = SuiBackgroundPreviewType.Color;

	/// <summary>Hex color string, e.g. "#101010" or "#101010ff". Empty = use default.</summary>
	public string Color { get; set; } = "#101010";

	/// <summary>Optional image asset path for the preview only.</summary>
	public string ImagePath { get; set; } = null;

	public SuiBackgroundPreview Clone() => new()
	{
		Type = Type,
		Color = Color,
		ImagePath = ImagePath,
	};
}
