using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Animations dock — placeholder for V2. Registered as its own DockManager
/// dock so it gets a native dock tab in the bottom area; the schema already
/// reserves space for animation tracks per element (see SuiAnimationData).
/// </summary>
public class SuiAnimationsWidget : Widget
{
	public SuiAnimationsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Animations";
		Name = "SuiAnimations";

		Layout = Layout.Column();
		Layout.Margin = 12;

		var msg = new Label( "Animations are planned for V2.\n\nThe schema reserves space for animation tracks per element\n(.sui field 'animations'); UI lands later.", this );
		msg.WordWrap = true;
		msg.SetStyles( "color: #6b7280; font-size: 11px;" );
		Layout.Add( msg );
		Layout.AddStretchCell();
	}
}
