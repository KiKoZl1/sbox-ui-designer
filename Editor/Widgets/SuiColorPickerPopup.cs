using System;
using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Custom color picker popup — replaces <c>Editor.ColorPicker.OpenColorPopup</c>
/// because the editor's built-in widget has multiple correctness bugs (ISSUE-001
/// to ISSUE-003 in the project ISSUES.md):
///
/// - SV gradient doesn't repaint on hue change
/// - Lag during slider drag
/// - Commit fires intermittently (subsequent picks ignored)
/// - Initial state mis-positioned for the current color
///
/// Architecture:
/// - State lives as <see cref="ColorHsv"/> internally so all subwidgets read/write
///   the same source of truth without lossy round-trips through hex/RGB.
/// - <see cref="ColorChanged"/> fires on every interactive change (live preview).
/// - <see cref="EditingFinished"/> fires when the user commits (closes popup or
///   blurs out of an input).
/// - Any change recomputes the cached SV gradient pixmap (only when hue moves).
/// </summary>
public sealed class SuiColorPickerPopup : Window
{
	private ColorHsv _color;
	private readonly Color _initial;

	private SvSquare _svSquare;
	private HueSlider _hueSlider;
	private AlphaSlider _alphaSlider;
	private LineEdit _hexInput;
	private LineEdit _rEdit, _gEdit, _bEdit, _aEdit;
	private Widget _newSwatch, _oldSwatch;
	private bool _suppressBack;

	public event Action<Color> ColorChanged;
	public event Action EditingFinished;

	public Color Current
	{
		get => (Color)_color;
		set
		{
			_color = (ColorHsv)value;
			RefreshAllFromState();
		}
	}

