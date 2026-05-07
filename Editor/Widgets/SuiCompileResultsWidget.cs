using System.Collections.Generic;
using System.Text;
using Editor;
using Sandbox;
using SboxUiDesigner.Generation;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Compile Results dock — shows the output of the most recent generation pass:
/// file list, errors, warnings, and a preview of each file's contents.
///
/// In M9 the generator runs but does NOT write to disk — this widget is the
/// only surface for the user to inspect output. M12 will add the actual
/// filesystem writer + manifest tracking and this widget will gain a "Write
/// to disk" / conflict resolution UI.
/// </summary>
public class SuiCompileResultsWidget : Widget
{
	private Label _summary;
	private TextEdit _preview;
	private TabWidget _fileTabs;
	private SuiGenerationResult _lastResult;

	public SuiCompileResultsWidget( Widget parent = null ) : base( parent )
	{
		WindowTitle = "Compile Results";
		Name = "SuiCompileResults";

		Layout = Layout.Column();
		Layout.Margin = 8;
		Layout.Spacing = 6;

		_summary = new Label( "(compile not run yet)", this );
		_summary.WordWrap = true;
		_summary.SetStyles( "color: #9ca3af; font-size: 11px;" );
		Layout.Add( _summary );

		_fileTabs = new TabWidget( this );
		Layout.Add( _fileTabs, 1 );

		// One single 'Output' tab pre-built; on each compile we replace its
		// contents. (TabWidget supports multiple tabs but for M9 keep it
		// simple and switch the rendered file via a dropdown above the
		// preview if needed — for now a single tab + summary is enough.)
		var initial = new Widget( null );
		initial.Layout = Layout.Column();
		initial.Layout.Margin = 6;

		_preview = new TextEdit( initial );
		_preview.PlainText = "// Run Compile to see generated output here.\n// M9 generates content; M12 writes to disk.";
		_preview.ReadOnly = true;
		initial.Layout.Add( _preview, 1 );

		_fileTabs.AddPage( "Output", "description", initial );
	}

	/// <summary>
	/// Render the most recent generation result — file list summary in the
	/// header and the first file's content in the preview pane.
	/// </summary>
	public void DisplayResult( SuiGenerationResult result )
	{
		_lastResult = result;
		if ( result == null )
		{
			_summary.Text = "(no result)";
			return;
		}

		var sb = new StringBuilder();
		sb.Append( result.Ok ? "✓ Compile OK — " : "✗ Compile failed — " );
		sb.Append( result.Files.Count ).Append( " file" ).Append( result.Files.Count == 1 ? "" : "s" );
		if ( result.Errors.Count > 0 ) sb.Append( ", " ).Append( result.Errors.Count ).Append( " error" ).Append( result.Errors.Count == 1 ? "" : "s" );
		if ( result.Warnings.Count > 0 ) sb.Append( ", " ).Append( result.Warnings.Count ).Append( " warning" ).Append( result.Warnings.Count == 1 ? "" : "s" );
		sb.AppendLine();

		foreach ( var f in result.Files )
		{
			sb.Append( "  • " ).Append( f.Path ).Append( " (sha256 " ).Append( f.Sha256.Substring( 0, 12 ) ).AppendLine( "...)" );
		}

		if ( result.Errors.Count > 0 )
		{
			sb.AppendLine();
			sb.AppendLine( "Errors:" );
			foreach ( var e in result.Errors ) sb.Append( "  • " ).AppendLine( e );
		}

		if ( result.Warnings.Count > 0 )
		{
			sb.AppendLine();
			sb.AppendLine( "Warnings:" );
			foreach ( var w in result.Warnings ) sb.Append( "  • " ).AppendLine( w );
		}

		_summary.Text = sb.ToString();

		// Preview the first file's content. A future iteration can let the
		// user pick which file via a dropdown; for M9 just show the razor.
		var first = result.Files.Count > 0 ? result.Files[0] : null;
		if ( first != null && _preview != null )
		{
			_preview.PlainText = first.Content ?? "";
		}
	}
}
