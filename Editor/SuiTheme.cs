namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Single source of truth for visual styling across the SUI Designer.
/// Every widget reads from here so the look stays consistent and tweaks
/// land in one place. Values mirror the M14 mockups (dark UI editor).
/// </summary>
internal static class SuiTheme
{
	// ── Colors (CSS rgba strings — Editor.Widget.SetStyles wants CSS) ─────

	/// <summary>Window/panel base background — slightly above pure black.</summary>
	public const string BgBase = "rgb(26, 28, 32)";

	/// <summary>One step above base — dock surfaces, toolbar.</summary>
	public const string BgSurface = "rgb(32, 35, 40)";

	/// <summary>Two steps above — input fields, dropdown buttons.</summary>
	public const string BgRaised = "rgb(40, 44, 50)";

	/// <summary>Hover tint (overlay over Bg* via background-color).</summary>
	public const string BgHover = "rgba(255, 255, 255, 0.05)";

	/// <summary>Pressed/active tint.</summary>
	public const string BgPressed = "rgba(255, 255, 255, 0.10)";

	/// <summary>Selected/accent tint.</summary>
	public const string BgAccentSoft = "rgba(0, 140, 255, 0.18)";

	/// <summary>Subtle border between dock regions.</summary>
	public const string BorderSubtle = "rgba(255, 255, 255, 0.06)";

	/// <summary>Stronger border — input field outlines.</summary>
	public const string BorderField = "rgba(255, 255, 255, 0.10)";

	/// <summary>Focused input border.</summary>
	public const string BorderFocus = "rgb(0, 140, 255)";

	/// <summary>Brand/accent — used for active tab underline, focus ring.</summary>
	public const string Accent = "rgb(0, 140, 255)";

	/// <summary>Primary text on dark backgrounds.</summary>
	public const string TextPrimary = "rgb(225, 228, 232)";

	/// <summary>Secondary/label text.</summary>
	public const string TextMuted = "rgb(150, 156, 165)";

	/// <summary>Section header text — small caps style.</summary>
	public const string TextHeader = "rgb(165, 172, 182)";

	/// <summary>Disabled text.</summary>
	public const string TextDisabled = "rgb(95, 100, 108)";

	// ── Spacing (px) — used in Layout.Margin / Spacing ────────────────────

	public const int SpaceXs = 2;
	public const int SpaceSm = 4;
	public const int SpaceMd = 6;
	public const int SpaceLg = 10;
	public const int SpaceXl = 16;

	// ── Sizing ────────────────────────────────────────────────────────────

	public const int ToolbarHeight = 40;
	public const int FieldHeight = 22;
	public const int RowHeight = 24;
	public const int TabHeight = 32;
	public const int IconSize = 16;
	public const int RadiusSm = 3;
	public const int RadiusMd = 4;

	// ── Typography ─────────────────────────────────────────────────────────

	public const string FontStackUi = "font-family: 'Inter', 'Segoe UI', sans-serif;";
	public const int FontSizeSm = 11;
	public const int FontSizeMd = 12;
	public const int FontSizeLg = 13;

	// ── Pre-built style strings (paste-ready into SetStyles) ───────────────

	/// <summary>Top window toolbar bar.</summary>
	public static string ToolbarBar =>
		$"background-color: {BgSurface};" +
		$"border-bottom: 1px solid {BorderSubtle};";

	/// <summary>Toolbar button — label + icon, transparent until hover.</summary>
	public static string ToolbarButton =>
		$"background-color: transparent;" +
		$"color: {TextPrimary};" +
		$"border: none;" +
		$"border-radius: {RadiusSm}px;" +
		$"padding: 4px 10px;" +
		$"font-size: {FontSizeMd}px;";

	public static string ToolbarButtonHover =>
		$"background-color: {BgHover};";

	public static string ToolbarButtonActive =>
		$"background-color: {BgAccentSoft};" +
		$"color: {TextPrimary};";

	/// <summary>Vertical separator between toolbar groups.</summary>
	public static string ToolbarSeparator =>
		$"background-color: {BorderSubtle};" +
		$"min-width: 1px; max-width: 1px;" +
		$"min-height: 18px; max-height: 18px;" +
		$"margin: 0 {SpaceMd}px;";

	/// <summary>Pill-style dropdown button (Snap / Zoom / Screen).</summary>
	public static string PillButton =>
		$"background-color: {BgRaised};" +
		$"color: {TextPrimary};" +
		$"border: 1px solid {BorderField};" +
		$"border-radius: {RadiusMd}px;" +
		$"padding: 3px 8px;" +
		$"font-size: {FontSizeMd}px;";

	/// <summary>LineEdit / dropdown surface.</summary>
	public static string Input =>
		$"background-color: {BgRaised};" +
		$"color: {TextPrimary};" +
		$"border: 1px solid {BorderField};" +
		$"border-radius: {RadiusSm}px;" +
		$"padding: 2px 6px;" +
		$"font-size: {FontSizeMd}px;";

	/// <summary>Section header label (COMMON / TRANSFORM / APPEARANCE).</summary>
	public static string SectionHeader =>
		$"color: {TextHeader};" +
		$"font-size: {FontSizeSm}px;" +
		$"font-weight: 600;" +
		$"letter-spacing: 1px;" +
		$"padding: 4px 0;";

	/// <summary>Property row label (left column).</summary>
	public static string RowLabel =>
		$"color: {TextMuted};" +
		$"font-size: {FontSizeMd}px;";

	/// <summary>Tab strip background — flush with the dock body.</summary>
	public static string TabBar =>
		$"background-color: {BgBase};" +
		$"border-bottom: 1px solid {BorderSubtle};";
}
