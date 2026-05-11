using Editor;
using Sandbox;
using SboxUiDesigner.Generation;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Read-only code viewer — shows the generated Razor + SCSS for the current
/// document. Updates only when the user clicks Refresh or when the tab is
/// activated (M14 spec: not a live updating editor; it's an inspection tool).
///
/// Code tab editable behaviour is V2 — the .sui document is the source of
/// truth, hand-edits to the generated files would be lost on next compile.
/// </summary>
public sealed class SuiCodeTabWidget : Widget
{
	private SuiDocument _document;
	private TabWidget _innerTabs;
	private TextEdit _razorView;
	private TextEdit _scssView;

	public SuiCodeTabWidget( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		// Top bar with Refresh button.
		var topBar = new Widget( this );
		topBar.Layout = Layout.Row();
		topBar.Layout.Margin = new Sandbox.UI.Margin( 6, 4, 6, 4 );
		topBar.Layout.Spacing = 6;
		topBar.FixedHeight = 32;

		var refreshBtn = new Button( "Refresh", "refresh", topBar );
		refreshBtn.ToolTip = "Re-run the generator and refresh the code view";
		refreshBtn.Clicked += Reload;
		topBar.Layout.Add( refreshBtn );

		var hint = new Label( "Read-only — generated from the .sui document. Click Refresh to re-run.", topBar );
		hint.SetStyles( "color: #9ca3af; font-size: 11px;" );
		topBar.Layout.Add( hint, 1 );

		Layout.Add( topBar );

		// Inner tabs — Razor + SCSS side by side via tabs.
		_innerTabs = new TabWidget( this );

		_razorView = MakeCodeView( "// Click Refresh to generate" );
		_innerTabs.AddPage( ".razor", "code", _razorView );

		_scssView = MakeCodeView( "/* Click Refresh to generate */" );
		_innerTabs.AddPage( ".razor.scss", "style", _scssView );

		Layout.Add( _innerTabs, 1 );
	}

	/// <summary>
	/// Build a TextEdit styled to look like a real IDE pane: monospace font,
	/// 2-character tab stop (in case any \t leaks through the generator),
	/// comfortable line height, dark background slightly different from the
	/// dock body so the code reads as content, soft padding so the first
	/// column isn't glued to the chrome.
	/// </summary>
	private static TextEdit MakeCodeView( string placeholder )
	{
		var view = new TextEdit( null );
		view.PlainText = placeholder;
		view.ReadOnly = true;
		// Generator emits 2-space indents now (see SuiRazorGenerator/SuiScssGenerator),
		// so we don't need any tab-stop tweaks here. Just an IDE-style code surface.
		view.SetStyles(
			"background-color: rgb(15,15,16);" +
			"color: rgb(220,222,228);" +
			"border: none;" +
			"padding: 12px 14px;" +
			"font-family: 'Consolas', 'Cascadia Code', 'Menlo', monospace;" +
			"font-size: 12px;"
		);
		return view;
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		// Don't auto-Reload — runs the full generation pipeline on every doc
		// change. The Code tab regenerates lazily when the user opens it or
		// clicks Refresh inside it.
	}

	public void Reload()
	{
		if ( _document == null )
		{
			_razorView.PlainText = "// (no document loaded)";
			_scssView.PlainText = "/* (no document loaded) */";
			return;
		}

		var ctx = new SuiGenerationContext
		{
			Document = _document,
			Mode = SuiGenerationMode.Final,
			OutputFolder = "",
			ClassName = _document.Output?.ClassName,
			Namespace = _document.Output?.Namespace,
		};
		var result = SuiGenerationPipeline.Run( ctx );

		if ( !result.Ok )
		{
			var errs = string.Join( "\n", result.Errors );
			_razorView.PlainText = $"// Generation failed:\n{errs}";
			_scssView.PlainText = $"/* Generation failed — see .razor tab */";
			return;
		}

		var razor = result.FindByKind( SuiGeneratedFileKind.Razor );
		var scss = result.FindByKind( SuiGeneratedFileKind.Scss );
		_razorView.PlainText = razor?.Content ?? "// (empty)";
		_scssView.PlainText = scss?.Content ?? "/* (empty) */";
	}
}
