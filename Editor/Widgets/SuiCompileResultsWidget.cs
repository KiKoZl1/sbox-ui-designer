using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Compile Results dock — placeholder until M12 wires the real compile pipeline.
/// Registered as its own DockManager dock so it tabs naturally with the
/// Animations dock at the bottom of the designer.
/// </summary>
public class SuiCompileResultsWidget : Widget
{
	private Label _bodyLabel;

	public SuiCompileResultsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Compile Results";
		Name = "SuiCompileResults";

		Layout = Layout.Column();
		Layout.Margin = 12;

		_bodyLabel = new Label( "(compile not run yet — M12 wires this)", this );
		_bodyLabel.WordWrap = true;
		_bodyLabel.SetStyles( "color: #9ca3af; font-size: 11px;" );
		Layout.Add( _bodyLabel );
		Layout.AddStretchCell();
	}

	/// <summary>Replace the body text with a fresh report — M12 calls this on each compile.</summary>
	public void SetText( string text )
	{
		if ( _bodyLabel != null )
			_bodyLabel.Text = text ?? "";
	}
}
