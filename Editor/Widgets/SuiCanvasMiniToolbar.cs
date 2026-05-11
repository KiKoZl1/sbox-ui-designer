using System;
using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Mini-toolbar that lives directly under the Designer/Preview/Code tab strip.
/// Holds canvas-scoped controls (Screen / Zoom / Snap dropdowns).
///
/// Reuses <see cref="SuiTopBarDropdown"/> for visual consistency with the
/// top toolbar — same pill style, same chevron, same paint-only widget.
///
/// Alignment / cursor / marquee / fit / lock buttons (yellow-marked in the
/// mockup) are V2 — left out for now.
/// </summary>
public sealed class SuiCanvasMiniToolbar : Widget
{
	public SuiCanvasMiniToolbar( Widget parent = null ) : base( parent )
	{
		FixedHeight = 44;

		Layout = Layout.Row();
		Layout.Margin = new Sandbox.UI.Margin( 12, 6, 12, 6 );
		Layout.Spacing = 4;
	}

	public SuiTopBarDropdown AddDropdown( string icon, string label, string value, Action<Widget> onOpen, string tooltip = null )
	{
		var dd = new SuiTopBarDropdown( icon, label, value );
		dd.ToolTip = tooltip;
		dd.Clicked += () => onOpen?.Invoke( dd );
		Layout.Add( dd );
		return dd;
	}

	public SuiTopBarButton AddButton( string label, string icon, Action onClick, string tooltip = null )
	{
		var btn = new SuiTopBarButton( label, icon, hasChevron: false );
		btn.ToolTip = tooltip ?? label;
		btn.Clicked += onClick;
		Layout.Add( btn );
		return btn;
	}

	/// <summary>Button styled like a dropdown — chevron on the right, click
	/// opens a context menu. Used for grouped ops (e.g. Align, Distribute).</summary>
	public SuiTopBarButton AddMenuButton( string label, string icon, Action<Widget> onOpen, string tooltip = null )
	{
		var btn = new SuiTopBarButton( label, icon, hasChevron: true );
		btn.ToolTip = tooltip ?? label;
		btn.Clicked += () => onOpen?.Invoke( btn );
		Layout.Add( btn );
		return btn;
	}

	public void AddSeparator()
	{
		var sep = new SuiTopBarSeparator();
		Layout.Add( sep );
	}

	public void AddGap( int px = 12 )
	{
		var spacer = new Widget( this );
		spacer.FixedWidth = px;
		spacer.SetStyles( "background-color: transparent;" );
		Layout.Add( spacer );
	}

	public void AddStretch() => Layout.AddStretchCell();

	protected override void OnPaint()
	{
		// Mini-toolbar background — #1E1F1E (rgba 30,31,30) per user spec.
		Paint.SetBrushAndPen( new Color( 30 / 255f, 31 / 255f, 30 / 255f ) );
		Paint.DrawRect( LocalRect );

		// Subtle 1px bottom border to visually separate from canvas viewport.
		Paint.SetPen( Color.White.WithAlpha( 0.06f ) );
		Paint.DrawLine( new Vector2( 0, Height - 1 ), new Vector2( Width, Height - 1 ) );
	}
}
