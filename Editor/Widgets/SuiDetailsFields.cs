using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

// =============================================================================
//  Details panel field widgets — all 100% custom paint, share the dark
//  rounded look of the search inputs in Palette/Hierarchy.
//
//  Common style:
//    background-color: rgb(20,20,19)         (#141413, same as search)
//    border: 1px solid rgba(255,255,255,0.06)
//    border-radius: 3px
//    color: rgb(220,224,230)
//    font-size: 11px
// =============================================================================

internal static class SuiFieldStyle
{
	public const string Input =
		"background-color: rgb(20,20,19);" +
		"border: 1px solid rgba(255,255,255,0.06);" +
		"border-radius: 3px;" +
		"color: rgb(220,224,230);" +
		// Same formula as the search inputs in Palette/Hierarchy which
		// vertical-center automatically: zero top/bottom padding + a tall
		// enough height for the font to breathe.
		"padding: 0 8px;" +
		"font-size: 11px;";

	public const string SubLabel = "color: rgb(140,145,153); font-size: 10px;";
}

/// <summary>Single-line text field — wraps a styled <see cref="LineEdit"/>.</summary>
public sealed class SuiTextField : Widget
{
	private readonly LineEdit _input;
	public event Action<string> ValueCommitted;

	public string Text
	{
		get => _input?.Text ?? "";
		set { if ( _input != null ) _input.Text = value ?? ""; }
	}

	public SuiTextField( Widget parent = null ) : base( parent )
	{
		FixedHeight = 26;
		SetStyles( "background-color: transparent; border: none;" );
		Layout = Layout.Row();
		Layout.Margin = 0;

		_input = new LineEdit( this );
		_input.FixedHeight = 26;
		_input.SetStyles( SuiFieldStyle.Input );
		_input.EditingFinished += () => ValueCommitted?.Invoke( _input.Text );
		Layout.Add( _input, 1 );
	}
}

/// <summary>
/// Number field — styled <see cref="LineEdit"/> with numeric parsing on commit.
/// </summary>
public sealed class SuiNumberField : Widget
{
	private readonly LineEdit _input;
	public event Action<float> ValueCommitted;

	public float Value
	{
		get => float.TryParse( _input?.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v ) ? v : 0f;
		set { if ( _input != null ) _input.Text = value.ToString( "0.###", System.Globalization.CultureInfo.InvariantCulture ); }
	}

	public SuiNumberField( Widget parent = null ) : base( parent )
	{
		FixedHeight = 26;
		SetStyles( "background-color: transparent; border: none;" );
		Layout = Layout.Row();
		Layout.Margin = 0;

		_input = new LineEdit( this );
		_input.FixedHeight = 26;
		_input.SetStyles( SuiFieldStyle.Input );
		_input.EditingFinished += () =>
		{
			var v = Value;
			ValueCommitted?.Invoke( v );
		};
		Layout.Add( _input, 1 );
	}
}

/// <summary>
/// Vector field — N labeled number boxes in a row. Use for X/Y, X/Y/W/H,
/// L/T/R/B etc. Each component has a small uppercase letter label to its left.
/// </summary>
public sealed class SuiVectorField : Widget
{
	private readonly List<(string label, SuiNumberField field)> _components = new();
	public event Action<int, float> ComponentCommitted;

	public SuiVectorField( string[] componentLabels, Widget parent = null ) : base( parent )
	{
		FixedHeight = 26;
		SetStyles( "background-color: transparent; border: none;" );
		Layout = Layout.Row();
		Layout.Margin = 0;
		Layout.Spacing = 6;

		for ( int i = 0; i < componentLabels.Length; i++ )
		{
			var idx = i;
			var lab = componentLabels[i];

			var pair = new Widget( this );
			pair.SetStyles( "background-color: transparent; border: none;" );
			pair.Layout = Layout.Row();
			pair.Layout.Margin = 0;
			pair.Layout.Spacing = 4;

			var letter = new SuiVectorLabel( lab );
			letter.FixedWidth = 12;
			pair.Layout.Add( letter );

			var f = new SuiNumberField();
			f.Parent = pair;
			f.ValueCommitted += v => ComponentCommitted?.Invoke( idx, v );
			pair.Layout.Add( f, 1 );

			_components.Add( (lab, f) );
			Layout.Add( pair, 1 );
		}
	}

