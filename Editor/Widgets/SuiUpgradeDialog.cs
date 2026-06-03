using System;
using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Three-button upgrade prompt — used when the SUI Designer detects one or
/// more <c>.sui</c> documents saved against an older schema. Primary action
/// migrates everything; secondary lets the user defer; tertiary persists
/// "stop asking" for the current schema version.
///
/// <para>Modeled on <see cref="SuiConfirmDialog"/> but with a third action
/// row — the two-button dialog can't express "migrate / skip / never ask"
/// without overloading the cancel handler with state.</para>
/// </summary>
public sealed class SuiUpgradeDialog : Window
{
	/// <summary>Show the modal upgrade dialog. Exactly one callback fires (or none, if the user X-closes).</summary>
	public static void Show(
		string title,
		string body,
		string primaryText,
		string secondaryText,
		string tertiaryText,
		Action onPrimary,
		Action onSecondary = null,
		Action onTertiary = null )
	{
		_ = new SuiUpgradeDialog( title, body, primaryText, secondaryText, tertiaryText )
		{
			OnPrimary = onPrimary,
			OnSecondary = onSecondary,
			OnTertiary = onTertiary,
		};
	}

	public Action OnPrimary;
	public Action OnSecondary;
	public Action OnTertiary;

	private SuiUpgradeDialog( string title, string body, string primaryText, string secondaryText, string tertiaryText )
	{
		Title = title ?? "Sbox UI Designer";
		WindowTitle = Title;
		Size = new Vector2( 520, 240 );
		MinimumSize = new Vector2( 520, 240 );
		SetWindowIcon( "auto_fix_high" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 16;
		Canvas.Layout.Spacing = 12;

		var msg = new Label( body ?? "", Canvas );
		msg.WordWrap = true;
		msg.SetStyles( "color: #e5e7eb; font-size: 12px;" );
		Canvas.Layout.Add( msg );

		Canvas.Layout.AddStretchCell();

		var buttons = new Widget( Canvas );
		buttons.Layout = Layout.Row();
		buttons.Layout.Spacing = 8;

		// Tertiary "Don't ask again" — far-left so it can't be mis-clicked by a user reaching for Migrate.
		if ( !string.IsNullOrEmpty( tertiaryText ) )
		{
			var tertiary = new Button( tertiaryText, buttons );
			tertiary.SetStyles( "color: #9ca3af;" );
			tertiary.Clicked = () => { var cb = OnTertiary; Close(); cb?.Invoke(); };
			buttons.Layout.Add( tertiary );
		}

		buttons.Layout.AddStretchCell();

		var secondary = new Button( secondaryText ?? "Skip", buttons );
		secondary.Clicked = () => { var cb = OnSecondary; Close(); cb?.Invoke(); };
		buttons.Layout.Add( secondary );

		var primary = new Button( primaryText ?? "OK", buttons );
		primary.SetStyles( "background: #2563eb; color: #fff;" );
		primary.Clicked = () => { var cb = OnPrimary; Close(); cb?.Invoke(); };
		buttons.Layout.Add( primary );

		Canvas.Layout.Add( buttons );
		Show();
	}
}
