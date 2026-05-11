using System;
using Editor;
using Sandbox;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Top toolbar — built from scratch as a pure <see cref="Widget"/> (no
/// <c>Editor.ToolBar</c> chrome, no <c>Editor.Button</c>, no <c>TabWidget</c>).
/// Background, buttons, dropdowns are all custom-painted and measure their
/// own text via <c>Paint.MeasureText</c> on first paint so labels never clip.
/// </summary>
public sealed class SuiTopBar : Widget
{
	public SuiTopBar( Widget parent = null ) : base( parent )
	{
		FixedHeight = 52;
		SetStyles( "background-color: rgb(30,30,29);" ); // #1e1e1d — hsl(60,2%,12%)

		Layout = Layout.Row();
		Layout.Margin = new Sandbox.UI.Margin( 14, 8, 14, 8 );
		Layout.Spacing = 4;
	}

	public SuiTopBarButton AddButton( string label, string icon, Action onClick, bool hasChevron = false, string tooltip = null )
	{
		var btn = new SuiTopBarButton( label, icon, hasChevron );
		btn.ToolTip = tooltip ?? label;
		btn.Clicked += onClick;
		Layout.Add( btn );
		return btn;
	}

	public SuiTopBarDropdown AddDropdown( string icon, string label, string value, Action<Widget> onOpen, string tooltip = null )
	{
		var dd = new SuiTopBarDropdown( icon, label, value );
		dd.ToolTip = tooltip;
		dd.Clicked += () => onOpen?.Invoke( dd );
		Layout.Add( dd );
		return dd;
	}

	public void AddGap( int px = 16 )
	{
		var spacer = new Widget( this );
		spacer.FixedWidth = px;
		spacer.SetStyles( "background-color: transparent;" );
		Layout.Add( spacer );
	}

	/// <summary>Subtle vertical 1px line between toolbar groups (mockup spec).</summary>
	public void AddSeparator()
	{
		var sep = new SuiTopBarSeparator();
		Layout.Add( sep );
	}

	public void AddStretch() => Layout.AddStretchCell();

	protected override void OnPaint()
	{
		// Solid dark background — #1e1e1d / hsl(60,2%,12%) — + subtle bottom border.
		Paint.SetBrushAndPen( new Color( 30 / 255f, 30 / 255f, 29 / 255f ) );
		Paint.DrawRect( LocalRect );

		Paint.SetPen( Color.White.WithAlpha( 0.06f ) );
		Paint.DrawLine( new Vector2( 0, Height - 1 ), new Vector2( Width, Height - 1 ) );
	}
}

/// <summary>
/// Top-bar button — icon + label, optional chevron, hover/press/active states.
/// Width is calibrated on first paint via <c>Paint.MeasureText</c> so the
/// label never clips regardless of font metrics or DPI scaling.
/// </summary>
public sealed class SuiTopBarButton : Widget
{
	public string Label { get; set; }
	public string Icon { get; set; }
	public bool HasChevron { get; set; }
	public bool IsActive { get; set; }
	public event Action Clicked;

	private bool _hover;
	private bool _pressed;
	private bool _sized;

	private const int FontSize = 12;
	private const int PadL = 14;
	private const int PadR = 14;
	private const int IconSize = 16;
	private const int IconLabelGap = 8;
	private const int ChevronW = 14;
	// Icon-only mode (label is empty): square, larger icon for tap target.
	private const int IconOnlySize = 38;
	private const int IconOnlyIconPx = 18;

	public bool IsIconOnly => string.IsNullOrEmpty( Label );

	public SuiTopBarButton( string label, string icon, bool hasChevron, Widget parent = null ) : base( parent )
	{
		Label = label ?? "";
		Icon = icon;
		HasChevron = hasChevron;
		Cursor = CursorShape.Finger;
		// Kill the default Qt widget background so the parent toolbar's color
		// shows through wherever we don't paint our own tint.
		SetStyles( "background-color: transparent;" );

		if ( IsIconOnly )
		{
			FixedHeight = IconOnlySize;
			FixedWidth = IconOnlySize;
		}
		else
		{
			FixedHeight = 36;
			// Conservative initial width — replaced on first paint with real measurement.
			var iconBlock = string.IsNullOrEmpty( icon ) ? 0 : IconSize + IconLabelGap;
			var chev = hasChevron ? ChevronW : 0;
			FixedWidth = PadL + iconBlock + (Label.Length * 12) + chev + PadR;
		}
	}

	protected override void OnPaint()
	{
		Paint.SetDefaultFont( FontSize );

		// Icon-only buttons are square — no text measurement needed.
		if ( !IsIconOnly && !_sized )
		{
			var textW = string.IsNullOrEmpty( Label ) ? 0 : Paint.MeasureText( Label ).x;
			var iconBlock = string.IsNullOrEmpty( Icon ) ? 0 : IconSize + IconLabelGap;
			var chev = HasChevron ? ChevronW : 0;
			int newW = (int)(PadL + iconBlock + textW + 2 + chev + PadR);
			_sized = true;
			if ( newW != FixedWidth )
			{
				FixedWidth = newW;
				return; // re-layout; engine repaints again
			}
		}

		var rect = LocalRect;

		// Background tint.
		if ( IsActive )
		{
			Paint.SetBrushAndPen( Color.White.WithAlpha( 0.10f ) );
			Paint.DrawRect( rect, 4f );
		}
		else if ( _pressed )
		{
			Paint.SetBrushAndPen( Color.White.WithAlpha( 0.14f ) );
			Paint.DrawRect( rect, 4f );
		}
		else if ( _hover )
		{
			Paint.SetBrushAndPen( Color.White.WithAlpha( 0.06f ) );
			Paint.DrawRect( rect, 4f );
		}

		var textColor = new Color( 232 / 255f, 235 / 255f, 238 / 255f );
		Paint.SetPen( textColor );

		// Icon-only: center the icon in the square.
		if ( IsIconOnly )
		{
			if ( !string.IsNullOrEmpty( Icon ) )
			{
				var iconRect = new Rect( (Width - IconOnlyIconPx) / 2f, (Height - IconOnlyIconPx) / 2f, IconOnlyIconPx, IconOnlyIconPx );
				Paint.DrawIcon( iconRect, Icon, IconOnlyIconPx );
			}
			return;
		}

		// Normal: icon + label + optional chevron.
		float x = PadL;
		if ( !string.IsNullOrEmpty( Icon ) )
		{
			var iconRect = new Rect( x, (Height - IconSize) / 2f, IconSize, IconSize );
			Paint.DrawIcon( iconRect, Icon, IconSize );
			x += IconSize + IconLabelGap;
		}

		float labelW = 0;
		if ( !string.IsNullOrEmpty( Label ) )
		{
			labelW = Paint.MeasureText( Label ).x;
			var labelRect = new Rect( x, 0, labelW + 2, Height );
			Paint.DrawText( labelRect, Label, TextFlag.LeftCenter );
			x += labelW + 2;
		}

		if ( HasChevron )
		{
			Paint.SetPen( new Color( 165 / 255f, 172 / 255f, 182 / 255f ), 1.5f );
			var cx = Width - PadR - 2f;
			var cy = Height / 2f + 1f;
			Paint.DrawLine( new Vector2( cx - 4, cy - 2 ), new Vector2( cx, cy + 2 ) );
			Paint.DrawLine( new Vector2( cx, cy + 2 ), new Vector2( cx + 4, cy - 2 ) );
		}
	}

	protected override void OnMouseEnter()  { _hover = true; Update(); }
	protected override void OnMouseLeave()  { _hover = false; _pressed = false; Update(); }
	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton ) { _pressed = true; Update(); }
	}
	protected override void OnMouseReleased( MouseEvent e )
	{
		if ( _pressed && e.LeftMouseButton )
		{
			_pressed = false;
			bool shouldFire = _hover;
			// Callback may rebuild parent and destroy this. Don't touch Qt
			// state afterwards unless we still exist.
			if ( shouldFire ) Clicked?.Invoke();
			if ( IsValid ) Update();
		}
	}
}

