using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Modal that walks the user through declaring a new <c>[SuiConverter]</c>
/// method (PRD 18 § 5.6). On accept the dialog produces a
/// <see cref="Config"/> record; the caller hands it to
/// <see cref="SuiUserConverterScaffolder"/> which writes the stub into the
/// user-converters file.
///
/// M1-C2-c scope: Code path only. The ActionGraph backed-converter route
/// (PRD 18 § 5.5) lands with the broader ActionGraph integration in M3.
/// </summary>
public sealed class SuiCustomConverterDialog : Window
{
	/// <summary>Result of a successful dialog — the spec the scaffolder writes to disk.</summary>
	public sealed record Config(
		string Name,
		string Category,
		string Description,
		List<(string name, string type)> Inputs,
		string ReturnType );

	public Action<Config> OnAccept;

	private sealed class InputDef
	{
		public string Name = "input";
		public string Type = "float";
	}

	private static readonly string[] TypeChoices =
	{
		"string", "int", "long", "float", "double", "bool",
		"Color", "Vector2", "Vector3", "Vector4",
		"Angles", "Rotation", "Transform",
		"Texture", "Resource", "Sound", "Material",
	};

	private LineEdit _nameEdit;
	private LineEdit _categoryEdit;
	private LineEdit _descEdit;
	private Widget _inputsHost;
	private List<InputDef> _inputs = new();
	private string _returnType = "float";
	private Label _error;

