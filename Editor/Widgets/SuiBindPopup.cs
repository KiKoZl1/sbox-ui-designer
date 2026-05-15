using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Modal dialog for creating or editing a property binding (PRD 18 § 4.9).
///
/// M1-C2-a scope: element + property + source Variable + mode + converter chain.
/// Each chain step picks a converter from the catalog; additional inputs (any
/// past the implicit chain-fed first input) are filled with document Variables.
/// The "Expects" caption considers the chain's effective output type, so
/// bridging a type mismatch with <c>Divide(Health, MaxHealth)</c> reads as
/// "Direct bind / Auto-converts / Needs converter" correctly.
///
/// Two entry paths:
///  • Path A — opened from the Details 🔗 icon (M1-C2-b): element + property fixed.
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

	// V1.5 converter chain — applied source → step₀ → step₁ → … → property.
	private List<SuiBindingConverterStep> _converterSteps = new();

	private Button _elementBtn;
	private Button _propertyBtn;
	private Label _expectsLabel;
	private Widget _chainHost;
	private Button _addConverterBtn;
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

		// Clone an existing chain so edits inside the popup don't mutate the live
		// binding until OK is pressed.
		if ( existing?.Converters != null )
		{
			foreach ( var s in existing.Converters )
				if ( s != null ) _converterSteps.Add( s.Clone() );
		}

		Title = existing != null ? "Edit Binding" : "Bind Property";
		WindowTitle = Title;
		Size = new Vector2( 520, 440 );
		MinimumSize = new Vector2( 520, 440 );
		SetWindowIcon( "link" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 14;
		Canvas.Layout.Spacing = 8;

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

		// ── Expects (target-type hint) ──
		_expectsLabel = new Label( "", Canvas );
		_expectsLabel.SetStyles( "color: #9ca3af; font-size: 10px; padding-left: 98px;" );
		Canvas.Layout.Add( _expectsLabel );

		// ── Converter chain ──
		AddChainSection();

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

		// Initial chain UI + expects label now that everything exists.
		RebuildChainUI();
		UpdateExpectsLabel();
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Converter chain section (M1-C2-a)
	// ─────────────────────────────────────────────────────────────────────

	private void AddChainSection()
	{
		// Header row: "Converters" label + "+ Add Converter" button.
		var header = new Widget( Canvas );
		header.Layout = Layout.Row();
		header.Layout.Spacing = 8;

		var lbl = new Label( "Converters", header ) { FixedWidth = 90 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		header.Layout.Add( lbl );

		_addConverterBtn = new Button( "+ Add Converter", "add", header );
		_addConverterBtn.FixedHeight = 24;
		_addConverterBtn.SetStyles(
			"background-color: rgb(24,24,23);" +
			"border: 1px solid rgba(255,255,255,0.1);" +
			"border-radius: 3px; padding: 0 8px;" +
			"color: #e5e7eb; font-size: 10px;" );
		_addConverterBtn.Clicked = OpenAddConverterMenu;
		header.Layout.Add( _addConverterBtn );
		header.Layout.AddStretchCell();

		Canvas.Layout.Add( header );

		// Host for step rows (built by RebuildChainUI).
		_chainHost = new Widget( Canvas );
		_chainHost.Layout = Layout.Column();
		_chainHost.Layout.Spacing = 2;
		Canvas.Layout.Add( _chainHost );
	}

	private void RebuildChainUI()
	{
		if ( _chainHost?.Layout == null ) return;
		_chainHost.Layout.Clear( true );

		if ( _converterSteps == null || _converterSteps.Count == 0 )
		{
			var empty = new Label( "(no converters — source flows directly into the property)", _chainHost );
			empty.SetStyles( "color: #6b7280; font-size: 10px; padding-left: 98px;" );
			_chainHost.Layout.Add( empty );
			return;
		}

		for ( int i = 0; i < _converterSteps.Count; i++ )
			_chainHost.Layout.Add( BuildStepRow( i, _converterSteps[i] ) );
	}

	private Widget BuildStepRow( int stepIndex, SuiBindingConverterStep step )
	{
		var meta = SuiConverterCatalog.Find( step?.ConverterRef );
		var accent = meta != null ? SuiTypeRegistry.Color( meta.ReturnType ) : "#9ca3af";

		var row = new Widget( _chainHost );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 4;
		row.Layout.Margin = new Sandbox.UI.Margin( 98, 2, 0, 2 );
		row.FixedHeight = 26;

		var idxLbl = new Label( $"#{stepIndex + 1}", row );
		idxLbl.SetStyles( "color: #6b7280; font-size: 9px; padding: 0 4px;" );
		idxLbl.FixedWidth = 22;
		row.Layout.Add( idxLbl );

		var nameLbl = new Label( meta?.DisplayName ?? "(unknown converter)", row );
		nameLbl.SetStyles( $"color: {accent}; font-size: 11px; font-weight: 600;" );
		row.Layout.Add( nameLbl );

		// Args for inputs past the implicit chain-fed first one.
		if ( meta?.Inputs != null && meta.Inputs.Length > 1 )
		{
			for ( int argIdx = 1; argIdx < meta.Inputs.Length; argIdx++ )
			{
				var capturedArgIdx = argIdx;
				var input = meta.Inputs[argIdx];

				while ( step.Args.Count <= argIdx )
					step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.Variable } );

				var argBtn = new Button( ArgButtonText( step.Args[argIdx], input.Name ), "data_object", row );
				argBtn.FixedHeight = 22;
				argBtn.SetStyles(
					"background-color: rgb(24,24,23);" +
					"border: 1px solid rgba(255,255,255,0.12);" +
					"border-radius: 3px; padding: 0 6px;" +
					"color: #e5e7eb; font-size: 10px;" );
				argBtn.Clicked = () => OpenArgPickerMenu( argBtn, step, capturedArgIdx );
				row.Layout.Add( argBtn );
			}
		}

		row.Layout.AddStretchCell();

		var rmBtn = new Button( "", "close", row );
		rmBtn.FixedWidth = 22;
		rmBtn.FixedHeight = 22;
		rmBtn.SetStyles( "background-color: transparent; border: none; color: #6b7280;" );
		rmBtn.Clicked = () =>
		{
			_converterSteps.RemoveAt( stepIndex );
			FixChainRefsAfterMutation();
			RebuildChainUI();
			UpdateExpectsLabel();
		};
		row.Layout.Add( rmBtn );

		return row;
	}

	private string ArgButtonText( SuiConverterArg arg, string paramName )
	{
		if ( arg?.Kind == SuiConverterArgKind.Variable && !string.IsNullOrEmpty( arg.VariableId ) )
		{
			foreach ( var v in _doc?.Variables ?? new List<SuiVariable>() )
				if ( v?.Id == arg.VariableId ) return $"{paramName}: {v.Name}";
			return $"{paramName}: (missing)";
		}
		return $"{paramName}: (pick…)";
	}

	private void OpenAddConverterMenu()
	{
		var menu = new Menu( _addConverterBtn );

		// Group built-ins by category — easier to scan a 51-entry catalog.
		var byCategory = new Dictionary<string, List<SuiConverterMetadata>>();
		foreach ( var c in SuiConverterCatalog.GetBuiltins() )
		{
			if ( c == null ) continue;
			var cat = string.IsNullOrEmpty( c.Category ) ? "Other" : c.Category;
			if ( !byCategory.TryGetValue( cat, out var list ) )
			{
				list = new List<SuiConverterMetadata>();
				byCategory[cat] = list;
			}
			list.Add( c );
		}

		foreach ( var kv in byCategory )
		{
			var sub = menu.AddMenu( kv.Key );
			foreach ( var c in kv.Value )
			{
				var captured = c;
				sub.AddOption( captured.DisplayName, SuiTypeRegistry.Icon( captured.ReturnType ),
					() => AddStep( captured ) );
			}
		}

		menu.OpenAtCursor( true );
	}

	private void AddStep( SuiConverterMetadata meta )
	{
		if ( meta == null ) return;

		var step = new SuiBindingConverterStep
		{
			ConverterRef = meta.Ref,
			Args = new List<SuiConverterArg>(),
		};

		// Args[0] is fed by the chain — "Source" for the first step, "Previous"
		// for every step after.
		var firstRef = _converterSteps.Count == 0 ? "Source" : "Previous";
		step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.ChainRef, ChainRef = firstRef } );

		// Reserve a Variable slot per additional input; the user fills these in
		// via the per-arg button.
		if ( meta.Inputs != null )
		{
			for ( int i = 1; i < meta.Inputs.Length; i++ )
				step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.Variable } );
		}

		_converterSteps.Add( step );
		RebuildChainUI();
		UpdateExpectsLabel();
	}

	private void OpenArgPickerMenu( Button anchor, SuiBindingConverterStep step, int argIdx )
	{
		var menu = new Menu( anchor );
		var vars = _doc?.Variables ?? new List<SuiVariable>();
		if ( vars.Count == 0 )
		{
			menu.AddOption( "(no variables in this document)", null, null );
		}
		else
		{
			foreach ( var v in vars )
			{
				if ( v == null ) continue;
				var captured = v;
				menu.AddOption( $"{v.Name}  ({v.Type})", SuiTypeRegistry.Icon( v.Type ), () =>
				{
					while ( step.Args.Count <= argIdx )
						step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.Variable } );
					step.Args[argIdx] = new SuiConverterArg
					{
						Kind = SuiConverterArgKind.Variable,
						VariableId = captured.Id,
					};
					RebuildChainUI();
					UpdateExpectsLabel();
				} );
			}
		}
		menu.OpenAtCursor( true );
	}

	/// <summary>
	/// After inserting or removing a step, fix the implicit chain-fed first arg
	/// of every step — step 0 reads from "Source", later steps from "Previous".
	/// </summary>
	private void FixChainRefsAfterMutation()
	{
		for ( int i = 0; i < _converterSteps.Count; i++ )
		{
			var step = _converterSteps[i];
			if ( step?.Args == null || step.Args.Count == 0 ) continue;
			var first = step.Args[0];
			if ( first?.Kind == SuiConverterArgKind.ChainRef )
				first.ChainRef = i == 0 ? "Source" : "Previous";
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Element picker (Button + Menu)
	// ─────────────────────────────────────────────────────────────────────

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

	// ─────────────────────────────────────────────────────────────────────
	//  Property picker (Button + Menu)
	// ─────────────────────────────────────────────────────────────────────

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
		menu.SetStyles( "color: #e5e7eb;" );

		var typesByIndex = new List<string>();

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

	private static void TintMenuItems( Menu menu, List<string> typesByIndex )
	{
		if ( menu == null || typesByIndex == null ) return;
		int idx = 0;
		foreach ( var child in menu.Children )
		{
			if ( idx >= typesByIndex.Count ) break;
			var t = typesByIndex[idx++];
			if ( string.IsNullOrEmpty( t ) ) continue;
			try { child.SetStyles( $"color: {SuiTypeRegistry.Color( t )};" ); }
			catch { /* best-effort */ }
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Accept + validation
	// ─────────────────────────────────────────────────────────────────────

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

		// TwoWay rejects converter chains (PRD 18 § 4.6 — round-trip semantics
		// for chained transforms aren't defined in V1.5).
		if ( _mode == SuiBindingMode.TwoWay && _converterSteps != null && _converterSteps.Count > 0 )
		{
			ShowError( "TwoWay bindings cannot have converters." );
			return;
		}

		// Validate each converter step has its required args filled.
		if ( _converterSteps != null )
		{
			for ( int i = 0; i < _converterSteps.Count; i++ )
			{
				var step = _converterSteps[i];
				var meta = SuiConverterCatalog.Find( step?.ConverterRef );
				if ( meta == null )
				{
					ShowError( $"Converter step #{i + 1} has no converter selected." );
					return;
				}
				if ( meta.Inputs == null ) continue;
				for ( int a = 1; a < meta.Inputs.Length; a++ )
				{
					var arg = step.Args != null && a < step.Args.Count ? step.Args[a] : null;
					if ( arg == null
						|| arg.Kind != SuiConverterArgKind.Variable
						|| string.IsNullOrEmpty( arg.VariableId ) )
					{
						ShowError( $"Converter #{i + 1} ({meta.DisplayName}) — pick a value for '{meta.Inputs[a].Name}'." );
						return;
					}
				}
			}
		}

		var binding = new SuiBinding
		{
			Id = _editingBindingId ?? SuiBinding.NewBindingId(),
			Property = _property,
			Mode = _mode,
			Source = new SuiBindingSource { VariableId = _sourceVariableId },
		};
		if ( _converterSteps != null )
		{
			foreach ( var s in _converterSteps )
				binding.Converters.Add( s.Clone() );
		}

		OnAccept?.Invoke( _element.Id, binding );
		Close();
	}

	private void ShowError( string msg )
	{
		_error.Text = msg;
		_error.Visible = true;
	}

	private static string FormatPropertyButtonText( SuiElement el, string property )
		=> string.IsNullOrEmpty( property ) ? "(pick…)" : property;

	// ─────────────────────────────────────────────────────────────────────
	//  Expects label (target ↔ effective chain output compatibility)
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// The TypeRef that actually arrives at the target property after the chain
	/// runs. With no chain it's the source variable's type; otherwise it's the
	/// last step's catalog return type (falling back to the source on lookup miss).
	/// </summary>
	private string EffectiveOutputType( string sourceType )
	{
		if ( _converterSteps == null || _converterSteps.Count == 0 ) return sourceType;
		var last = _converterSteps[_converterSteps.Count - 1];
		var meta = SuiConverterCatalog.Find( last?.ConverterRef );
		if ( meta == null || string.IsNullOrEmpty( meta.ReturnType ) ) return sourceType;
		return meta.ReturnType;
	}

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

		string sourceType = null;
		if ( !string.IsNullOrEmpty( _sourceVariableId ) && _doc?.Variables != null )
		{
			foreach ( var v in _doc.Variables )
			{
				if ( v?.Id == _sourceVariableId ) { sourceType = v.Type; break; }
			}
		}

		var effective = EffectiveOutputType( sourceType );
		var chainNote = _converterSteps != null && _converterSteps.Count > 0
			? $" (via {_converterSteps.Count} converter{(_converterSteps.Count == 1 ? "" : "s")})"
			: "";

		string text;
		string color;

		if ( string.IsNullOrEmpty( sourceType ) )
		{
			text  = $"Expects: {targetType}";
			color = SuiTypeRegistry.Color( targetType );
		}
		else if ( SameType( effective, targetType ) )
		{
			text  = $"✓ Direct bind  —  {effective}{chainNote}";
			color = "#34d399";
		}
		else if ( SuiConverterSuggester.TypesCompatible( effective, targetType ) )
		{
			text  = $"✓ Auto-converts  —  {effective} → {targetType}{chainNote}";
			color = "#34d399";
		}
		else
		{
			var suggested = SuiConverterSuggester.Suggest( effective, targetType );
			if ( !string.IsNullOrEmpty( suggested ) )
			{
				var name = suggested.StartsWith( "builtin." )
					? suggested.Substring( "builtin.".Length )
					: suggested;
				text  = $"Needs converter:  {name}()  —  {effective} → {targetType}";
				color = "#fbbf24";
			}
			else
			{
				text  = $"⚠ Type mismatch  —  {effective} → {targetType}";
				color = "#ef4444";
			}
		}

		_expectsLabel.Text = text;
		_expectsLabel.SetStyles(
			$"color: {color}; font-size: 10px; font-weight: 600; padding-left: 98px;" );
	}

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

	private void UpdatePropertyButtonStyle()
	{
		if ( _propertyBtn == null ) return;

		string color = "#9ca3af";
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

	// ─────────────────────────────────────────────────────────────────────
	//  Row helpers
	// ─────────────────────────────────────────────────────────────────────

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