/// <summary>
/// Pill dropdown — icon + muted label + bright value + chevron. Click opens
/// a menu (host is responsible for that). Width measured on first paint.
/// </summary>
public sealed class SuiTopBarDropdown : Widget
{
	public string Icon { get; set; }
	private string _label;
	private string _value;
	public string Label
	{
		get => _label;
		set { _label = value ?? ""; _sized = false; Update(); }
	}
	public string Value
	{
		get => _value;
		set { _value = value ?? ""; _sized = false; Update(); }
	}
	public event Action Clicked;

	private bool _hover;
	private bool _sized;

	private const int FontSize = 12;
	private const int PadL = 6;          // Outer left padding (whole widget)
	private const int PadR = 6;          // Outer right padding
	private const int IconSize = 14;
	private const int IconGap = 8;       // Gap icon → label
	private const int LabelToPillGap = 8;// Gap between flat label and value-pill
	private const int PillPadL = 10;     // Inner pill left padding (around value)
	private const int PillPadR = 10;     // Inner pill right padding (around chevron)
	private const int ValueChevronGap = 6;
	private const int ChevronW = 8;
	private const int PillHeight = 26;

	public SuiTopBarDropdown( string icon, string label, string value, Widget parent = null ) : base( parent )
	{
		Icon = icon;
		_label = label ?? "";
		_value = value ?? "";
		Cursor = CursorShape.Finger;
		FixedHeight = 36;
		SetStyles( "background-color: transparent;" );

		// Conservative initial width — gets calibrated on first paint.
		var iconBlock = string.IsNullOrEmpty( icon ) ? 0 : IconSize + IconGap;
		var labelBlock = _label.Length > 0 ? (_label.Length * 12) + LabelToPillGap : 0;
		var valueBlock = _value.Length * 12;
		FixedWidth = PadL + iconBlock + labelBlock + PillPadL + valueBlock + ValueChevronGap + ChevronW + PillPadR + PadR;
	}