	public void SetComponent( int index, float value )
	{
		if ( index < 0 || index >= _components.Count ) return;
		_components[index].field.Value = value;
	}
}

internal sealed class SuiVectorLabel : Widget
{
	public string Text { get; }

	public SuiVectorLabel( string text, Widget parent = null ) : base( parent )
	{
		Text = text ?? "";
		FixedHeight = 26;
		SetStyles( "background-color: transparent; border: none;" );
	}

	protected override void OnPaint()
	{
		Paint.SetDefaultFont( 10 );
		Paint.SetPen( new Color( 140 / 255f, 145 / 255f, 153 / 255f ) );
		Paint.DrawText( new Rect( 0, 0, Width, Height ), Text, TextFlag.Center );
	}
}

/// <summary>Boolean toggle — checkbox-style on the left, "On"/"Off" label.</summary>
public sealed class SuiToggleField : Widget
{
	public bool Checked { get; private set; }
	public event Action<bool> ValueChanged;

	public SuiToggleField( bool initial = false, Widget parent = null ) : base( parent )
	{
		Checked = initial;
		FixedHeight = 26;
		Cursor = CursorShape.Finger;
		SetStyles( SuiFieldStyle.Input );
	}

	public void SetChecked( bool value, bool fireEvent = false )
	{
		if ( Checked == value ) return;
		Checked = value;
		Update();
		if ( fireEvent ) ValueChanged?.Invoke( Checked );
	}

	protected override void OnPaint()
	{
		// Checkbox square on the left.
		var box = new Rect( 6, (Height - 14) / 2f, 14, 14 );
		var boxBg = Checked ? new Color( 0f, 0.55f, 1f, 1f ) : new Color( 30 / 255f, 30 / 255f, 29 / 255f );
		Paint.SetBrush( boxBg );
		Paint.SetPen( Color.White.WithAlpha( 0.18f ) );
		Paint.DrawRect( box, 2f );

		if ( Checked )
		{
			// Checkmark.
			Paint.SetPen( Color.White, 2f );
			Paint.DrawLine( new Vector2( box.Left + 3, box.Top + 7 ), new Vector2( box.Left + 6, box.Top + 10 ) );
			Paint.DrawLine( new Vector2( box.Left + 6, box.Top + 10 ), new Vector2( box.Left + 11, box.Top + 4 ) );
		}

		// Label "On" / "Off".
		Paint.SetDefaultFont( 10 );
		Paint.SetPen( new Color( 220 / 255f, 224 / 255f, 230 / 255f ) );
		Paint.DrawText( new Rect( 26, 0, Width - 28, Height ), Checked ? "On" : "Off", TextFlag.LeftCenter );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( !e.LeftMouseButton ) return;
		Checked = !Checked;
		// Callback may rebuild the parent (Refresh) and destroy this widget.
		// Update only AFTER, and only if we still exist.
		ValueChanged?.Invoke( Checked );
		if ( IsValid ) Update();
	}
}

/// <summary>
/// Dropdown field — paint-only widget showing the current value + chevron.
/// Click opens a Menu populated by <see cref="SetOptions"/>.
/// </summary>
public sealed class SuiDropdownField : Widget
{
	private string _value = "";
	private List<(string label, Action select)> _options = new();
	public event Action<string> ValueSelected;

	public string Value
	{
		get => _value;
		set { _value = value ?? ""; Update(); }
	}

	public SuiDropdownField( Widget parent = null ) : base( parent )
	{
		FixedHeight = 26;
		Cursor = CursorShape.Finger;
		SetStyles( SuiFieldStyle.Input );
	}

