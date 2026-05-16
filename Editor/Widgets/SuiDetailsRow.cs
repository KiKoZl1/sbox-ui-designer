using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// One row inside the Details panel — left-aligned label (~38% width) + a
/// control filling the rest. Fixed height for consistent rhythm.
/// </summary>
public sealed class SuiDetailsRow : Widget
{
	public string Label { get; }
	public Widget Control { get; }

	private const int LabelWidth = 100;
	private const int RowHeight = 26;

	public SuiDetailsRow( string label, Widget control, Widget parent = null ) : base( parent )
	{
		Label = label ?? "";
		Control = control;
		FixedHeight = RowHeight;
		SetStyles( "background-color: transparent; border: none;" );

		Layout = Layout.Row();
		Layout.Margin = new Sandbox.UI.Margin( 0, 2, 0, 2 );
		Layout.Spacing = 8;

		var labelW = new SuiDetailsRowLabel( label );
		labelW.FixedWidth = LabelWidth;
		Layout.Add( labelW );

		if ( control != null )
		{
			control.Parent = this;
			Layout.Add( control, 1 );
		}
	}
}

internal sealed class SuiDetailsRowLabel : Widget
{
	public string Text { get; }

	/// <summary>
	/// When true, a small link icon is painted before the label text — signals
	/// that the property on this row is bound to a Variable. Set by
	/// <c>SuiDetailsWidget.AddRowLabel</c> after consulting the element's
	/// <c>Bindings</c> list.
	/// </summary>
	public bool ShowBindIcon { get; set; }

	private const int BindIconSize = 12;
	private const int BindIconSlot = 16; // icon + 4px gap

	public SuiDetailsRowLabel( string text, Widget parent = null ) : base( parent )
	{
		Text = text ?? "";
		FixedHeight = 26;
		SetStyles( "background-color: transparent; border: none;" );
	}

	protected override void OnPaint()
	{
		var textOffset = 0f;
		if ( ShowBindIcon )
		{
			// Purple accent matches the V1.5 binding visual language (popup ring,
			// chain step badges in SuiBindPopup). Material icon "link".
			Paint.SetPen( new Color( 167 / 255f, 139 / 255f, 250 / 255f ) );
			var iconY = (Height - BindIconSize) / 2f;
			Paint.DrawIcon( new Rect( 0, iconY, BindIconSize, BindIconSize ), "link", BindIconSize );
			textOffset = BindIconSlot;
		}

		Paint.SetDefaultFont( 10 );
		Paint.SetPen( new Color( 165 / 255f, 170 / 255f, 178 / 255f ) );
		Paint.DrawText( new Rect( textOffset, 0, Width - textOffset, Height ), Text, TextFlag.LeftCenter );
	}
}
