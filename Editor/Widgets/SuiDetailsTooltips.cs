using System.Collections.Generic;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Centralised hover tooltip text for every row in the Details panel. Each
/// entry is keyed by the row's label (lowercased, trimmed) and explains
/// "what it is" + "when to use" in 1–2 short sentences. The pattern mirrors
/// the per-type tooltips in the Palette.
///
/// AddRowLabel reads from this map automatically — adding a new tooltip is
/// just a new dictionary entry; no per-call wiring needed.
/// </summary>
internal static class SuiDetailsTooltips
{
	public static readonly Dictionary<string, string> Map = new()
	{
		// COMMON
		["name"] = "Element identifier — shown in the Hierarchy and used as the variable name when code references this element (e.g. HealthBar.Value).",
		["tooltip text"] = "Text shown when the player hovers this element in-game.\nUse for stat explanations, ability descriptions, item lore.",

		// TRANSFORM
		["mode"] = "Layout mode.\nAbsolute = explicit X/Y/W/H. Flex = browser-style flexbox flow. Most game HUDs use Absolute.",
		["position"] = "Offset from the anchor reference point, in pixels (logical 1920×1080 space).\nNegative values move toward the opposite edge.",
		["size"] = "Element width and height in pixels (logical 1920×1080 space).",
		["anchor"] = "Reference point on the parent that this element snaps to.\nWhen the parent resizes, the element repositions relative to this anchor (e.g. TopRight stays glued to the parent's top-right corner).",
		["pivot"] = "Origin point inside the element itself, as a 0..1 fraction.\n(0,0) = top-left, (0.5,0.5) = center, (1,1) = bottom-right. Affects rotation and the meaning of Position.",
		["z index"] = "Render order within the same parent. Higher draws on top, lower draws under.\nUse to keep tooltips/badges above icons.",
		["margin"] = "Outer offsets used when Anchor is a Stretch variant.\nLeft / Top / Right / Bottom space between this element and the parent edges.",
		["padding"] = "Inner offsets — pushes child elements inward from this element's edges.\nUse to add breathing room without shrinking child boxes manually.",

		// APPEARANCE
		["background"] = "Solid fill color of the element.\nClick to pick · right-click for clear / copy / paste.",
		["border"] = "Stroke color drawn around the element.\nVisible only when Border Width is greater than 0.",
		["border width"] = "Stroke thickness in pixels. 0 = no border.",
		["border radius"] = "Corner rounding in pixels. 0 = sharp corners.\nUse for pill buttons, soft cards, rounded panels.",
		["opacity"] = "0..1 transparency. 0 = fully transparent, 1 = fully opaque.\nAffects all child elements too.",
		["visibility"] = "Visible = normal.\nHidden = invisible but still occupies layout space.\nCollapsed = removed from layout entirely.",
		["pointer events"] = "None = ignores clicks and hovers (input passes through to elements below).\nAll = catches input. Use None on decorative overlays.",
		["overflow"] = "Visible = children can render outside this element's bounds.\nHidden / Clip = children clipped at the edges (use for ScrollPanels and masked content).",

		// IMAGE
		["image path"] = "Path to the image asset, relative to the project's Assets folder.\nClick the folder button to browse.",
		["tint"] = "Color multiplied with the image's pixels. White = no tint.\nUse to recolor sprites, dim icons, flash on damage.",
		["fit mode"] = "Stretch = fill regardless of aspect ratio.\nContain = fit inside, keep ratio (may letterbox).\nCover = fill, keep ratio (may crop).\nNone = use original size.",
		["background position"] = "Where the image sits inside its bounds when Fit Mode leaves empty space.\nCenter / Left / Right / Top / Bottom / corners.",

		// TEXT
		["text"] = "Literal text shown by this element.\nUse {placeholders} for dynamic values driven by bindings.",
		["font family"] = "Font face name (e.g. Inter, Roboto). Leave empty to use the project default.",
		["color"] = "Text color.",
		["align"] = "Horizontal alignment of text within the element.",
		["vertical align"] = "Vertical alignment of text within the element.",
		["auto wrap text"] = "When on, lines wrap to fit the element width.\nWhen off, text stays on one line and may overflow.",
		["wrap text at"] = "Maximum width before wrapping. Used when Auto Wrap Text is on.",

		// BUTTON
		["button text"] = "Caption shown inside the button.",

		// GRID
		["columns"] = "Number of vertical lanes — children pack left-to-right, wrapping every Columns items.",
		["rows"] = "Number of horizontal rows. Combined with Columns determines total slot count.",
		["cell width"] = "Width of each grid slot in pixels.",
		["cell height"] = "Height of each grid slot in pixels.",
		["grid gap"] = "Spacing between slots in pixels.",
		["auto fill"] = "When on, the grid fills its container with as many cells as fit, using Cell Width / Height as the grid step.",

		// PROGRESSBAR
		["value"] = "Current fill amount, 0..1.\nBind to a runtime value like Player.Health / Player.MaxHealth.",
		["max value"] = "Upper bound for the value. Most code paths normalize to 1 (Value 0..1).",
		["direction"] = "Direction the bar fills as the value grows.\nLeft to Right / Right to Left / Top to Bottom / Bottom to Top.",
		["fill color"] = "Color of the filled portion of the bar.",

		// INVENTORY
		["slot index"] = "Position in the parent InventoryGrid (0-based). Set automatically by the grid layout.",

		// BINDING
		["is variable"] = "Exposes this element as a code-accessible field on the generated PanelComponent.\nUse so scripts can call e.g. HealthBar.SetValue(0.7f).",
		["isbound"] = "Toggle on to attach a runtime data path to this element's main property.",
		["binding source"] = "The object that owns the value (e.g. Player, Weapon, Inventory).",
		["binding mode"] = "OneWay = data flows to UI.\nTwoWay = UI and data sync both ways (use for input fields).\nOneTime = read once at mount.",
		["binding path"] = "Property path on the source — e.g. Health, CurrentAmmo, Inventory.Items.",
		["property"] = "Which property of THIS element receives the value (Value for ProgressBar, Text for Label, Items for InventoryGrid, etc).",
	};

	public static string Lookup( string label )
	{
		if ( string.IsNullOrEmpty( label ) ) return null;
		var key = label.Trim().ToLowerInvariant();
		return Map.TryGetValue( key, out var tip ) ? tip : null;
	}
}
