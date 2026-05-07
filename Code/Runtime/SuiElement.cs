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

		// Boxes use Flex layout by default, everything else starts Absolute.
		Layout.Mode = Type switch
		{
			SuiElementType.HorizontalBox or SuiElementType.VerticalBox or SuiElementType.Hotbar
				=> SuiLayoutMode.Flex,
			_ => SuiLayoutMode.Absolute,
		};

		// Boxes pick the right flex direction.
		Layout.FlexDirection = Type switch
		{
			SuiElementType.VerticalBox => SuiFlexDirection.Column,
			SuiElementType.HorizontalBox or SuiElementType.Hotbar => SuiFlexDirection.Row,
			_ => Layout.FlexDirection,
		};

		// Hotbar implies a wrapped row of fixed-size slots.
		if ( Type == SuiElementType.Hotbar )
		{
			Layout.FlexWrap = SuiFlexWrap.NoWrap;
			Props.Rows = 1;
		}
	}
}