	public void SetOptions( IEnumerable<string> labels )
	{
		_options.Clear();
		foreach ( var l in labels )
		{
			var captured = l;
			_options.Add( (l, () =>
			{
				_value = captured;
				// Invoke callback FIRST — it may rebuild parent (Refresh)
				// and destroy this widget. Update only if we survived.
				ValueSelected?.Invoke( captured );
				if ( IsValid ) Update();
			}
			) );
		}
	}

	protected override void OnPaint()
	{
		Paint.SetDefaultFont( 10 );
		Paint.SetPen( new Color( 220 / 255f, 224 / 255f, 230 / 255f ) );
		Paint.DrawText( new Rect( 8, 0, Width - 24, Height ), _value, TextFlag.LeftCenter );

		// Chevron.
		Paint.SetPen( new Color( 165 / 255f, 172 / 255f, 182 / 255f ), 1.5f );
		var cx = Width - 12f;
		var cy = Height / 2f;
		Paint.DrawLine( new Vector2( cx - 4, cy - 2 ), new Vector2( cx, cy + 2 ) );
		Paint.DrawLine( new Vector2( cx, cy + 2 ), new Vector2( cx + 4, cy - 2 ) );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( !e.LeftMouseButton || _options.Count == 0 ) return;
		var menu = new Menu( this );
		foreach ( var (label, sel) in _options )
		{
			menu.AddOption( label, null, sel );
		}
		// Open the menu anchored to the dropdown widget itself (just below it)
		// instead of at cursor — which was placing it wherever the user clicked,
		// often outside the panel.
		menu.OpenAt( ScreenPosition + new Vector2( 0, Height ) );
	}
}

// SuiColorField removed — SuiColorSwatchField (in its own file) provides the
// same purpose with more features (alpha checkerboard, clear button, copy/
// paste context menu) and is what AddColorRow uses.


/// <summary>
/// Anchor picker button — shows a tiny 3x3 grid icon highlighting the current
/// anchor + the anchor name + chevron. Click opens the anchor popup.
/// </summary>
public sealed class SuiAnchorPickerButton : Widget
{
	public SuiAnchor Anchor { get; private set; }
	public event Action Clicked;

	public SuiAnchorPickerButton( SuiAnchor initial, Widget parent = null ) : base( parent )
	{
		Anchor = initial;
		FixedHeight = 26;
		Cursor = CursorShape.Finger;
		SetStyles( SuiFieldStyle.Input );
	}

	public void SetAnchor( SuiAnchor a ) { Anchor = a; Update(); }

