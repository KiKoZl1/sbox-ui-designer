using System;
using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Color row replacement for the Details panel — instead of a small swatch
/// next to a hex LineEdit, this widget IS the color, painted full-width with
/// the hex value overlaid in a contrasting color.
///
/// Click anywhere → opens <see cref="SuiColorPickerPopup"/> (live commits
/// during drag, final commit on close). Right-click → context menu (copy
/// hex / paste hex / clear).
/// </summary>
public sealed class SuiColorSwatchField : Widget
{
	private string _hex = "";
	private Action<string> _onCommit;
	private Rect _clearButtonRect;

	public SuiColorSwatchField( Widget parent ) : base( parent )
	{
		// Match the height of the other field types (text / dropdown / etc)
		// so rows line up. No max width — fill the row.
		FixedHeight = 26;
		Cursor = CursorShape.Finger;
		ToolTip = "Click to pick a color · right-click for clear / copy / paste";
		MouseTracking = true;
		SetStyles( "background-color: transparent; border: none;" );
	}

	public void SetValue( string hex )
	{
		_hex = hex ?? "";
		Update();
	}

	public void SetCommitHandler( Action<string> onCommit )
	{
		_onCommit = onCommit;
	}

	protected override void OnPaint()
	{
		var rect = LocalRect;
		var hasColor = !string.IsNullOrEmpty( _hex ) && Color.TryParse( _hex, out _ );

		// Hit area for clear button — unused now (right-click menu replaces it).
		_clearButtonRect = default;

		// 1. OUTER FRAME — same dark bg + 1px border as other input fields.
		Paint.SetBrush( new Color( 20 / 255f, 20 / 255f, 19 / 255f ) );
		Paint.SetPen( Color.White.WithAlpha( 0.10f ) );
		Paint.DrawRect( rect, 3 );

		// 2. INNER COLOR PATCH — sits inside the frame with 3px padding so
		// the dark border reads as a visible "box".
		var inner = new Rect(
			rect.Left + 3,
			rect.Top + 3,
			rect.Width - 6,
			rect.Height - 6 );

		if ( hasColor )
		{
			PaintCheckerboard( inner );
			Color.TryParse( _hex, out var color );
			Paint.SetBrush( color );
			Paint.ClearPen();
			Paint.DrawRect( inner, 2 );
		}
		else
		{
			// Empty: leave the inner area showing a neutral grey.
			Paint.SetBrush( new Color( 70 / 255f, 72 / 255f, 78 / 255f ) );
			Paint.ClearPen();
			Paint.DrawRect( inner, 2 );
		}
	}

	private static void PaintCheckerboard( Rect rect )
	{
		Paint.ClearPen();
		Paint.SetBrush( new Color( 0.6f, 0.6f, 0.6f ) );
		Paint.DrawRect( rect, 3 );
		Paint.SetBrush( new Color( 0.4f, 0.4f, 0.4f ) );
		const float cell = 6;
		var cols = (int)MathF.Ceiling( rect.Width / cell );
		var rows = (int)MathF.Ceiling( rect.Height / cell );
		for ( int y = 0; y < rows; y++ )
			for ( int x = 0; x < cols; x++ )
				if ( ((x + y) & 1) != 0 )
					Paint.DrawRect( new Rect( rect.Left + x * cell, rect.Top + y * cell, cell, cell ) );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( !e.LeftMouseButton ) return;
		// Click on the × clear button → wipe the color.
		if ( _clearButtonRect.Width > 0 && PointInside( _clearButtonRect, e.LocalPosition ) )
		{
			_hex = "";
			Update();
			_onCommit?.Invoke( "" );
			return;
		}
		OpenPicker();
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		OpenContextMenuAt();
	}

	private static bool PointInside( Rect r, Vector2 p )
		=> p.x >= r.Left && p.x <= r.Right && p.y >= r.Top && p.y <= r.Bottom;

	private void OpenPicker()
	{
		Color.TryParse( _hex, out var start );
		if ( start.a == 0 && start.r == 0 && start.g == 0 && start.b == 0 ) start = Color.White;

		var picker = SuiColorPickerPopup.OpenColorPopup( start, c =>
		{
			_hex = ColorToHex( c );
			Update();
			_onCommit?.Invoke( _hex );
		} );
		picker.EditingFinished += () =>
		{
			// Final safety commit so the document state is in sync with whatever
			// the popup last reported, even if the live callback was missed.
			_onCommit?.Invoke( _hex );
		};
		picker.Cleared += () =>
		{
			_hex = "";
			Update();
			_onCommit?.Invoke( "" );
		};
	}

	private void OpenContextMenuAt()
	{
		var m = new Menu( this );
		m.AddOption( "Copy hex", "content_copy", () =>
		{
			if ( !string.IsNullOrEmpty( _hex ) )
				EditorUtility.Clipboard.Copy( _hex );
		} );
		m.AddOption( "Paste hex", "content_paste", () =>
		{
			var pasted = EditorUtility.Clipboard.Paste();
			if ( !string.IsNullOrEmpty( pasted ) && Color.TryParse( pasted, out _ ) )
			{
				_hex = pasted;
				Update();
				_onCommit?.Invoke( _hex );
			}
		} );
		m.AddSeparator();
		m.AddOption( "Clear", "delete", () =>
		{
			_hex = "";
			Update();
			_onCommit?.Invoke( _hex );
		} );
		m.OpenAtCursor( true );
	}

	private static string ColorToHex( Color c )
	{
		var r = (int)Math.Clamp( c.r * 255, 0, 255 );
		var g = (int)Math.Clamp( c.g * 255, 0, 255 );
		var b = (int)Math.Clamp( c.b * 255, 0, 255 );
		var a = (int)Math.Clamp( c.a * 255, 0, 255 );
		return a < 255 ? $"#{r:x2}{g:x2}{b:x2}{a:x2}" : $"#{r:x2}{g:x2}{b:x2}";
	}
}