	protected override void OnPaint()
	{
		Paint.SetDefaultFont( FontSize );

		// First-paint calibration with real text widths.
		if ( !_sized )
		{
			var labelW = string.IsNullOrEmpty( _label ) ? 0 : Paint.MeasureText( _label ).x;
			var valueW = string.IsNullOrEmpty( _value ) ? 0 : Paint.MeasureText( _value ).x;
			var iconBlock = string.IsNullOrEmpty( Icon ) ? 0 : IconSize + IconGap;
			var labelBlock = labelW > 0 ? (int)labelW + LabelToPillGap : 0;
			int newW = PadL + iconBlock + labelBlock + PillPadL + (int)valueW + 2 + ValueChevronGap + ChevronW + PillPadR + PadR;
			_sized = true;
			if ( newW != FixedWidth )
			{
				FixedWidth = newW;
				return;
			}
		}

		// 1. ICON + LABEL — flat, no background.
		float x = PadL;
		if ( !string.IsNullOrEmpty( Icon ) )
		{
			Paint.SetPen( new Color( 195 / 255f, 200 / 255f, 208 / 255f ) );
			var ir = new Rect( x, (Height - IconSize) / 2f, IconSize, IconSize );
			Paint.DrawIcon( ir, Icon, IconSize );
			x += IconSize + IconGap;
		}

		float labelW2 = 0;
		if ( !string.IsNullOrEmpty( _label ) )
		{
			Paint.SetPen( new Color( 220 / 255f, 224 / 255f, 230 / 255f ) );
			labelW2 = Paint.MeasureText( _label ).x;
			var lr = new Rect( x, 0, labelW2 + 2, Height );
			Paint.DrawText( lr, _label, TextFlag.LeftCenter );
			x += labelW2 + LabelToPillGap;
		}

		// 2. VALUE PILL — only around the value + chevron.
		var pillStart = x;
		var pillEnd = Width - PadR;
		var pillRect = new Rect( pillStart, (Height - PillHeight) / 2f, pillEnd - pillStart, PillHeight );

		// Pill harmonized with toolbar HSL(60,2%,*): bg=hsl(60,2%,15%) / hover=hsl(60,2%,18%).
		var pillBg = _hover
			? new Color( 46 / 255f, 46 / 255f, 45 / 255f )  // #2e2e2d
			: new Color( 38 / 255f, 38 / 255f, 37 / 255f ); // #262625
		Paint.SetBrush( pillBg );
		Paint.SetPen( Color.White.WithAlpha( 0.08f ) );
		Paint.DrawRect( pillRect, 4f );

		// 3. VALUE inside pill.
		float vx = pillStart + PillPadL;
		if ( !string.IsNullOrEmpty( _value ) )
		{
			Paint.SetPen( new Color( 232 / 255f, 235 / 255f, 238 / 255f ) );
			var vw = Paint.MeasureText( _value ).x;
			var vr = new Rect( vx, pillRect.Top, vw + 2, pillRect.Height );
			Paint.DrawText( vr, _value, TextFlag.LeftCenter );
		}

		// 4. CHEVRON at right of pill.
		Paint.SetPen( new Color( 165 / 255f, 172 / 255f, 182 / 255f ), 1.5f );
		var cx = pillEnd - PillPadR;
		var cy = pillRect.Top + pillRect.Height / 2f + 1f;
		Paint.DrawLine( new Vector2( cx - 4, cy - 2 ), new Vector2( cx, cy + 2 ) );
		Paint.DrawLine( new Vector2( cx, cy + 2 ), new Vector2( cx + 4, cy - 2 ) );
	}

	protected override void OnMouseEnter() { _hover = true; Update(); }
	protected override void OnMouseLeave() { _hover = false; Update(); }
	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton ) Clicked?.Invoke();
	}
}

/// <summary>
/// Subtle vertical 1px line that visually separates groups in the top bar.
/// Centered vertically, doesn't span full toolbar height.
/// </summary>
public sealed class SuiTopBarSeparator : Widget
{
	public SuiTopBarSeparator( Widget parent = null ) : base( parent )
	{
		FixedWidth = 13; // 1px line + 6px padding each side
		FixedHeight = 36;
		SetStyles( "background-color: transparent;" );
	}

	protected override void OnPaint()
	{
		// 1px vertical line, 60% of widget height, centered.
		float lineHeight = Height * 0.55f;
		float yTop = (Height - lineHeight) / 2f;
		float xCenter = Width / 2f;

		Paint.SetPen( Color.White.WithAlpha( 0.08f ) );
		Paint.DrawLine( new Vector2( xCenter, yTop ), new Vector2( xCenter, yTop + lineHeight ) );
	}
}
