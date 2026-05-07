using System.Collections.Generic;
using Editor;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Palette dock — left side. Lists element types the user can add to the document.
///
/// M4: visual structure only — buttons are inert placeholders.
/// M6: wires drag-drop / click-to-add into the document via SuiDesignerController.
/// </summary>
public class SuiPaletteWidget : Widget
{
	public SuiPaletteWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Palette";
		Name = "SuiPalette";
		MinimumSize = new Vector2( 200, 200 );

		Layout = Layout.Column();
		Layout.Margin = 6;
		Layout.Spacing = 4;

		BuildCategory( "Common", new[]
		{
			SuiElementType.Panel,
			SuiElementType.Text,
			SuiElementType.Image,
			SuiElementType.Button,
		} );

		BuildCategory( "Layout", new[]
		{
			SuiElementType.HorizontalBox,
			SuiElementType.VerticalBox,
			SuiElementType.Grid,
			SuiElementType.Overlay,
		} );

		BuildCategory( "Game UI (V1)", new[]
		{
			SuiElementType.ProgressBar,
			SuiElementType.ScrollPanel,
			SuiElementType.InventoryGrid,
			SuiElementType.InventorySlot,
			SuiElementType.ItemIcon,
			SuiElementType.Tooltip,
			SuiElementType.Hotbar,
		} );

		Layout.AddStretchCell();
	}

	/// <summary>Raised when the user clicks a palette item — wired in M6 by the controller.</summary>
	public event System.Action<SuiElementType> ElementRequested;

	private void BuildCategory( string title, IEnumerable<SuiElementType> types )
	{
		var header = new Label( title.ToUpperInvariant(), this );
		header.SetStyles( "color: #9ca3af; font-size: 10px; font-weight: bold; padding-top: 6px;" );
		Layout.Add( header );

		foreach ( var type in types )
		{
			var btn = new Button( type.ToString(), IconFor( type ), this );
			btn.ToolTip = $"Add a {type} element (click; M6 enables drag-drop)";
			var captured = type;
			btn.Clicked += () => ElementRequested?.Invoke( captured );
			Layout.Add( btn );
		}
	}

	private static string IconFor( SuiElementType type ) => type switch
	{
		SuiElementType.Canvas => "crop_free",
		SuiElementType.Panel => "crop_square",
		SuiElementType.Overlay => "layers",
		SuiElementType.Text => "title",
		SuiElementType.Image => "image",
		SuiElementType.Button => "smart_button",
		SuiElementType.HorizontalBox => "view_week",
		SuiElementType.VerticalBox => "view_agenda",
		SuiElementType.Grid => "grid_on",
		SuiElementType.ScrollPanel => "swap_vert",
		SuiElementType.ProgressBar => "linear_scale",
		SuiElementType.InventoryGrid => "grid_view",
		SuiElementType.InventorySlot => "check_box_outline_blank",
		SuiElementType.ItemIcon => "category",
		SuiElementType.Tooltip => "info",
		SuiElementType.Hotbar => "view_carousel",
		_ => "extension",
	};
}
