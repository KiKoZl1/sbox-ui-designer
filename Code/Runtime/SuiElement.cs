using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// One node in a .sui document tree. The full document is a flat list of these
/// — parent/child relationships are stored via <see cref="ParentId"/> and
/// <see cref="Children"/>. The validator keeps both in sync.
/// </summary>
public sealed class SuiElement
{
	/// <summary>Stable internal id. Generated at element creation, never changed on rename.</summary>
	public string Id { get; set; }

	/// <summary>User-facing name, shown in the Hierarchy widget.</summary>
	public string Name { get; set; }

	public SuiElementType Type { get; set; }

	/// <summary>Parent element id. Null for the root canvas.</summary>
	public string ParentId { get; set; }

	/// <summary>Ordered list of child element ids. Mirrors the document tree order.</summary>
	public List<string> Children { get; set; } = new();

	public SuiElementFlags Flags { get; set; } = new();
	public SuiLayoutData Layout { get; set; } = new();
	public SuiStyleData Style { get; set; } = new();
	public SuiElementProps Props { get; set; } = new();

	/// <summary>Optional designer-only notes attached to the element.</summary>
	public string Notes { get; set; }

	/// <summary>Tooltip shown when the user hovers the element at runtime.</summary>
	public string TooltipText { get; set; }

	/// <summary>True if the element should be reachable from generated C# as a [Property] field.</summary>
	public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Optional widget class override — when set, the generator emits a custom
	/// PanelComponent type instead of the default per-Type behavior. V2 will
	/// surface this as a dropdown of registered classes; V1 is free-text.
	/// </summary>
	public string ClassOverride { get; set; }

	/// <summary>
	/// Optional reusable style reference — name of a shared style block
	/// applied to this element. V2 will surface this as a dropdown of styles
	/// defined in the document; V1 is free-text.
	/// </summary>
	public string StyleRef { get; set; }

	public SuiElement Clone() => new()
	{
		Id = Id,
		Name = Name,
		Type = Type,
		ParentId = ParentId,
		Children = new List<string>( Children ?? new() ),
		Flags = Flags?.Clone() ?? new(),
		Layout = Layout?.Clone() ?? new(),
		Style = Style?.Clone() ?? new(),
		Props = Props?.Clone() ?? new(),
		Notes = Notes,
		TooltipText = TooltipText,
		IsVisible = IsVisible,
		ClassOverride = ClassOverride,
		StyleRef = StyleRef,
	};

	/// <summary>
	/// Apply the per-element-type defaults (pointer-events, etc.) to a freshly
	/// created element. Called by the controller when adding a new element via
	/// the Palette so the user doesn't have to flip these manually.
	/// </summary>
	public void ApplyTypeDefaults()
	{
		// Per PRD doc 07: interactive elements default to PointerEvents.All;
		// passive elements default to None (matches runtime default).
		Style.PointerEvents = Type switch
		{
			SuiElementType.Button => SuiPointerEvents.All,
			SuiElementType.InventorySlot => SuiPointerEvents.All,
			SuiElementType.ScrollPanel => SuiPointerEvents.All,
			_ => SuiPointerEvents.None,
		};

		// Boxes + grids use Flex layout by default, everything else starts Absolute.
		// Grids (Grid, InventoryGrid) need flex+wrap or children stack at (0,0)
		// — the canvas solver doesn't have a dedicated grid-pass, but flex-row+wrap
		// produces the correct visual since the SCSS generator already maps Grid
		// to wrapped-flex (PRD doc 08 strategy A).
		Layout.Mode = Type switch
		{
			SuiElementType.HorizontalBox
				or SuiElementType.VerticalBox
				or SuiElementType.Hotbar
				or SuiElementType.Grid
				or SuiElementType.InventoryGrid
				=> SuiLayoutMode.Flex,
			_ => SuiLayoutMode.Absolute,
		};

		// Boxes pick the right flex direction.
		Layout.FlexDirection = Type switch
		{
			SuiElementType.VerticalBox => SuiFlexDirection.Column,
			SuiElementType.HorizontalBox
				or SuiElementType.Hotbar
				or SuiElementType.Grid
				or SuiElementType.InventoryGrid
				=> SuiFlexDirection.Row,
			_ => Layout.FlexDirection,
		};

		// Grid + InventoryGrid wrap on overflow so multi-row layouts work.
		// Hotbar is a single row of fixed slots → no wrap.
		Layout.FlexWrap = Type switch
		{
			SuiElementType.Grid or SuiElementType.InventoryGrid => SuiFlexWrap.Wrap,
			SuiElementType.Hotbar => SuiFlexWrap.NoWrap,
			_ => Layout.FlexWrap,
		};

		// Hotbar implies a single row.
		if ( Type == SuiElementType.Hotbar )
		{
			Props.Rows = 1;
		}

		// Default placeholder content so newly-created text elements are
		// immediately visible on the canvas (and the user has something to
		// type over). Only applied when the field is empty so existing
		// elements keep their content on Clone() / re-defaulting.
		switch ( Type )
		{
			case SuiElementType.Text:
				if ( string.IsNullOrEmpty( Props.Text ) ) Props.Text = "Text";
				break;
			case SuiElementType.Button:
				if ( string.IsNullOrEmpty( Props.ButtonText ) ) Props.ButtonText = "Button";
				break;
		}
	}
}