	public SuiColorPickerPopup( Color start, Action<Color> onLiveChange )
	{
		_initial = start;
		_color = (ColorHsv)start;
		ColorChanged += onLiveChange;

		Title = "Color Picker";
		WindowTitle = "Color Picker";
		Size = new Vector2( 280, 460 );
		MinimumSize = new Vector2( 280, 460 );
		SetWindowIcon( "palette" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 8;
		Canvas.Layout.Spacing = 6;

		BuildLayout();
		RefreshAllFromState();
		Show();
	}

	/// <summary>
	/// Drop-in replacement for <c>Editor.ColorPicker.OpenColorPopup</c>. Returns
	/// the popup so callers can hook <see cref="EditingFinished"/>.
	/// </summary>
	public static SuiColorPickerPopup OpenColorPopup( Color start, Action<Color> onLiveChange )
		=> new( start, onLiveChange );

	private void BuildLayout()
	{
		// SV square (saturation × value).
		_svSquare = new SvSquare( Canvas );
		_svSquare.OnSvChanged = ( s, v ) =>
		{
			_color = new ColorHsv( _color.Hue, s, v, _color.Alpha );
			RefreshAllFromState( exceptSvSquare: true );
			Notify();
		};
		Canvas.Layout.Add( _svSquare );

		// Hue slider. Subwidget API uses 0..1 (intuitive for sliders); we
		// scale to 0..360 at the boundary because ColorHsv.Hue expects degrees.
		_hueSlider = new HueSlider( Canvas );
		_hueSlider.OnHueChanged = h =>
		{
			_color = new ColorHsv( h * 360f, _color.Saturation, _color.Value, _color.Alpha );
			RefreshAllFromState( exceptHueSlider: true );
			Notify();
		};
		Canvas.Layout.Add( _hueSlider );

		// Alpha slider.
		_alphaSlider = new AlphaSlider( Canvas );
		_alphaSlider.OnAlphaChanged = a =>
		{
			_color = new ColorHsv( _color.Hue, _color.Saturation, _color.Value, a );
			RefreshAllFromState( exceptAlphaSlider: true );
			Notify();
		};
		Canvas.Layout.Add( _alphaSlider );

		// Compare swatches: old vs new.
		var swatchRow = Layout.Row();
		swatchRow.Spacing = 6;

		_oldSwatch = new Widget( Canvas );
		_oldSwatch.FixedHeight = 28;
		_oldSwatch.SetStyles( $"background-color: {ColorToCss( _initial )}; border: 1px solid #555; border-radius: 3px;" );
		_oldSwatch.ToolTip = "Original color (click to revert)";
		_oldSwatch.MouseLeftPress += () => { Current = _initial; Notify(); };
		swatchRow.Add( _oldSwatch, 1 );

		_newSwatch = new Widget( Canvas );
		_newSwatch.FixedHeight = 28;
		_newSwatch.SetStyles( "border: 1px solid #555; border-radius: 3px;" );
		swatchRow.Add( _newSwatch, 1 );

		Canvas.Layout.Add( swatchRow );

		// Hex input.
		var hexRow = Layout.Row();
		hexRow.Spacing = 4;
		var hexLabel = new Label( "Hex", Canvas );
		hexLabel.FixedWidth = 32;
		hexRow.Add( hexLabel );
		_hexInput = new LineEdit( Canvas );
		_hexInput.PlaceholderText = "#rrggbb or #rrggbbaa";
		_hexInput.EditingFinished += () =>
		{
			if ( _suppressBack ) return;
			if ( Color.TryParse( _hexInput.Text, out var parsed ) )
			{
				_color = (ColorHsv)parsed;
				RefreshAllFromState( exceptHex: true );
				Notify();
			}
		};
		hexRow.Add( _hexInput, 1 );
		Canvas.Layout.Add( hexRow );

		// RGB inputs (255 scale).
		var rgbRow = Layout.Row();
		rgbRow.Spacing = 4;
		_rEdit = AddInt255( rgbRow, "R", v => SetRgb( v, null, null, null ) );
		_gEdit = AddInt255( rgbRow, "G", v => SetRgb( null, v, null, null ) );
		_bEdit = AddInt255( rgbRow, "B", v => SetRgb( null, null, v, null ) );
		_aEdit = AddInt255( rgbRow, "A", v => SetRgb( null, null, null, v ) );
		Canvas.Layout.Add( rgbRow );

		Canvas.Layout.AddStretchCell();

		// Footer buttons.
		var footer = Layout.Row();
		footer.Spacing = 6;
		var clear = new Button( "Clear", "delete", Canvas );
		clear.ToolTip = "Remove the color (sets to empty/unset)";
		clear.Clicked += () =>
		{
			Cleared?.Invoke();
			EditingFinished?.Invoke();
			Close();
		};
		footer.Add( clear );
		footer.AddStretchCell();
		var cancel = new Button( "Cancel", "close", Canvas );
		cancel.Clicked += () => { Current = _initial; Notify(); EditingFinished?.Invoke(); Close(); };
		footer.Add( cancel );
		var ok = new Button( "OK", "check", Canvas );
		ok.Clicked += () => { EditingFinished?.Invoke(); Close(); };
		footer.Add( ok );
		Canvas.Layout.Add( footer );
	}

	/// <summary>Fires when the user clicks the "Clear" button to remove the color entirely.</summary>
	public event Action Cleared;

	private LineEdit AddInt255( Layout row, string label, Action<int> onCommit )
	{
		var lbl = new Label( label, Canvas );
		lbl.FixedWidth = 14;
		lbl.SetStyles( "color: #9ca3af;" );
		row.Add( lbl );
		var le = new LineEdit( Canvas );
		le.FixedWidth = 48;
		le.PlaceholderText = "0-255";
		le.EditingFinished += () =>
		{
			if ( _suppressBack ) return;
			if ( int.TryParse( le.Text, out var v ) )
				onCommit( Math.Clamp( v, 0, 255 ) );
		};
		row.Add( le, 1 );
		return le;
	}

	private void SetRgb( int? r, int? g, int? b, int? a )
	{
		var c = (Color)_color;
		var nr = r.HasValue ? r.Value / 255f : c.r;
		var ng = g.HasValue ? g.Value / 255f : c.g;
		var nb = b.HasValue ? b.Value / 255f : c.b;
		var na = a.HasValue ? a.Value / 255f : c.a;
		_color = (ColorHsv)new Color( nr, ng, nb, na );
		RefreshAllFromState();
		Notify();
	}

	private void RefreshAllFromState(
		bool exceptSvSquare = false,
		bool exceptHueSlider = false,
		bool exceptAlphaSlider = false,
		bool exceptHex = false )
	{
		_suppressBack = true;
		try
		{
			// Subwidgets work in 0..1 hue range; ColorHsv.Hue is 0..360.
			var hue01 = _color.Hue / 360f;

			if ( !exceptSvSquare ) _svSquare.SetState( hue01, _color.Saturation, _color.Value );
			else _svSquare.SetHueOnly( hue01 ); // still refresh gradient
			if ( !exceptHueSlider ) _hueSlider.SetHue( hue01 );
			if ( !exceptAlphaSlider ) _alphaSlider.SetState( (Color)_color, _color.Alpha );
			else _alphaSlider.SetBaseColor( (Color)_color );

			if ( !exceptHex ) _hexInput.Text = ColorToHex( (Color)_color );

			var c = (Color)_color;
			_rEdit.Text = ((int)Math.Round( c.r * 255 )).ToString();
			_gEdit.Text = ((int)Math.Round( c.g * 255 )).ToString();
			_bEdit.Text = ((int)Math.Round( c.b * 255 )).ToString();
			_aEdit.Text = ((int)Math.Round( c.a * 255 )).ToString();

			_newSwatch.SetStyles( $"background-color: {ColorToCss( (Color)_color )}; border: 1px solid #555; border-radius: 3px;" );
		}
		finally
		{
			_suppressBack = false;
		}
	}

	private void Notify()
	{
		ColorChanged?.Invoke( (Color)_color );
	}

	private static string ColorToHex( Color c )
	{
		var r = (int)Math.Clamp( c.r * 255, 0, 255 );
		var g = (int)Math.Clamp( c.g * 255, 0, 255 );
		var b = (int)Math.Clamp( c.b * 255, 0, 255 );
		var a = (int)Math.Clamp( c.a * 255, 0, 255 );
		return a < 255
			? $"#{r:x2}{g:x2}{b:x2}{a:x2}"
			: $"#{r:x2}{g:x2}{b:x2}";
	}

	private static string ColorToCss( Color c )
	{
		var r = (int)Math.Clamp( c.r * 255, 0, 255 );
		var g = (int)Math.Clamp( c.g * 255, 0, 255 );
		var b = (int)Math.Clamp( c.b * 255, 0, 255 );
		return $"rgba({r},{g},{b},{c.a:0.###})";
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Subwidget: SV square (saturation × value, hue-driven gradient)
	// ─────────────────────────────────────────────────────────────────────

	private sealed class SvSquare : Widget
	{
		private float _hue, _sat, _val;
		private Pixmap _gradientCache;
		private float _cachedHue = -1;
		public Action<float, float> OnSvChanged;

		public SvSquare( Widget parent ) : base( parent )
		{
			FixedSize = new Vector2( 256, 200 );
			Cursor = CursorShape.Cross;
			MouseTracking = true;
		}

		public void SetState( float h, float s, float v )
		{
			_hue = h; _sat = s; _val = v;
			Update();
		}

		public void SetHueOnly( float h )
		{
			_hue = h;
			Update();
		}

		protected override void OnPaint()
		{
			var rect = LocalRect;

			// Cache the gradient pixmap per-hue. 64×64 is enough — we'll let the GPU
			// upscale via Paint.Draw bilinear; the SV gradient is smooth so artifacts
			// are imperceptible at picker resolution.
			if ( _gradientCache == null || MathF.Abs( _cachedHue - _hue ) > 0.001f )
			{
				_gradientCache = BuildSvPixmap( _hue, 64, 64 );
				_cachedHue = _hue;
			}
			Paint.Draw( rect, _gradientCache );

			// Crosshair at (s, 1-v).
			var px = rect.Left + _sat * rect.Width;
			var py = rect.Top + (1 - _val) * rect.Height;
			Paint.SetPen( Color.Black.WithAlpha( 0.7f ), 2 );
			Paint.ClearBrush();
			Paint.DrawCircle( new Vector2( px, py ), new Vector2( 8, 8 ) );
			Paint.SetPen( Color.White.WithAlpha( 0.95f ), 1 );
			Paint.DrawCircle( new Vector2( px, py ), new Vector2( 8, 8 ) );
		}

		private static Pixmap BuildSvPixmap( float hue01, int w, int h )
		{
			var pix = new Pixmap( w, h );
			var hueDeg = hue01 * 360f; // ColorHsv.Hue is 0..360 (degrees), not 0..1
			using ( Paint.ToPixmap( pix ) )
			{
				Paint.ClearPen();
				for ( int y = 0; y < h; y++ )
				{
					var v = 1f - y / (float)(h - 1);
					for ( int x = 0; x < w; x++ )
					{
						var s = x / (float)(w - 1);
						var c = (Color)new ColorHsv( hueDeg, s, v, 1f );
						Paint.SetBrush( c );
						Paint.DrawRect( new Rect( x, y, 1, 1 ) );
					}
				}
			}
			return pix;
		}

		protected override void OnMousePress( MouseEvent e )
		{
			if ( e.LeftMouseButton ) UpdateFromMouse( e.LocalPosition );
		}

		protected override void OnMouseMove( MouseEvent e )
		{
			if ( (e.ButtonState & MouseButtons.Left) != 0 ) UpdateFromMouse( e.LocalPosition );
		}

		private void UpdateFromMouse( Vector2 p )
		{
			var s = Math.Clamp( p.x / Width, 0f, 1f );
			var v = 1f - Math.Clamp( p.y / Height, 0f, 1f );
			_sat = s; _val = v;
			OnSvChanged?.Invoke( s, v );
			Update();
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Subwidget: Hue slider (horizontal rainbow bar)
	// ─────────────────────────────────────────────────────────────────────

	private sealed class HueSlider : Widget
	{
		private float _hue;
		// Per-instance cache (not static) — static fields don't survive hotload
		// cleanly and the hotload pipeline reports "Unable to find matching
		// substitution for a static method" when refs go stale.
		private Pixmap _cache;
		public Action<float> OnHueChanged;

		public HueSlider( Widget parent ) : base( parent )
		{
			FixedHeight = 18;
			MinimumWidth = 256;
			Cursor = CursorShape.SplitH;
			MouseTracking = true;
		}

		public void SetHue( float h )
		{
			_hue = h;
			Update();
		}

		protected override void OnPaint()
		{
			var rect = LocalRect;
			if ( _cache == null ) _cache = BuildHuePixmap( 360, 1 );
			Paint.Draw( rect, _cache );

			var x = rect.Left + _hue * rect.Width;
			Paint.SetPen( Color.White, 2 );
			Paint.ClearBrush();
			Paint.DrawLine( new Vector2( x, rect.Top - 1 ), new Vector2( x, rect.Bottom + 1 ) );
			Paint.SetPen( Color.Black, 1 );
			Paint.DrawLine( new Vector2( x - 1, rect.Top - 1 ), new Vector2( x - 1, rect.Bottom + 1 ) );
			Paint.DrawLine( new Vector2( x + 1, rect.Top - 1 ), new Vector2( x + 1, rect.Bottom + 1 ) );
		}

		private static Pixmap BuildHuePixmap( int w, int h )
		{
			var pix = new Pixmap( w, h );
			using ( Paint.ToPixmap( pix ) )
			{
				Paint.ClearPen();
				for ( int x = 0; x < w; x++ )
				{
					// ColorHsv.Hue is 0..360 (degrees) — span the rainbow that way.
					var hueDeg = (x / (float)(w - 1)) * 360f;
					var c = (Color)new ColorHsv( hueDeg, 1f, 1f, 1f );
					Paint.SetBrush( c );
					Paint.DrawRect( new Rect( x, 0, 1, h ) );
				}
			}
			return pix;
		}

		protected override void OnMousePress( MouseEvent e ) { if ( e.LeftMouseButton ) UpdateFromMouse( e.LocalPosition ); }
		protected override void OnMouseMove( MouseEvent e ) { if ( (e.ButtonState & MouseButtons.Left) != 0 ) UpdateFromMouse( e.LocalPosition ); }

		private void UpdateFromMouse( Vector2 p )
		{
			_hue = Math.Clamp( p.x / Width, 0f, 1f );
			OnHueChanged?.Invoke( _hue );
			Update();
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Subwidget: Alpha slider (transparent → currentColor)
	// ─────────────────────────────────────────────────────────────────────

	private sealed class AlphaSlider : Widget
	{
		private float _alpha;
		private Color _baseColor = Color.White;
		public Action<float> OnAlphaChanged;

		public AlphaSlider( Widget parent ) : base( parent )
		{
			FixedHeight = 18;
			MinimumWidth = 256;
			Cursor = CursorShape.SplitH;
			MouseTracking = true;
		}

		public void SetState( Color baseColor, float alpha )
		{
			_baseColor = baseColor;
			_alpha = alpha;
			Update();
		}

		public void SetBaseColor( Color baseColor )
		{
			_baseColor = baseColor;
			Update();
		}

		protected override void OnPaint()
		{
			var rect = LocalRect;

			// Checkerboard bg.
			Paint.ClearPen();
			const float cell = 8;
			var cols = (int)MathF.Ceiling( rect.Width / cell );
			var rows = (int)MathF.Ceiling( rect.Height / cell );
			Paint.SetBrush( new Color( 0.85f, 0.85f, 0.85f ) );
			Paint.DrawRect( rect );
			Paint.SetBrush( new Color( 0.5f, 0.5f, 0.5f ) );
			for ( int y = 0; y < rows; y++ )
				for ( int x = 0; x < cols; x++ )
					if ( ((x + y) & 1) != 0 )
						Paint.DrawRect( new Rect( rect.Left + x * cell, rect.Top + y * cell, cell, cell ) );

			// Manual horizontal gradient by drawing N narrow rects with alpha ramp.
			const int steps = 64;
			var stepW = rect.Width / steps;
			for ( int i = 0; i < steps; i++ )
			{
				var t = i / (float)(steps - 1);
				var c = _baseColor.WithAlpha( t );
				Paint.SetBrush( c );
				Paint.DrawRect( new Rect( rect.Left + i * stepW, rect.Top, stepW + 1, rect.Height ) );
			}

			// Position knob.
			var x2 = rect.Left + _alpha * rect.Width;
			Paint.SetPen( Color.White, 2 );
			Paint.ClearBrush();
			Paint.DrawLine( new Vector2( x2, rect.Top - 1 ), new Vector2( x2, rect.Bottom + 1 ) );
			Paint.SetPen( Color.Black, 1 );
			Paint.DrawLine( new Vector2( x2 - 1, rect.Top - 1 ), new Vector2( x2 - 1, rect.Bottom + 1 ) );
			Paint.DrawLine( new Vector2( x2 + 1, rect.Top - 1 ), new Vector2( x2 + 1, rect.Bottom + 1 ) );
		}

		protected override void OnMousePress( MouseEvent e ) { if ( e.LeftMouseButton ) UpdateFromMouse( e.LocalPosition ); }
		protected override void OnMouseMove( MouseEvent e ) { if ( (e.ButtonState & MouseButtons.Left) != 0 ) UpdateFromMouse( e.LocalPosition ); }

		private void UpdateFromMouse( Vector2 p )
		{
			_alpha = Math.Clamp( p.x / Width, 0f, 1f );
			OnAlphaChanged?.Invoke( _alpha );
			Update();
		}
	}
}
