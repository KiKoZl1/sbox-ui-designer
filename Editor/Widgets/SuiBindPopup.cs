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
	private ComboBox _modeCombo;
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
		_modeCombo = AddComboRow( "Mode" );
		foreach ( var m in new[] { SuiBindingMode.OneWay, SuiBindingMode.OneTime, SuiBindingMode.TwoWay } )
		{
			var captured = m;
			_modeCombo.AddItem( m.ToString(), "swap_horiz", () => _mode = captured, null, m == _mode );
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

		// Args for every input — arg 0 is the chain feed by convention, but the
		// user can repoint it to a Variable or Literal (and feed the chain into
		// a different arg index instead) via the per-arg picker.
		//
		// Variadic support: when meta.Inputs ends with an `IsParams` slot
		// (e.g. Format's `params object[] args`), the user can append N extra
		// arguments via a `+ Add Arg` button. Each extra slot has its own `×`
		// remove button. The variadic element type is whatever the params
		// array's element is (string, object, etc.).
		var lastIdx = (meta?.Inputs?.Length ?? 0) - 1;
		var isVariadic = lastIdx >= 0 && meta.Inputs[lastIdx].IsParams;
		var variadicElemType = isVariadic
			? StripArraySuffix( meta.Inputs[lastIdx].Type )
			: null;
		var fixedArgCount = isVariadic ? lastIdx : (meta?.Inputs?.Length ?? 0);

		if ( meta?.Inputs != null && meta.Inputs.Length > 0 )
		{
			// Fixed args (everything before the variadic slot, or all args if no variadic).
			for ( int argIdx = 0; argIdx < fixedArgCount; argIdx++ )
			{
				var capturedArgIdx = argIdx;
				var input = meta.Inputs[argIdx];

				while ( step.Args.Count <= argIdx )
				{
					var defaultKind = step.Args.Count == 0
						? SuiConverterArgKind.ChainRef
						: SuiConverterArgKind.Variable;
					step.Args.Add( new SuiConverterArg
					{
						Kind = defaultKind,
						ChainRef = defaultKind == SuiConverterArgKind.ChainRef ? "Source" : null,
					} );
				}

				var argBtn = new Button( ArgButtonText( step.Args[argIdx], input.Name ), ArgButtonIcon( step.Args[argIdx] ), row );
				argBtn.FixedHeight = 22;
				argBtn.SetStyles(
					"background-color: rgb(24,24,23);" +
					"border: 1px solid rgba(255,255,255,0.12);" +
					"border-radius: 3px; padding: 0 6px;" +
					"color: #e5e7eb; font-size: 10px;" );
				argBtn.Clicked = () => OpenArgPickerMenu( argBtn, step, capturedArgIdx, input?.Type );
				row.Layout.Add( argBtn );
			}

			// Variadic args (only when meta has IsParams on last slot). Each
			// rendered as `[value] [×]` pair; ensures `+ Add Arg` is the
			// rightmost button before the step controls.
			if ( isVariadic )
			{
				for ( int argIdx = fixedArgCount; argIdx < step.Args.Count; argIdx++ )
				{
					var capturedArgIdx = argIdx;
					var argBtn = new Button( ArgButtonText( step.Args[argIdx], $"#{argIdx - fixedArgCount + 1}" ), ArgButtonIcon( step.Args[argIdx] ), row );
					argBtn.FixedHeight = 22;
					argBtn.SetStyles(
						"background-color: rgb(24,24,23);" +
						"border: 1px solid rgba(255,255,255,0.12);" +
						"border-radius: 3px; padding: 0 6px;" +
						"color: #e5e7eb; font-size: 10px;" );
					argBtn.Clicked = () => OpenArgPickerMenu( argBtn, step, capturedArgIdx, variadicElemType );
					row.Layout.Add( argBtn );

					var removeArgBtn = new Button( "", "close", row );
					removeArgBtn.FixedWidth = 16;
					removeArgBtn.FixedHeight = 22;
					removeArgBtn.SetStyles( "background-color: transparent; border: none; color: #6b7280; font-size: 10px;" );
					removeArgBtn.Clicked = () =>
					{
						step.Args.RemoveAt( capturedArgIdx );
						RebuildChainUI();
						UpdateExpectsLabel();
					};
					row.Layout.Add( removeArgBtn );
				}

				var addArgBtn = new Button( "+ Add Arg", "add", row );
				addArgBtn.FixedHeight = 22;
				addArgBtn.SetStyles(
					"background-color: rgba(59,130,246,0.15);" +
					"border: 1px solid rgba(59,130,246,0.4);" +
					"border-radius: 3px; padding: 0 8px;" +
					"color: #93c5fd; font-size: 10px; font-weight: 600;" );
				addArgBtn.Clicked = () =>
				{
					step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.Variable } );
					RebuildChainUI();
					UpdateExpectsLabel();
				};
				row.Layout.Add( addArgBtn );
			}
		}

		row.Layout.AddStretchCell();

		// Issue #12 — step reordering. Up/Down swap with the neighbouring step.
		// Disabled buttons (faded) at the ends communicate "no neighbour" without
		// hiding the control set jumping around as the user rearranges.
		// Enabled state uses bright white + subtle bg fill; disabled gets a
		// dim opacity 35 % so the "can't click" state is unmistakable against
		// the dark popup background. Previous low-contrast `#374151` blended
		// with the popup row and looked identical to the enabled state.
		var upBtn = new Button( "", "keyboard_arrow_up", row );
		upBtn.FixedWidth = 22;
		upBtn.FixedHeight = 22;
		upBtn.Enabled = stepIndex > 0;
		upBtn.SetStyles( upBtn.Enabled
			? "background-color: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.18); border-radius: 3px; color: #e5e7eb;"
			: "background-color: transparent; border: 1px dashed rgba(255,255,255,0.08); border-radius: 3px; color: rgba(229,231,235,0.25);" );
		upBtn.Clicked = () => SwapSteps( stepIndex, stepIndex - 1 );
		row.Layout.Add( upBtn );

		var downBtn = new Button( "", "keyboard_arrow_down", row );
		downBtn.FixedWidth = 22;
		downBtn.FixedHeight = 22;
		downBtn.Enabled = stepIndex < _converterSteps.Count - 1;
		downBtn.SetStyles( downBtn.Enabled
			? "background-color: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.18); border-radius: 3px; color: #e5e7eb;"
			: "background-color: transparent; border: 1px dashed rgba(255,255,255,0.08); border-radius: 3px; color: rgba(229,231,235,0.25);" );
		downBtn.Clicked = () => SwapSteps( stepIndex, stepIndex + 1 );
		row.Layout.Add( downBtn );

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

	private void SwapSteps( int a, int b )
	{
		if ( _converterSteps == null ) return;
		if ( a < 0 || b < 0 || a >= _converterSteps.Count || b >= _converterSteps.Count ) return;
		if ( a == b ) return;
		(_converterSteps[a], _converterSteps[b]) = (_converterSteps[b], _converterSteps[a]);
		FixChainRefsAfterMutation();
		RebuildChainUI();
		UpdateExpectsLabel();
	}

	private string ArgButtonText( SuiConverterArg arg, string paramName )
	{
		if ( arg == null ) return $"{paramName}: (pick…)";
		switch ( arg.Kind )
		{
			case SuiConverterArgKind.ChainRef:
				return $"{paramName}: ⛓ {(string.IsNullOrEmpty( arg.ChainRef ) ? "Chain" : arg.ChainRef)}";
			case SuiConverterArgKind.Variable:
				if ( string.IsNullOrEmpty( arg.VariableId ) ) return $"{paramName}: (pick…)";
				foreach ( var v in _doc?.Variables ?? new List<SuiVariable>() )
					if ( v?.Id == arg.VariableId ) return $"{paramName}: {v.Name}";
				return $"{paramName}: (missing)";
			case SuiConverterArgKind.Literal:
				return $"{paramName}: ={LiteralPreview( arg.Literal )}";
			default:
				return $"{paramName}: (pick…)";
		}
	}

	private static string ArgButtonIcon( SuiConverterArg arg )
	{
		return arg?.Kind switch
		{
			SuiConverterArgKind.ChainRef => "link",
			SuiConverterArgKind.Literal  => "edit",
			_                            => "data_object",
		};
	}

	/// <summary>
	/// Strip the trailing <c>[]</c> from an array type name, returning the
	/// element type — e.g. <c>"Object[]"</c> → <c>"Object"</c>. Used by the
	/// variadic arg renderer to pick the right typed input dialog per extra
	/// argument (each element of <c>params object[] args</c> is a single object).
	/// </summary>
	private static string StripArraySuffix( string t )
	{
		if ( string.IsNullOrEmpty( t ) ) return t;
		if ( t.EndsWith( "[]" ) ) return t.Substring( 0, t.Length - 2 );
		return t;
	}

	/// <summary>Compact preview of a literal value for the arg button label.</summary>
	private static string LiteralPreview( System.Text.Json.Nodes.JsonNode node )
	{
		if ( node == null ) return "null";
		var s = node.ToJsonString();
		if ( string.IsNullOrEmpty( s ) ) return "\"\"";
		if ( s.Length > 12 ) s = s.Substring( 0, 11 ) + "…";
		return s;
	}

	private void OpenAddConverterMenu()
	{
		var menu = new Menu( _addConverterBtn );

		// Group every available converter (built-ins + user [SuiConverter]
		// discoveries) by category — easier to scan a 51+-entry catalog.
		var byCategory = new Dictionary<string, List<SuiConverterMetadata>>();
		foreach ( var c in SuiConverterCatalog.GetAll() )
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

		menu.AddSeparator();
		menu.AddOption( "+ Custom Converter…", "add_circle", OpenCustomConverterDialog );

		menu.OpenAtCursor( true );
	}

	/// <summary>
	/// Open the custom-converter dialog and scaffold the result into the user
	/// converters file (PRD 18 § 5.6). The new method appears in the chain
	/// menu after the next compile — discovery via TypeLibrary lands with the
	/// codegen integration in M1-D.
	/// </summary>
	private void OpenCustomConverterDialog()
	{
		var dlg = new SuiCustomConverterDialog();
		dlg.OnAccept = config =>
		{
			var path = SuiUserConverterScaffolder.Scaffold( config );
			if ( !string.IsNullOrEmpty( path ) )
			{
				Log.Info( $"[SUI] Created converter '{config.Name}' → {path}" );
				Log.Info( "[SUI] Fill in the stub, save, and the converter will appear in the chain menu after the next compile." );
			}
			else
			{
				Log.Warning( "[SUI] Failed to scaffold the custom converter — see warnings above." );
			}
		};
	}

	private void AddStep( SuiConverterMetadata meta )
	{
		if ( meta == null ) return;

		// Issue #11 — TwoWay bindings can't carry a chain (round-trip semantics
		// for chained transforms are undefined in V1.5, PRD 18 § 4.6). Rather
		// than silently rejecting on Accept, prompt the user to switch to OneWay.
		if ( _mode == SuiBindingMode.TwoWay )
		{
			SuiConfirmDialog.Show(
				"Two-way bindings can't use converters",
				"Two-way bindings cannot have a converter chain (round-trip semantics for chained transforms aren't defined). Switch this binding to OneWay so the converter can be added?",
				okText: "Switch to OneWay",
				onConfirm: () =>
				{
					_mode = SuiBindingMode.OneWay;
					try { _modeCombo?.TrySelectNamed( "OneWay" ); } catch { /* best-effort */ }
					AddStepInternal( meta );
				} );
			return;
		}

		AddStepInternal( meta );
	}

	private void AddStepInternal( SuiConverterMetadata meta )
	{
		var step = new SuiBindingConverterStep
		{
			ConverterRef = meta.Ref,
			Args = new List<SuiConverterArg>(),
		};

		// Args[0] is fed by the chain — "Source" for the first step, "Previous"
		// for every step after.
		var firstRef = _converterSteps.Count == 0 ? "Source" : "Previous";
		step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.ChainRef, ChainRef = firstRef } );

		// Reserve a Variable slot per additional FIXED input. Variadic slots
		// (params) start empty — user appends them via the `+ Add Arg` button
		// in the chain row, otherwise the converter would always emit at least
		// one extra arg the user can't see.
		if ( meta.Inputs != null )
		{
			var lastIdx = meta.Inputs.Length - 1;
			var skipLast = lastIdx >= 0 && meta.Inputs[lastIdx].IsParams;
			var fixedEnd = skipLast ? lastIdx : meta.Inputs.Length;
			for ( int i = 1; i < fixedEnd; i++ )
				step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.Variable } );
		}

		_converterSteps.Add( step );
		RebuildChainUI();
		UpdateExpectsLabel();
	}

	private void OpenArgPickerMenu( Button anchor, SuiBindingConverterStep step, int argIdx, string paramType = null )
	{
		var menu = new Menu( anchor );

		// ── 1) Chain refs — Source / Previous ──
		// Allowing the user to put the chain feed in any arg slot fixes the
		// long-standing "Concat reorder" friction (arg 0 is no longer the only
		// position that can receive the chain output).
		var fromChainSub = menu.AddMenu( "From Chain", "link" );
		fromChainSub.AddOption( "⛓ Source  (binding's variable)", "link", () => SetArgToChain( step, argIdx, "Source" ) );
		fromChainSub.AddOption( "⛓ Previous  (last step's output)", "link", () => SetArgToChain( step, argIdx, "Previous" ) );

		// ── 2) Variables — tinted by compatibility with paramType (#15) ──
		var vars = _doc?.Variables ?? new List<SuiVariable>();
		var varCompat = new List<string>();
		if ( vars.Count == 0 )
		{
			menu.AddOption( "(no variables in this document)", null, null );
			varCompat.Add( null );
		}
		else
		{
			foreach ( var v in vars )
			{
				if ( v == null ) continue;
				var captured = v;
				menu.AddOption( $"{v.Name}  ({v.Type})", SuiTypeRegistry.Icon( v.Type ),
					() => SetArgToVariable( step, argIdx, captured.Id ) );
				varCompat.Add( CompatRating( v.Type, paramType ) );
			}
		}
		menu.AddSeparator();

		// ── 3) Literal — typed input dialog per paramType ──
		menu.AddOption( "Literal…", "edit",
			() => OpenLiteralInputDialog( step, argIdx, paramType ) );

		// Best-effort tint variables by compatibility with paramType (#15).
		TintVariableMenuItems( menu, varCompat );

		menu.OpenAtCursor( true );
	}

	private void SetArgToChain( SuiBindingConverterStep step, int argIdx, string chainRef )
	{
		while ( step.Args.Count <= argIdx )
			step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.Variable } );
		step.Args[argIdx] = new SuiConverterArg
		{
			Kind = SuiConverterArgKind.ChainRef,
			ChainRef = chainRef,
		};
		// Don't re-run FixChainRefsAfterMutation here — that helper assumes
		// arg 0 is the chain feed. Now that arg position is editable, we
		// respect whatever the user picked.
		RebuildChainUI();
		UpdateExpectsLabel();
	}

	private void SetArgToVariable( SuiBindingConverterStep step, int argIdx, string variableId )
	{
		while ( step.Args.Count <= argIdx )
			step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.Variable } );
		step.Args[argIdx] = new SuiConverterArg
		{
			Kind = SuiConverterArgKind.Variable,
			VariableId = variableId,
		};
		RebuildChainUI();
		UpdateExpectsLabel();
	}

	private void OpenLiteralInputDialog( SuiBindingConverterStep step, int argIdx, string paramType )
	{
		var existing = ( step?.Args != null && argIdx < step.Args.Count ) ? step.Args[argIdx] : null;
		var existingLiteral = existing?.Kind == SuiConverterArgKind.Literal ? existing.Literal : null;

		var dlg = new SuiLiteralInputDialog( paramType, existingLiteral );
		dlg.OnAccept = json =>
		{
			while ( step.Args.Count <= argIdx )
				step.Args.Add( new SuiConverterArg { Kind = SuiConverterArgKind.Variable } );
			step.Args[argIdx] = new SuiConverterArg
			{
				Kind = SuiConverterArgKind.Literal,
				Literal = json,
			};
			RebuildChainUI();
			UpdateExpectsLabel();
		};
	}

	/// <summary>
	/// Tint the variable rows in the arg picker by how well each variable's
	/// type matches the expected parameter type (#15). Insertion order at the
	/// top level: [fromChainSubMenu, …vars…, literalOption]. The sub-menu
	/// counts as a single child, so we skip 1 leading entry and then read up
	/// to <c>ratings.Count</c> rows.
	/// </summary>
	private static void TintVariableMenuItems( Menu menu, List<string> ratings )
	{
		if ( menu == null || ratings == null || ratings.Count == 0 ) return;
		const int leading = 1;
		int idx = 0;
		foreach ( var child in menu.Children )
		{
			if ( idx < leading ) { idx++; continue; }
			int varIdx = idx - leading;
			if ( varIdx >= ratings.Count ) break;
			var rating = ratings[varIdx];
			if ( !string.IsNullOrEmpty( rating ) )
			{
				try { child.SetStyles( $"color: {rating};" ); }
				catch { /* best-effort */ }
			}
			idx++;
		}
	}

	/// <summary>
	/// Color-code a variable's compatibility with a target parameter type. Green
	/// for exact match, yellow for safe widening (int→float etc.), red for clear
	/// mismatches, null when we can't tell.
	/// </summary>
	private static string CompatRating( string variableType, string paramType )
	{
		if ( string.IsNullOrEmpty( paramType ) || string.IsNullOrEmpty( variableType ) ) return null;
		if ( paramType == "T" || paramType == "object" || paramType == "object[]" ) return "#9ca3af";
		var vn = NormalizeCsType( variableType );
		var pn = NormalizeCsType( paramType );
		if ( vn == pn ) return "#34d399"; // green
		if ( IsSafeWidening( vn, pn ) ) return "#fbbf24"; // yellow
		return "#ef4444"; // red
	}

	private static string NormalizeCsType( string t ) => t switch
	{
		"Single"  => "float",
		"Double"  => "double",
		"Int32"   => "int",
		"Int64"   => "long",
		"Boolean" => "bool",
		"String"  => "string",
		_         => t,
	};

	private static bool IsSafeWidening( string from, string to )
		=> (from == "int"   && (to == "long" || to == "float" || to == "double"))
		|| (from == "long"  && (to == "float" || to == "double"))
		|| (from == "float" && to == "double");

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
