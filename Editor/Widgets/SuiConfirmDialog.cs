using System;
using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Minimal confirm modal — title + body + OK/Cancel. Used wherever the editor
/// needs the user to acknowledge an irreversible action (e.g. auto-switching
/// a binding's mode to OneWay because the user added a converter).
/// </summary>
public sealed class SuiConfirmDialog : Window
{
	/// <summary>Show a modal confirm dialog. The <paramref name="onConfirm"/> action fires only on OK.</summary>
	public static void Show( string title, string body, string okText = "OK", string cancelText = "Cancel", Action onConfirm = null )
	{
		var dlg = new SuiConfirmDialog( title, body, okText, cancelText );
		dlg.OnConfirm = onConfirm;
	}

	public Action OnConfirm;

	private SuiConfirmDialog( string title, string body, string okText, string cancelText )
	{
		Title = title ?? "Confirm";
		WindowTitle = Title;
		Size = new Vector2( 440, 180 );
		MinimumSize = new Vector2( 440, 180 );
		SetWindowIcon( "help" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 14;
		Canvas.Layout.Spacing = 12;

		var msg = new Label( body ?? "", Canvas );
		msg.WordWrap = true;
		msg.SetStyles( "color: #e5e7eb; font-size: 12px;" );
		Canvas.Layout.Add( msg );

		Canvas.Layout.AddStretchCell();

		var buttons = new Widget( Canvas );
		buttons.Layout = Layout.Row();
		buttons.Layout.Spacing = 8;
		buttons.Layout.AddStretchCell();

		// Pass a null/empty `cancelText` to render a single-button informational
		// dialog (OK only). Used for post-action summary modals where the user
		// has nothing to cancel — they just need to acknowledge.
		if ( !string.IsNullOrEmpty( cancelText ) )
		{
			var cancel = new Button( cancelText, buttons );
			cancel.Clicked = Close;
			buttons.Layout.Add( cancel );
		}

		var ok = new Button( okText ?? "OK", buttons );
		ok.Clicked = () => { var cb = OnConfirm; Close(); cb?.Invoke(); };
		buttons.Layout.Add( ok );

		Canvas.Layout.Add( buttons );
		Show();
	}
}
