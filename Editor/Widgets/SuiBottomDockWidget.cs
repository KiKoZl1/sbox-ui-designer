using Editor;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Bottom dock — split between Animations (V2 placeholder) and Compile Results.
///
/// M4: visual structure only.
/// M12: Compile Results gets populated after each compile run.
/// V2: Animations tab gets a real timeline.
/// </summary>
public class SuiBottomDockWidget : Widget
{
	private TabWidget _tabs;
	private Widget _animationsTab;
	private Widget _compileResultsTab;
	private Label _compileResultsLabel;

	public SuiBottomDockWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Bottom Dock";
		Name = "SuiBottomDock";
		MinimumSize = new Vector2( 400, 120 );

		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		_tabs = new TabWidget( this );
		Layout.Add( _tabs, 1 );

		BuildAnimationsTab();
		BuildCompileResultsTab();
	}

	private void BuildAnimationsTab()
	{
		_animationsTab = new Widget( null );
		_animationsTab.Layout = Layout.Column();
		_animationsTab.Layout.Margin = 12;

		var msg = new Label( "Animations are planned for V2.\n\nThe schema reserves space for animation tracks per element\n(.sui field 'animations'); UI lands later.", _animationsTab );
		msg.WordWrap = true;
		msg.SetStyles( "color: #6b7280; font-size: 11px; text-align: center;" );
		_animationsTab.Layout.Add( msg );

		// TabWidget.AddPage signature: (name, icon, page).
		_tabs.AddPage( "Animations", "movie", _animationsTab );
	}

	private void BuildCompileResultsTab()
	{
		_compileResultsTab = new Widget( null );
		_compileResultsTab.Layout = Layout.Column();
		_compileResultsTab.Layout.Margin = 12;

		_compileResultsLabel = new Label( "(compile not run yet — M12 wires this)", _compileResultsTab );
		_compileResultsLabel.WordWrap = true;
		_compileResultsLabel.SetStyles( "color: #9ca3af; font-size: 11px;" );
		_compileResultsTab.Layout.Add( _compileResultsLabel );
		_compileResultsTab.Layout.AddStretchCell();

		_tabs.AddPage( "Compile Results", "build", _compileResultsTab );
	}

	/// <summary>Replace the Compile Results body with a fresh report — M12 will call this.</summary>
	public void SetCompileResultsText( string text )
	{
		if ( _compileResultsLabel != null )
			_compileResultsLabel.Text = text ?? "";
	}
}
