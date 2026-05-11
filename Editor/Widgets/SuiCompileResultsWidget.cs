using System.Collections.Generic;
using System.Text;
using Editor;
using Sandbox;
using SboxUiDesigner.Generation;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Compile Results dock — surfaces the output of the most recent compile pass.
///
/// Two render paths:
/// - <see cref="DisplayResult"/> — generation only (no disk write). Used by
///   M9-era compile preview.
/// - <see cref="DisplayCompileResult"/> — generation + writer report
///   (M12, full disk pipeline). Renders categorised sections.
/// </summary>
public class SuiCompileResultsWidget : Widget
{
	private Label _summary;
	private TextEdit _detail;

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

		_detail = new TextEdit( this );
		_detail.PlainText = "// Run Compile to see the full report here.";
		_detail.ReadOnly = true;
		Layout.Add( _detail, 1 );
	}

	/// <summary>
	/// Generation-only display (no disk write). Used when the user clicks
	/// Compile but no output folder is configured.
	/// </summary>
	public void DisplayResult( SuiGenerationResult result )
	{
		if ( result == null )
		{
			_summary.Text = "(no result)";
			_detail.PlainText = "";
			return;
		}

		var sb = new StringBuilder();
		sb.Append( result.Ok ? "✓ Compile OK — " : "✗ Compile failed — " );
		sb.Append( result.Files.Count ).Append( " file" ).Append( result.Files.Count == 1 ? "" : "s" );
		if ( result.Errors.Count > 0 ) sb.Append( ", " ).Append( result.Errors.Count ).Append( " error" ).Append( result.Errors.Count == 1 ? "" : "s" );
		if ( result.Warnings.Count > 0 ) sb.Append( ", " ).Append( result.Warnings.Count ).Append( " warning" ).Append( result.Warnings.Count == 1 ? "" : "s" );
		_summary.Text = sb.ToString();

		var detail = new StringBuilder();
		AppendList( detail, "Generated (in-memory only)", result.Files, f => f.Path );
		AppendList( detail, "Errors", result.Errors, s => s );
		AppendList( detail, "Warnings", result.Warnings, s => s );
		if ( result.Files.Count > 0 )
		{
			detail.AppendLine();
			detail.AppendLine( "── First file content ──" );
			detail.AppendLine( result.Files[0].Content ?? "" );
		}
		_detail.PlainText = detail.ToString();
	}

	/// <summary>
	/// Full M12 compile result: includes write classification (Generated /
	/// Skipped / Preserved / Conflicts / Obsolete) and backup folder.
	/// </summary>
	public void DisplayCompileResult( SuiGenerationResult generation, SuiCompileResult compile )
	{
		if ( compile == null ) { DisplayResult( generation ); return; }

		var headline = compile.Ok ? "✓ Compile OK" : "✗ Compile failed";
		var sb = new StringBuilder();
		sb.Append( headline );
		sb.Append( $" — Generated {compile.Generated.Count}, Skipped {compile.Skipped.Count}, Preserved {compile.Preserved.Count}" );
		if ( compile.UserOwned.Count > 0 ) sb.Append( $", User {compile.UserOwned.Count}" );
		if ( compile.Conflicts.Count > 0 ) sb.Append( $", ⚠ Conflicts {compile.Conflicts.Count}" );
		if ( compile.Obsolete.Count > 0 ) sb.Append( $", Obsolete {compile.Obsolete.Count}" );
		if ( compile.Errors.Count > 0 ) sb.Append( $", ✗ Errors {compile.Errors.Count}" );
		if ( compile.Warnings.Count > 0 ) sb.Append( $", Warnings {compile.Warnings.Count}" );
		_summary.Text = sb.ToString();

		var d = new StringBuilder();
		d.AppendLine( $"Output folder: {compile.OutputFolder ?? "(unset)"}" );
		if ( compile.BackupFolder != null )
			d.AppendLine( $"Backup folder: {compile.BackupFolder}" );
		d.AppendLine();

		AppendCompileSection( d, "Generated", compile.Generated );
		AppendCompileSection( d, "Preserved (overwritten — prior content backed up)", compile.Preserved, includeBackup: true );
		AppendCompileSection( d, "User-Owned (.User.scss — created if missing, never overwritten)", compile.UserOwned );
		AppendCompileSection( d, "Skipped (unchanged)", compile.Skipped );
		AppendCompileSection( d, "⚠ Conflicts (NOT touched — resolve manually)", compile.Conflicts, includeReason: true );
		AppendCompileSection( d, "Obsolete (in old manifest, not in this generation — review)", compile.Obsolete );

		AppendList( d, "Errors", compile.Errors, s => s );
		AppendList( d, "Warnings", compile.Warnings, s => s );

		_detail.PlainText = d.ToString();
	}

	// ─────────────────────────────────────────────────────────────────────
	//  helpers
	// ─────────────────────────────────────────────────────────────────────

	private static void AppendCompileSection(
		StringBuilder sb, string title,
		List<SuiCompileFileEntry> entries,
		bool includeBackup = false, bool includeReason = false )
	{
		if ( entries == null || entries.Count == 0 ) return;
		sb.Append( "── " ).Append( title ).Append( " (" ).Append( entries.Count ).AppendLine( ") ──" );
		foreach ( var e in entries )
		{
			sb.Append( "  • " ).Append( e.RelativePath );
			if ( !string.IsNullOrEmpty( e.Sha256 ) )
				sb.Append( "  [" ).Append( e.Sha256.Substring( 0, System.Math.Min( 12, e.Sha256.Length ) ) ).Append( "…]" );
			sb.AppendLine();
			if ( includeBackup && !string.IsNullOrEmpty( e.BackupPath ) )
				sb.Append( "      backup: " ).AppendLine( e.BackupPath );
			if ( includeReason && !string.IsNullOrEmpty( e.ConflictReason ) )
				sb.Append( "      reason: " ).AppendLine( e.ConflictReason );
		}
		sb.AppendLine();
	}

	private static void AppendList<T>(
		StringBuilder sb, string title,
		List<T> items, System.Func<T, string> selector )
	{
		if ( items == null || items.Count == 0 ) return;
		sb.Append( "── " ).Append( title ).Append( " (" ).Append( items.Count ).AppendLine( ") ──" );
		foreach ( var i in items )
			sb.Append( "  • " ).AppendLine( selector( i ) );
		sb.AppendLine();
	}
}
