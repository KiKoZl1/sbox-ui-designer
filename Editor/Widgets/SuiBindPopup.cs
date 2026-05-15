using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Modal dialog for creating or editing a property binding (PRD 18 § 4.9).
///
/// M1-C1 scope: element + property + source Variable + mode — a direct
/// Variable→property bind, the ~80% case. The converter-chain builder,
/// type-mismatch auto-suggest, and the FallbackValue field land in M1-C2.
///
/// Two entry paths:
///  • Path A — opened from the Details 🔗 icon (M1-C2): element + property fixed.
///  • Path B — opened from the Bindings panel "+ Add Binding": both selectable.
///    The window prefills <see cref="_element"/> with whatever is currently
///    selected in the canvas / Hierarchy.
/// </summary>
public sealed class SuiBindPopup : Window
{
	/// <summary>Invoked on OK with the target element id and the built binding.</summary>
	public Action<string, SuiBinding> OnAccept;

	private readonly SuiDocument _doc;
	private readonly bool _lockTarget;
	private readonly string _editingBindingId;

	private SuiElement _element;
	private string _property;
	private string _sourceVariableId;
	private SuiBindingMode _mode = SuiBindingMode.OneWay;

	private Button _elementBtn;
	private Button _propertyBtn;
	private Label _expectsLabel;
	private Label _error;