	public SuiCustomConverterDialog()
	{
		Title = "Create Custom Converter";
		WindowTitle = Title;
		Size = new Vector2( 520, 480 );
		MinimumSize = new Vector2( 520, 480 );
		SetWindowIcon( "add" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 14;
		Canvas.Layout.Spacing = 8;

		// Sensible defaults — most user converters are HP → Color tints or
		// (a, b) → result style transforms.
		_inputs.Add( new InputDef { Name = "a", Type = "float" } );
		_inputs.Add( new InputDef { Name = "b", Type = "float" } );

		BuildLayout();
		Show();
	}

	private void BuildLayout()
	{
		_nameEdit     = AddTextRow( "Name",        "HealthbarTint", "MyConverter" );
		_categoryEdit = AddTextRow( "Category",    "Game",          "where to group it" );
		_descEdit     = AddTextRow( "Description", "",              "optional" );

		// Return type
		var retRow = new Widget( Canvas );
		retRow.Layout = Layout.Row();
		retRow.Layout.Spacing = 8;
		var retLbl = new Label( "Returns", retRow ) { FixedWidth = 96 };
		retLbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		retRow.Layout.Add( retLbl );
		var retCombo = new ComboBox( retRow );
		retCombo.FixedHeight = 26;
		retCombo.SetStyles( FieldChrome );
		foreach ( var t in TypeChoices )
		{
			var captured = t;
			retCombo.AddItem( t, SuiTypeRegistry.Icon( t ),
				() => _returnType = captured, null, t == _returnType );
		}
		retRow.Layout.Add( retCombo, 1 );
		Canvas.Layout.Add( retRow );

		// Inputs header
		var inputsHeader = new Widget( Canvas );
		inputsHeader.Layout = Layout.Row();
		inputsHeader.Layout.Spacing = 8;
		var inputsLbl = new Label( "Inputs", inputsHeader ) { FixedWidth = 96 };
		inputsLbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		inputsHeader.Layout.Add( inputsLbl );
		var addInput = new Button( "+ Add Input", "add", inputsHeader );
		addInput.FixedHeight = 22;
		addInput.SetStyles(
			"background-color: rgb(24,24,23);" +
			"border: 1px solid rgba(255,255,255,0.1);" +
			"border-radius: 3px; padding: 0 8px;" +
			"color: #e5e7eb; font-size: 10px;" );
		addInput.Clicked = () => { _inputs.Add( new InputDef() ); RebuildInputs(); };
		inputsHeader.Layout.Add( addInput );
		inputsHeader.Layout.AddStretchCell();
		Canvas.Layout.Add( inputsHeader );

		_inputsHost = new Widget( Canvas );
		_inputsHost.Layout = Layout.Column();
		_inputsHost.Layout.Spacing = 4;
		Canvas.Layout.Add( _inputsHost );

		RebuildInputs();

		_error = new Label( "", Canvas );
		_error.SetStyles( "color: #ef4444; font-size: 11px;" );
		_error.Visible = false;
		Canvas.Layout.Add( _error );

		Canvas.Layout.AddStretchCell();

		// Buttons
		var buttons = new Widget( Canvas );
		buttons.Layout = Layout.Row();
		buttons.Layout.Spacing = 8;
		buttons.Layout.AddStretchCell();
		var cancel = new Button( "Cancel", buttons );
		cancel.Clicked = Close;
		buttons.Layout.Add( cancel );
		var ok = new Button( "Create", buttons );
		ok.Clicked = Accept;
		buttons.Layout.Add( ok );
		Canvas.Layout.Add( buttons );
	}

	private void RebuildInputs()
	{
		if ( _inputsHost?.Layout == null ) return;
		_inputsHost.Layout.Clear( true );
		for ( int i = 0; i < _inputs.Count; i++ )
			_inputsHost.Layout.Add( BuildInputRow( i, _inputs[i] ) );
	}

	private Widget BuildInputRow( int idx, InputDef def )
	{
		var row = new Widget( _inputsHost );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 6;
		row.Layout.Margin = new Sandbox.UI.Margin( 96, 0, 0, 0 );
		row.FixedHeight = 28;

		var nameEdit = new LineEdit( row ) { Text = def.Name };
		nameEdit.PlaceholderText = "name";
		nameEdit.FixedHeight = 24;
		nameEdit.SetStyles( FieldChrome );
		nameEdit.TextEdited += t => def.Name = t ?? "";
		row.Layout.Add( nameEdit, 1 );

		var typeCombo = new ComboBox( row );
		typeCombo.FixedHeight = 24;
		typeCombo.SetStyles( FieldChrome );
		foreach ( var t in TypeChoices )
		{
			var captured = t;
			typeCombo.AddItem( t, SuiTypeRegistry.Icon( t ),
				() => def.Type = captured, null, t == def.Type );
		}
		row.Layout.Add( typeCombo, 1 );

		var rmBtn = new Button( "", "close", row );
		rmBtn.FixedWidth = 22;
		rmBtn.FixedHeight = 22;
		rmBtn.SetStyles( "background-color: transparent; border: none; color: #6b7280;" );
		rmBtn.Clicked = () =>
		{
			if ( idx >= 0 && idx < _inputs.Count ) _inputs.RemoveAt( idx );
			RebuildInputs();
		};
		row.Layout.Add( rmBtn );

		return row;
	}

	private void Accept()
	{
		var name = _nameEdit.Text?.Trim() ?? "";
		if ( !SuiDocumentValidator.IsValidCSharpIdentifier( name ) )
		{
			ShowError( "Name must be a valid C# identifier (letter/underscore, then letters/digits/underscores)." );
			return;
		}

		var category = string.IsNullOrWhiteSpace( _categoryEdit.Text )
			? "User"
			: _categoryEdit.Text.Trim();
		var description = _descEdit.Text?.Trim() ?? "";

		// Validate every input name + check for duplicates.
		var seen = new HashSet<string>();
		var collected = new List<(string name, string type)>();
		foreach ( var i in _inputs )
		{
			var n = (i.Name ?? "").Trim();
			if ( !SuiDocumentValidator.IsValidCSharpIdentifier( n ) )
			{
				ShowError( $"Input name '{n}' is not a valid C# identifier." );
				return;
			}
			if ( !seen.Add( n ) )
			{
				ShowError( $"Duplicate input name: '{n}'." );
				return;
			}
			collected.Add( (n, i.Type ?? "float") );
		}

		OnAccept?.Invoke( new Config( name, category, description, collected, _returnType ) );
		Close();
	}

	private void ShowError( string msg )
	{
		_error.Text = msg;
		_error.Visible = true;
	}

	private const string FieldChrome =
		"background-color: rgb(20,20,19);" +
		"border: 1px solid rgba(255,255,255,0.12);" +
		"border-radius: 3px;" +
		"padding: 0 6px;" +
		"color: #e5e7eb;";

	private LineEdit AddTextRow( string label, string value, string placeholder )
	{
		var row = new Widget( Canvas );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 8;
		var lbl = new Label( label, row ) { FixedWidth = 96 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );

		var edit = new LineEdit( row ) { Text = value ?? "" };
		edit.FixedHeight = 26;
		if ( !string.IsNullOrEmpty( placeholder ) ) edit.PlaceholderText = placeholder;
		edit.SetStyles( FieldChrome );
		row.Layout.Add( edit, 1 );

		Canvas.Layout.Add( row );
		return edit;
	}
}
