using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Logs tab — placeholder for now (V1 just shows a hint pointing to the engine
/// console). V2 would intercept Log.Info/Warning/Error filtering by the
/// "[Sui ...]" prefix and render a timeline here.
/// </summary>
public sealed class SuiLogsWidget : Widget
{
	public SuiLogsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Logs";
		Name = "SuiLogs";

		Layout = Layout.Column();
		Layout.Margin = 8;
		Layout.Spacing = 6;

		var hint = new Label( "Designer logs are written to the engine console.\nLook for `[Sui ...]` prefixes.\n\nFiltered timeline view is V2 — for now use the engine Console panel.", this );
		hint.WordWrap = true;
		hint.SetStyles( "color: #9ca3af; font-size: 11px;" );
		Layout.Add( hint );
		Layout.AddStretchCell();
	}
}