	public SuiBindPopup( SuiDocument doc, SuiElement element = null, string lockedProperty = null, SuiBinding existing = null )
	{
		_doc = doc;
		_element = element;
		_property = lockedProperty ?? existing?.Property;
		_lockTarget = element != null && !string.IsNullOrEmpty( lockedProperty );
		_editingBindingId = existing?.Id;
		_sourceVariableId = existing?.Source?.VariableId;
		_mode = existing?.Mode ?? SuiBindingMode.OneWay;

		Title = existing != null ? "Edit Binding" : "Bind Property";
		WindowTitle = Title;
		Size = new Vector2( 460, 300 );
		MinimumSize = new Vector2( 460, 300 );
		SetWindowIcon( "link" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 14;
		Canvas.Layout.Spacing = 8;

		// Pre-fill defaults BEFORE BuildLayout so the visual "selected" state on
		// every combo / button matches the in-memory state. Without this, a user
		// opening the popup and clicking OK without explicitly picking each item
		// got "Pick a source Variable" errors despite the dropdowns clearly
		// showing the first option highlighted.
		PreInitState();
		BuildLayout();
		Show();
	}

	private void PreInitState()
	{
		if ( _element == null && _doc?.Elements != null )
		{
			foreach ( var el in _doc.Elements )
			{
				if ( el != null && !string.IsNullOrEmpty( el.Id ) ) { _element = el; break; }
			}
		}
		if ( string.IsNullOrEmpty( _property ) && _element != null )
		{
			foreach ( var p in SuiBindingModeMatrix.BindableProperties( _element.Type ) )
			{
				_property = p; break;
			}
		}
		if ( string.IsNullOrEmpty( _sourceVariableId ) && _doc?.Variables != null )
		{
			foreach ( var v in _doc.Variables )
			{
				if ( v != null && !string.IsNullOrEmpty( v.Id ) ) { _sourceVariableId = v.Id; break; }
			}
		}
		// Editing an existing binding preserves its mode; for fresh bindings, use
		// the per-(type, property) default from the matrix.
		if ( _editingBindingId == null && _element != null && !string.IsNullOrEmpty( _property ) )
			_mode = SuiBindingModeMatrix.DefaultMode( _element.Type, _property );
	}

	private void BuildLayout()
	{
		// ── Element ──
		if ( _lockTarget )
			AddReadonlyRow( "Element", _element?.Name ?? "(none)" );
		else
			AddElementButtonRow();

		// ── Property ──
		if ( _lockTarget )
			AddReadonlyRow( "Property", _property ?? "(none)" );
		else
			AddPropertyButtonRow();

		// ── Source Variable ──
		var sourceCombo = AddComboRow( "Source" );
		var vars = _doc?.Variables ?? new List<SuiVariable>();
		if ( vars.Count == 0 )
		{
			sourceCombo.AddItem( "(no variables — add one first)", null, null );
		}
		else
		{
			foreach ( var v in vars )
			{
				if ( v == null ) continue;
				var captured = v;
				sourceCombo.AddItem( $"{v.Name}  ({v.Type})", SuiTypeIcons.ForType( v.Type ),
					() => { _sourceVariableId = captured.Id; UpdateExpectsLabel(); },
					null, captured.Id == _sourceVariableId );
			}
		}

		// ── Expects (target-type hint, updates when property changes) ──
		_expectsLabel = new Label( "", Canvas );
		_expectsLabel.SetStyles( "color: #9ca3af; font-size: 10px; padding-left: 98px;" );
		Canvas.Layout.Add( _expectsLabel );
		UpdateExpectsLabel();

		// ── Mode ──
		var modeCombo = AddComboRow( "Mode" );
		foreach ( var m in new[] { SuiBindingMode.OneWay, SuiBindingMode.OneTime, SuiBindingMode.TwoWay } )
		{
			var captured = m;
			modeCombo.AddItem( m.ToString(), "swap_horiz", () => _mode = captured, null, m == _mode );
		}

		_error = new Label( "", Canvas );
		_error.SetStyles( "color: #ef4444; font-size: 11px;" );
		_error.Visible = false;
		Canvas.Layout.Add( _error );

		Canvas.Layout.AddStretchCell();

		// ── Buttons ──
		var buttons = new Widget( Canvas );
		buttons.Layout = Layout.Row();
		buttons.Layout.Spacing = 8;
		buttons.Layout.AddStretchCell();
		var cancel = new Button( "Cancel", buttons );
		cancel.Clicked = Close;
		buttons.Layout.Add( cancel );
		var ok = new Button( "OK", buttons );
		ok.Clicked = Accept;
		buttons.Layout.Add( ok );
		Canvas.Layout.Add( buttons );
	}

	// ── Element picker (Button + Menu — rebuilt each open, so it stays accurate
	//    if the document gains/loses elements while the popup is open) ──

	private void AddElementButtonRow()
	{
		var row = new Widget( Canvas );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 8;
		var lbl = new Label( "Element", row ) { FixedWidth = 90 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );

		_elementBtn = new Button( _element?.Name ?? "(pick…)", "category", row );
		_elementBtn.FixedHeight = 26;
		_elementBtn.SetStyles( FieldChrome );
		_elementBtn.Clicked = OpenElementMenu;
		row.Layout.Add( _elementBtn, 1 );

		Canvas.Layout.Add( row );
	}

	private void OpenElementMenu()
	{
		var menu = new Menu( _elementBtn );
		foreach ( var el in _doc?.Elements ?? new List<SuiElement>() )
		{
			if ( el == null || string.IsNullOrEmpty( el.Id ) ) continue;
			var captured = el;
			menu.AddOption( el.Name ?? el.Id, "category", () => SelectElement( captured ) );
		}
		menu.OpenAtCursor( true );
	}

	private void SelectElement( SuiElement el )
	{
		_element = el;
		if ( _elementBtn != null ) _elementBtn.Text = el?.Name ?? "(pick…)";

		// Element changed — the property dropdown content is element-type-specific,
		// so reset to the first valid prop for the new type.
		_property = null;
		if ( el != null )
		{
			foreach ( var p in SuiBindingModeMatrix.BindableProperties( el.Type ) )
			{
				_property = p; break;
			}
			if ( !string.IsNullOrEmpty( _property ) )
				_mode = SuiBindingModeMatrix.DefaultMode( el.Type, _property );
		}
		if ( _propertyBtn != null )
			_propertyBtn.Text = FormatPropertyButtonText( el, _property );
		UpdatePropertyButtonStyle();
		UpdateExpectsLabel();
	}

	// ── Property picker (Button + Menu — dynamic per element type) ──

	private void AddPropertyButtonRow()
	{
		var row = new Widget( Canvas );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 8;
		var lbl = new Label( "Property", row ) { FixedWidth = 90 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );

		_propertyBtn = new Button( FormatPropertyButtonText( _element, _property ), "tune", row );
		_propertyBtn.FixedHeight = 26;
		_propertyBtn.Clicked = OpenPropertyMenu;
		row.Layout.Add( _propertyBtn, 1 );
		UpdatePropertyButtonStyle();

		Canvas.Layout.Add( row );
	}

	private void OpenPropertyMenu()
	{
		if ( _element == null )
		{
			ShowError( "Pick an element first." );
			return;
		}
		var menu = new Menu( _propertyBtn );
		// Reset the menu's own colour so the button's accent (which cascades
		// into the menu) doesn't paint every item the same hue.
		menu.SetStyles( "color: #e5e7eb;" );

		// Capture each option's target type in order. `Menu.Option` itself isn't
		// a Widget (no SetStyles), but the menu's internal child widgets — one
		// per AddOption call — DO accept SetStyles, and they come back through
		// `menu.Children` in insertion order. After populating, we tint each
		// child with its type's accent so the dropdown reads as a colour-coded
		// type list.
		var typesByIndex = new System.Collections.Generic.List<string>();

		bool any = false;
		foreach ( var p in SuiBindingModeMatrix.BindableProperties( _element.Type ) )
		{
			var captured = p;
			var type = SuiBindingModeMatrix.GetTargetType( _element.Type, captured );
			typesByIndex.Add( type );
			var icon = SuiTypeRegistry.Icon( type );
			menu.AddOption( captured, icon, () =>
			{
				_property = captured;
				if ( _propertyBtn != null )
					_propertyBtn.Text = FormatPropertyButtonText( _element, captured );
				_mode = SuiBindingModeMatrix.DefaultMode( _element.Type, captured );
				UpdatePropertyButtonStyle();
				UpdateExpectsLabel();
			} );
			any = true;
		}
		if ( !any )
		{
			menu.AddOption( "(no bindable properties)", null, null );
			typesByIndex.Add( null );
		}

		TintMenuItems( menu, typesByIndex );
		menu.OpenAtCursor( true );
	}

	/// <summary>
	/// Per-item colour for a populated <see cref="Menu"/>. Iterates the menu's
	/// child widgets (one per <c>AddOption</c> call, in insertion order) and
	/// calls <c>SetStyles</c> with the type's accent colour. Best-effort — if
	/// the engine ever changes Menu's internal layout, we silently fall back
	/// to the menu's default colour rather than crashing.
	/// </summary>
	private static void TintMenuItems( Menu menu, System.Collections.Generic.List<string> typesByIndex )
	{
		if ( menu == null || typesByIndex == null ) return;
		int idx = 0;
		foreach ( var child in menu.Children )
		{
			if ( idx >= typesByIndex.Count ) break;
			var t = typesByIndex[idx++];
			if ( string.IsNullOrEmpty( t ) ) continue;
			try { child.SetStyles( $"color: {SuiTypeRegistry.Color( t )};" ); }
			catch { /* best-effort tint */ }
		}
	}

	private void Accept()
	{
		if ( _element == null ) { ShowError( "Pick a target element." ); return; }
		if ( string.IsNullOrEmpty( _property ) ) { ShowError( "Pick a target property." ); return; }
		if ( !SuiBindingModeMatrix.IsBindable( _element.Type, _property ) )
		{
			ShowError( $"'{_property}' is not bindable on a {_element.Type} element." );
			return;
		}
		if ( string.IsNullOrEmpty( _sourceVariableId ) ) { ShowError( "Pick a source Variable." ); return; }
		if ( !SuiBindingModeMatrix.IsModeAllowed( _element.Type, _property, _mode ) )
		{
			ShowError( $"{_mode} mode is not valid for {_element.Type}.{_property}." );
			return;
		}

		var binding = new SuiBinding
		{
			Id = _editingBindingId ?? SuiBinding.NewBindingId(),
			Property = _property,
			Mode = _mode,
			Source = new SuiBindingSource { VariableId = _sourceVariableId },
		};

		OnAccept?.Invoke( _element.Id, binding );
		Close();
	}

	private void ShowError( string msg )
	{
		_error.Text = msg;
		_error.Visible = true;
	}

	/// <summary>
	/// Property-button label — just the property name. The type is communicated
	/// visually by <see cref="UpdatePropertyButtonStyle"/> (tints the border /
	/// foreground with the type's accent) and by <see cref="UpdateExpectsLabel"/>
	/// (the "Expects: X" caption), so we don't repeat the type as text here.
	/// </summary>
	private static string FormatPropertyButtonText( SuiElement el, string property )
		=> string.IsNullOrEmpty( property ) ? "(pick…)" : property;

	/// <summary>
	/// Refresh the type-compatibility caption from the current element + property
	/// + source variable. Distinguishes four states so the user knows exactly
	/// what will happen rather than guessing whether <c>"Expects: float"</c>
	/// means the bind is broken:
	///   • No source yet         → "Expects: {target}"          (neutral target colour)
	///   • Same type              → "✓ Direct bind ({type})"     (green)
	///   • Numeric auto-convert   → "✓ Auto-converts — int → float"  (green)
	///   • Needs converter        → "Needs converter: X → Y"     (amber)
	///   • Truly incompatible     → "⚠ Type mismatch: X → Y"     (red)
	/// </summary>
	private void UpdateExpectsLabel()
	{
		if ( _expectsLabel == null ) return;

		var targetType = ( _element != null && !string.IsNullOrEmpty( _property ) )
			? SuiBindingModeMatrix.GetTargetType( _element.Type, _property )
			: null;
		if ( string.IsNullOrEmpty( targetType ) )
		{
			_expectsLabel.Text = "";
			return;
		}

		// Resolve the source variable's TypeRef (if any source is picked yet).
		string sourceType = null;
		if ( !string.IsNullOrEmpty( _sourceVariableId ) && _doc?.Variables != null )
		{
			foreach ( var v in _doc.Variables )
			{
				if ( v?.Id == _sourceVariableId ) { sourceType = v.Type; break; }
			}
		}

		string text;
		string color;

		if ( string.IsNullOrEmpty( sourceType ) )
		{
			// No source picked yet — show what the property accepts, in its accent.
			text  = $"Expects: {targetType}";
			color = SuiTypeRegistry.Color( targetType );
		}
		else if ( SameType( sourceType, targetType ) )
		{
			text  = $"✓ Direct bind  —  {sourceType}";
			color = "#34d399"; // emerald — success
		}
		else if ( SuiConverterSuggester.TypesCompatible( sourceType, targetType ) )
		{
			// Numeric widening / shrinking — the generator will emit the cast.
			text  = $"✓ Auto-converts  —  {sourceType} → {targetType}";
			color = "#34d399"; // emerald — success
		}
		else
		{
			var suggested = SuiConverterSuggester.Suggest( sourceType, targetType );
			if ( !string.IsNullOrEmpty( suggested ) )
			{
				var name = suggested.StartsWith( "builtin." )
					? suggested.Substring( "builtin.".Length )
					: suggested;
				text  = $"Needs converter:  {name}()  —  {sourceType} → {targetType}";
				color = "#fbbf24"; // amber — informational
			}
			else
			{
				text  = $"⚠ Type mismatch  —  {sourceType} → {targetType}";
				color = "#ef4444"; // red — error
			}
		}

		_expectsLabel.Text = text;
		_expectsLabel.SetStyles(
			$"color: {color}; font-size: 10px; font-weight: 600; padding-left: 98px;" );
	}

	/// <summary>True when two TypeRefs name the same type — handles the TypeRef/C#-name vocab gap (e.g. <c>"int"</c> ↔ <c>"Int32"</c>).</summary>
	private static bool SameType( string a, string b )
	{
		static string Norm( string t ) => t switch
		{
			"int"     => "Int32",
			"long"    => "Int64",
			"float"   => "Single",
			"double"  => "Double",
			"bool"    => "Boolean",
			"string"  => "String",
			_         => t,
		};
		return Norm( a ) == Norm( b );
	}

	/// <summary>
	/// Tint the property button's chrome with the target type's accent — gives
	/// the button itself a visual hint of what kind of value the binding will
	/// drive into the property.
	/// </summary>
	private void UpdatePropertyButtonStyle()
	{
		if ( _propertyBtn == null ) return;

		string color = "#9ca3af"; // neutral when no property picked yet
		if ( _element != null && !string.IsNullOrEmpty( _property ) )
		{
			var t = SuiBindingModeMatrix.GetTargetType( _element.Type, _property );
			if ( !string.IsNullOrEmpty( t ) )
				color = SuiTypeRegistry.Color( t );
		}

		_propertyBtn.SetStyles(
			"background-color: rgb(20,20,19);" +
			$"border: 1px solid {color}55;" +
			"border-radius: 3px;" +
			"padding: 0 6px;" +
			$"color: {color};" );
	}

	// ── row helpers ──

	private const string FieldChrome =
		"background-color: rgb(20,20,19);" +
		"border: 1px solid rgba(255,255,255,0.12);" +
		"border-radius: 3px;" +
		"padding: 0 6px;" +
		"color: #e5e7eb;";

	private ComboBox AddComboRow( string label )
	{
		var row = new Widget( Canvas );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 8;
		var lbl = new Label( label, row ) { FixedWidth = 90 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );

		var combo = new ComboBox( row );
		combo.FixedHeight = 26;
		combo.SetStyles( FieldChrome );
		row.Layout.Add( combo, 1 );

		Canvas.Layout.Add( row );
		return combo;
	}

	private void AddReadonlyRow( string label, string value )
	{
		var row = new Widget( Canvas );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 8;
		var lbl = new Label( label, row ) { FixedWidth = 90 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );

		var val = new Label( value, row );
		val.SetStyles( "color: #e5e7eb; font-size: 12px; font-weight: 500;" );
		row.Layout.Add( val, 1 );

		Canvas.Layout.Add( row );
	}
}