	protected override void OnPaint()
	{
		// 3x3 grid mini icon at left.
		float gridSize = 12f;
		float gx = 6f;
		float gy = (Height - gridSize) / 2f;
		var dotColor = new Color( 165 / 255f, 172 / 255f, 182 / 255f );
		var activeDot = new Color( 0f, 0.55f, 1f );
		Paint.SetPen( dotColor, 1f );
		for ( int row = 0; row < 3; row++ )
		{
			for ( int col = 0; col < 3; col++ )
			{
				bool isActive = IsAnchorCellActive( Anchor, col, row );
				Paint.SetBrush( isActive ? activeDot : dotColor.WithAlpha( 0.45f ) );
				Paint.ClearPen();
				var dotRect = new Rect( gx + col * 5, gy + row * 5, 3, 3 );
				Paint.DrawRect( dotRect, 1f );
			}
		}

		// Label.
		Paint.SetDefaultFont( 10 );
		Paint.SetPen( new Color( 220 / 255f, 224 / 255f, 230 / 255f ) );
		Paint.DrawText( new Rect( 28, 0, Width - 44, Height ), AnchorPrettyName( Anchor ), TextFlag.LeftCenter );

		// Chevron.
		Paint.SetPen( dotColor, 1.5f );
		var cx = Width - 12f;
		var cy = Height / 2f;
		Paint.DrawLine( new Vector2( cx - 4, cy - 2 ), new Vector2( cx, cy + 2 ) );
		Paint.DrawLine( new Vector2( cx, cy + 2 ), new Vector2( cx + 4, cy - 2 ) );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton ) Clicked?.Invoke();
	}

	private static bool IsAnchorCellActive( SuiAnchor a, int col, int row )
	{
		// 0=left/top 1=center/middle 2=right/bottom
		switch ( a )
		{
			case SuiAnchor.TopLeft:      return col == 0 && row == 0;
			case SuiAnchor.TopCenter:    return col == 1 && row == 0;
			case SuiAnchor.TopRight:     return col == 2 && row == 0;
			case SuiAnchor.MiddleLeft:   return col == 0 && row == 1;
			case SuiAnchor.MiddleCenter: return col == 1 && row == 1;
			case SuiAnchor.MiddleRight:  return col == 2 && row == 1;
			case SuiAnchor.BottomLeft:   return col == 0 && row == 2;
			case SuiAnchor.BottomCenter: return col == 1 && row == 2;
			case SuiAnchor.BottomRight:  return col == 2 && row == 2;

			// Stretch Horizontal — middle row, both ends.
			case SuiAnchor.StretchHorizontal:
				return row == 1 && (col == 0 || col == 2);

			// Stretch Vertical — middle column, both ends.
			case SuiAnchor.StretchVertical:
				return col == 1 && (row == 0 || row == 2);

			// Fill — four corners light up.
			case SuiAnchor.Stretch:
				return (col == 0 || col == 2) && (row == 0 || row == 2);

			default: return false;
		}
	}

	private static string AnchorPrettyName( SuiAnchor a ) => a switch
	{
		SuiAnchor.TopLeft => "Top Left",
		SuiAnchor.TopCenter => "Top Center",
		SuiAnchor.TopRight => "Top Right",
		SuiAnchor.MiddleLeft => "Middle Left",
		SuiAnchor.MiddleCenter => "Middle Center",
		SuiAnchor.MiddleRight => "Middle Right",
		SuiAnchor.BottomLeft => "Bottom Left",
		SuiAnchor.BottomCenter => "Bottom Center",
		SuiAnchor.BottomRight => "Bottom Right",
		SuiAnchor.Stretch => "Fill (Stretch)",
		SuiAnchor.StretchHorizontal => "Stretch Horizontal",
		SuiAnchor.StretchVertical => "Stretch Vertical",
		_ => a.ToString(),
	};
}

/// <summary>Browse-asset button — folder icon + transparent bg, hover tint.</summary>
public sealed class SuiBrowseButton : Widget
{
	public event Action Clicked;
	private bool _hover, _pressed;
	public string Icon { get; set; } = "folder_open";

	public SuiBrowseButton( Widget parent = null ) : base( parent )
	{
		FixedWidth = 28;
		FixedHeight = 26;
		Cursor = CursorShape.Finger;
		SetStyles( SuiFieldStyle.Input );
	}

	protected override void OnPaint()
	{
		if ( _pressed )
		{
			Paint.SetBrushAndPen( Color.White.WithAlpha( 0.10f ) );
			Paint.DrawRect( LocalRect, 3f );
		}
		else if ( _hover )
		{
			Paint.SetBrushAndPen( Color.White.WithAlpha( 0.06f ) );
			Paint.DrawRect( LocalRect, 3f );
		}
		Paint.SetPen( new Color( 220 / 255f, 224 / 255f, 230 / 255f ) );
		Paint.DrawIcon( LocalRect, Icon, 14, TextFlag.Center );
	}

	protected override void OnMouseEnter() { _hover = true; Update(); }
	protected override void OnMouseLeave() { _hover = false; _pressed = false; Update(); }
	protected override void OnMousePress( MouseEvent e ) { if ( e.LeftMouseButton ) { _pressed = true; Update(); } }
	protected override void OnMouseReleased( MouseEvent e )
	{
		if ( _pressed && e.LeftMouseButton )
		{
			_pressed = false;
			Update();
			if ( _hover ) Clicked?.Invoke();
		}
	}
}

