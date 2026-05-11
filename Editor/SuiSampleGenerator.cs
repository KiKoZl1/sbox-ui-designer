using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Generates the canonical sample .sui documents (PRD doc 14 § Sample projects).
/// Used by the Tools menu "Install Samples" action — produces fully-formed
/// <see cref="SuiDocument"/> instances the user can save as assets.
///
/// Each sample exercises a different slice of the schema:
/// - <see cref="BuildSimplePanel"/> — anchor/pivot, panel + text basics
/// - <see cref="BuildInventoryBasic"/> — InventoryGrid + InventorySlot composition
/// - <see cref="BuildHotbarBasic"/> — Hotbar (HorizontalBox flex) + ItemIcon
/// - <see cref="BuildHudSurvival"/> — composite HUD: ProgressBar + Text + Image
/// </summary>
public static class SuiSampleGenerator
{
	public static IReadOnlyList<(string Name, SuiDocument Doc)> All()
	{
		return new List<(string, SuiDocument)>
		{
			("simple_panel", BuildSimplePanel()),
			("inventory_basic", BuildInventoryBasic()),
			("hotbar_basic", BuildHotbarBasic()),
			("hud_survival", BuildHudSurvival()),
		};
	}

	// ─────────────────────────────────────────────────────────────────────
	//  simple_panel — minimal panel + centered text
	// ─────────────────────────────────────────────────────────────────────
	public static SuiDocument BuildSimplePanel()
	{
		var doc = SuiDocument.CreateDefault( "simple_panel" );
		doc.Output.ClassName = "SimplePanel";

		var root = doc.GetRoot();
		var panel = AddChild( doc, root, SuiElementType.Panel, "Panel", absolute: true );
		panel.Layout.Width = 480;
		panel.Layout.Height = 240;
		panel.Layout.Anchor = SuiAnchor.MiddleCenter;
		panel.Layout.PivotX = 0.5f;
		panel.Layout.PivotY = 0.5f;
		panel.Style.BackgroundColor = "#1f2937";
		panel.Style.BorderColor = "#3b82f6";
		panel.Style.BorderWidth = 2;
		panel.Style.BorderRadius = 6;

		var text = AddChild( doc, panel, SuiElementType.Text, "Title", absolute: true );
		text.Layout.Width = 400;
		text.Layout.Height = 32;
		text.Layout.Anchor = SuiAnchor.MiddleCenter;
		text.Layout.PivotX = 0.5f;
		text.Layout.PivotY = 0.5f;
		text.Props.Text = "Hello, s&box UI Designer!";
		text.Props.FontSize = 24;
		text.Props.FontWeight = SuiFontWeight.Bold;
		text.Props.Color = "#e5e7eb";
		text.Props.TextAlign = SuiTextAlign.Center;

		return doc;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  inventory_basic — 5×3 grid of inventory slots
	// ─────────────────────────────────────────────────────────────────────
	public static SuiDocument BuildInventoryBasic()
	{
		var doc = SuiDocument.CreateDefault( "inventory_basic" );
		doc.Output.ClassName = "InventoryBasic";

		var root = doc.GetRoot();
		var window = AddChild( doc, root, SuiElementType.Panel, "InventoryWindow", absolute: true );
		window.Layout.Width = 540;
		window.Layout.Height = 360;
		window.Layout.Anchor = SuiAnchor.MiddleCenter;
		window.Layout.PivotX = 0.5f;
		window.Layout.PivotY = 0.5f;
		window.Style.BackgroundColor = "#0b1220";
		window.Style.BorderColor = "#475569";
		window.Style.BorderWidth = 2;
		window.Style.BorderRadius = 8;

		var title = AddChild( doc, window, SuiElementType.Text, "Title", absolute: true );
		title.Layout.X = 0;
		title.Layout.Y = 8;
		title.Layout.Width = 540;
		title.Layout.Height = 32;
		title.Layout.Anchor = SuiAnchor.TopCenter;
		title.Layout.PivotX = 0.5f;
		title.Props.Text = "Inventory";
		title.Props.FontSize = 20;
		title.Props.FontWeight = SuiFontWeight.SemiBold;
		title.Props.Color = "#e5e7eb";
		title.Props.TextAlign = SuiTextAlign.Center;

		// InventoryGrid is flex+row+wrap by default (ApplyTypeDefaults), so
		// children flow into a grid. Width allows 5 slots @ 80px + 4 gaps @ 12px = 448 px.
		var grid = AddChild( doc, window, SuiElementType.InventoryGrid, "Slots", absolute: false );
		grid.Layout.Width = 448;
		grid.Layout.Height = 280;
		grid.Layout.X = 20;
		grid.Layout.Y = 56;
		grid.Layout.Anchor = SuiAnchor.TopLeft;
		grid.Layout.Gap = 12;
		grid.Props.Columns = 5;
		grid.Props.Rows = 3;
		grid.Props.CellWidth = 80;
		grid.Props.CellHeight = 80;
		grid.Props.GridGap = 12;

		// 15 slots in a 5×3 grid (flex layout — children inherit cell sizes from grid props)
		for ( int i = 0; i < 15; i++ )
		{
			var slot = AddChild( doc, grid, SuiElementType.InventorySlot, $"Slot_{i + 1}", absolute: false );
			slot.Layout.Width = 80;
			slot.Layout.Height = 80;
			slot.Style.BackgroundColor = "#1e293b";
			slot.Style.BorderColor = "#475569";
			slot.Style.BorderWidth = 1;
			slot.Style.BorderRadius = 4;
		}

		return doc;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  hotbar_basic — bottom-anchored row of 9 slots
	// ─────────────────────────────────────────────────────────────────────
	public static SuiDocument BuildHotbarBasic()
	{
		var doc = SuiDocument.CreateDefault( "hotbar_basic" );
		doc.Output.ClassName = "HotbarBasic";

		var root = doc.GetRoot();
		var hotbar = AddChild( doc, root, SuiElementType.Hotbar, "Hotbar", absolute: false );
		hotbar.Layout.Mode = SuiLayoutMode.Flex;
		hotbar.Layout.FlexDirection = SuiFlexDirection.Row;
		hotbar.Layout.JustifyContent = SuiJustifyContent.Center;
		hotbar.Layout.AlignItems = SuiAlignItems.Center;
		hotbar.Layout.Gap = 8;
		hotbar.Layout.Width = 800;
		hotbar.Layout.Height = 80;
		hotbar.Layout.Anchor = SuiAnchor.BottomCenter;
		hotbar.Layout.Y = 24;
		hotbar.Layout.PivotX = 0.5f;
		hotbar.Layout.PivotY = 1f;

		for ( int i = 0; i < 9; i++ )
		{
			var slot = AddChild( doc, hotbar, SuiElementType.InventorySlot, $"Slot_{i + 1}", absolute: false );
			slot.Layout.Width = 64;
			slot.Layout.Height = 64;
			slot.Style.BackgroundColor = "#0f172a";
			slot.Style.BorderColor = i == 0 ? "#3b82f6" : "#475569";
			slot.Style.BorderWidth = i == 0 ? 2 : 1;
			slot.Style.BorderRadius = 4;
		}

		return doc;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  hud_survival — top-left HUD with HP/Stamina/Hunger bars + label
	// ─────────────────────────────────────────────────────────────────────
	public static SuiDocument BuildHudSurvival()
	{
		var doc = SuiDocument.CreateDefault( "hud_survival" );
		doc.Output.ClassName = "HudSurvival";

		var root = doc.GetRoot();

		// Container — top-left, vertical stack of bars
		var hud = AddChild( doc, root, SuiElementType.VerticalBox, "HudContainer", absolute: false );
		hud.Layout.Mode = SuiLayoutMode.Flex;
		hud.Layout.FlexDirection = SuiFlexDirection.Column;
		hud.Layout.AlignItems = SuiAlignItems.Stretch;
		hud.Layout.Gap = 8;
		hud.Layout.Width = 280;
		hud.Layout.Height = 120;
		hud.Layout.Anchor = SuiAnchor.TopLeft;
		hud.Layout.X = 32;
		hud.Layout.Y = 32;

		AddBar( doc, hud, "Health", "#ef4444", 100 );
		AddBar( doc, hud, "Stamina", "#22c55e", 75 );
		AddBar( doc, hud, "Hunger", "#f59e0b", 60 );

		return doc;

		static void AddBar( SuiDocument d, SuiElement parent, string label, string fillColor, float value )
		{
			var row = AddChild( d, parent, SuiElementType.HorizontalBox, label + "_Row", absolute: false );
			row.Layout.Mode = SuiLayoutMode.Flex;
			row.Layout.FlexDirection = SuiFlexDirection.Row;
			row.Layout.AlignItems = SuiAlignItems.Center;
			row.Layout.Gap = 8;
			row.Layout.Width = 280;
			row.Layout.Height = 24;

			var lbl = AddChild( d, row, SuiElementType.Text, label + "_Label", absolute: false );
			lbl.Layout.Width = 80;
			lbl.Layout.Height = 24;
			lbl.Props.Text = label;
			lbl.Props.FontSize = 14;
			lbl.Props.FontWeight = SuiFontWeight.SemiBold;
			lbl.Props.Color = "#e5e7eb";
			lbl.Props.TextAlign = SuiTextAlign.Left;

			var bar = AddChild( d, row, SuiElementType.ProgressBar, label + "_Bar", absolute: false );
			bar.Layout.Width = 192;
			bar.Layout.Height = 16;
			bar.Style.BackgroundColor = "#1f2937";
			bar.Style.BorderColor = "#374151";
			bar.Style.BorderWidth = 1;
			bar.Style.BorderRadius = 4;
			bar.Props.ProgressMin = 0;
			bar.Props.ProgressMax = 100;
			bar.Props.ProgressPreviewValue = value;
			bar.Props.ProgressFillColor = fillColor;
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Helper — add a child element under parent with sensible defaults.
	// ─────────────────────────────────────────────────────────────────────
	private static SuiElement AddChild( SuiDocument doc, SuiElement parent, SuiElementType type, string name, bool absolute )
	{
		var el = new SuiElement
		{
			Id = SuiDocument.NewElementId(),
			Name = name,
			Type = type,
			ParentId = parent.Id,
		};
		el.ApplyTypeDefaults();
		if ( absolute ) el.Layout.Mode = SuiLayoutMode.Absolute;
		el.Style.ClassName = SuiDocumentValidator.SanitizeClassName( name );

		parent.Children.Add( el.Id );
		doc.Elements.Add( el );
		return el;
	}
}
