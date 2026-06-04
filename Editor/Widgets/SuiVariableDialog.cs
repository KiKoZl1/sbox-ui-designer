using System;
using System.Text.Json.Nodes;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Modal dialog for creating or editing a <see cref="SuiVariable"/> (PRD 18 § 3.7).
/// One dialog serves both flows — pass an existing Variable to edit it, or null to
/// add a new one. On accept it invokes <see cref="OnAccept"/> with a detached
/// <see cref="SuiVariable"/> carrying the entered values (the caller wraps it in
/// the appropriate command).
///
/// V1.5 — Manual source only. The FromComponent / FromActionGraph kinds
/// were ripped at M4 close (DEVIATION D-017). Enum / Component / List types
/// are still M-something polish items.
/// </summary>
public sealed class SuiVariableDialog : Window
{
	/// <summary>Invoked on Create/Save with a detached Variable holding the entered values.</summary>
	public Action<SuiVariable> OnAccept;

	/// <summary>The closed type set offered in the dropdown (PRD 18 § 3.3 — M1-B subset).</summary>
	private static readonly string[] Types =
	{
		"string", "int", "long", "float", "double", "bool",
		"Color", "Vector2", "Vector3", "Vector4", "Angles", "Rotation", "Transform",
		"Texture", "Resource", "Sound", "Material",
	};

	private readonly bool _isEdit;
	private readonly string _editingId; // preserved across edit so Id stays stable

	private LineEdit _nameEdit;
	private ComboBox _typeCombo;
	private LineEdit _groupEdit;
	private LineEdit _defaultEdit;
	private LineEdit _descEdit;
	private SuiToggleField _isPublicToggle;
	private Label _error;

	private string _type;
	private bool _existingIsPublic;

	public SuiVariableDialog( SuiVariable existing = null )
	{
		_isEdit = existing != null;
		_editingId = existing?.Id;
		_type = existing?.Type ?? "string";
		_existingIsPublic = existing?.IsPublic ?? false;

		Title = _isEdit ? "Edit Variable" : "New Variable";
		WindowTitle = Title;
		Size = new Vector2( 420, 360 );
		MinimumSize = new Vector2( 420, 360 );
		SetWindowIcon( _isEdit ? "edit" : "add" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 14;
		Canvas.Layout.Spacing = 8;

		BuildLayout( existing );
		Show();
	}

	private void BuildLayout( SuiVariable existing )
	{
		_nameEdit = AddTextRow( "Name", existing?.Name ?? "", "MyVariable" );

		// Type dropdown.
		var typeRow = new Widget( Canvas );
		typeRow.Layout = Layout.Row();
		typeRow.Layout.Spacing = 8;
		var typeLbl = new Label( "Type", typeRow ) { FixedWidth = 96 };
		typeLbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		typeRow.Layout.Add( typeLbl );
		_typeCombo = new ComboBox( typeRow );
		_typeCombo.FixedHeight = 26;
		_typeCombo.SetStyles(
			"background-color: rgb(20,20,19);" +
			"border: 1px solid rgba(255,255,255,0.12);" +
			"border-radius: 3px;" +
			"padding: 0 6px;" +
			"color: #e5e7eb;" );
		foreach ( var t in Types )
			_typeCombo.AddItem( t, null, () => _type = t, null, t == _type );
		typeRow.Layout.Add( _typeCombo, 1 );
		Canvas.Layout.Add( typeRow );

		_groupEdit = AddTextRow( "Group", existing?.Group ?? "", "optional category" );
		_defaultEdit = AddTextRow( "Default", existing?.Default?.ToJsonString() ?? "", "e.g. 100" );
		_descEdit = AddTextRow( "Description", existing?.Description ?? "", "optional" );

		// IsPublic toggle (V1.5-M2-K) — flip on to expose this Variable as a
		// parent-settable parameter when the .sui is embedded via SuiReference,
		// AND make it reachable from gameplay code as Parent.ChildName.VarName.
		var pubRow = new Widget( Canvas );
		pubRow.Layout = Layout.Row();
		pubRow.Layout.Spacing = 8;
		var pubLbl = new Label( "Is Public", pubRow ) { FixedWidth = 96 };
		pubLbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		pubRow.Layout.Add( pubLbl );
		_isPublicToggle = new SuiToggleField( _existingIsPublic, pubRow );
		_isPublicToggle.FixedWidth = 80;
		pubRow.Layout.Add( _isPublicToggle );
		pubRow.Layout.AddStretchCell();
		Canvas.Layout.Add( pubRow );

		var pubHint = new Label(
			"Off (default): internal — only code referencing this instance reads it.\n" +
			"On: exposed — parents that embed this .sui can set the value, and any caller can read via Parent.ChildName." + (existing?.Name ?? "VarName") + ".",
			Canvas );
		pubHint.WordWrap = true;
		pubHint.SetStyles( "color: #6b7280; font-size: 10px; padding: 0 0 4px 96px;" );
		Canvas.Layout.Add( pubHint );

		_error = new Label( "", Canvas );
		_error.SetStyles( "color: #ef4444; font-size: 11px;" );
		_error.Visible = false;
		Canvas.Layout.Add( _error );

		Canvas.Layout.AddStretchCell();

		// Buttons.
		var buttons = new Widget( Canvas );
		buttons.Layout = Layout.Row();
		buttons.Layout.Spacing = 8;
		buttons.Layout.AddStretchCell();

		var cancel = new Button( "Cancel", buttons );
		cancel.Clicked = Close;
		buttons.Layout.Add( cancel );

		var accept = new Button( _isEdit ? "Save" : "Create", buttons );
		accept.Clicked = Accept;
		buttons.Layout.Add( accept );

		Canvas.Layout.Add( buttons );
	}

	private LineEdit AddTextRow( string label, string value, string placeholder = null )
	{
		var row = new Widget( Canvas );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 8;
		var lbl = new Label( label, row ) { FixedWidth = 96 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );

		var edit = new LineEdit( row ) { Text = value ?? "" };
		edit.FixedHeight = 26;
		if ( !string.IsNullOrEmpty( placeholder ) )
			edit.PlaceholderText = placeholder;
		// Visible field chrome — without this the LineEdit renders with no
		// background or border and looks like empty space the user can't tell
		// is editable.
		edit.SetStyles(
			"background-color: rgb(20,20,19);" +
			"border: 1px solid rgba(255,255,255,0.12);" +
			"border-radius: 3px;" +
			"padding: 0 6px;" +
			"color: #e5e7eb;" );
		row.Layout.Add( edit, 1 );

		Canvas.Layout.Add( row );
		return edit;
	}

	private void Accept()
	{
		var name = _nameEdit.Text?.Trim() ?? "";

		if ( !SuiDocumentValidator.IsValidCSharpIdentifier( name ) )
		{
			ShowError( "Name must be a valid C# identifier (letter/underscore, then letters/digits/underscores)." );
			return;
		}

		var v = new SuiVariable
		{
			Id = _editingId ?? SuiVariable.NewVariableId(),
			Name = name,
			Type = _type,
			Group = string.IsNullOrWhiteSpace( _groupEdit.Text ) ? null : _groupEdit.Text.Trim(),
			Description = string.IsNullOrWhiteSpace( _descEdit.Text ) ? null : _descEdit.Text.Trim(),
			Default = ParseDefault( _type, _defaultEdit.Text ),
			IsPublic = _isPublicToggle?.Checked ?? false,
			Source = new SuiVariableSource { Kind = SuiVariableSourceKind.Manual },
		};

		OnAccept?.Invoke( v );
		Close();
	}

	private void ShowError( string msg )
	{
		_error.Text = msg;
		_error.Visible = true;
	}

	/// <summary>
	/// Parse the Default text field into a typed JSON node. Tolerant — an
	/// unparseable numeric/bool falls back to the type's zero value rather than
	/// blocking the dialog (the validator surfaces real mismatches later).
	/// </summary>
	private static JsonNode ParseDefault( string type, string text )
	{
		text = text?.Trim() ?? "";

		switch ( type )
		{
			case "int":
			case "long":
				return long.TryParse( text, out var l ) ? JsonValue.Create( l ) : JsonValue.Create( 0L );

			case "float":
			case "double":
				return double.TryParse( text, out var d ) ? JsonValue.Create( d ) : JsonValue.Create( 0d );

			case "bool":
				return JsonValue.Create( bool.TryParse( text, out var b ) && b );

			case "string":
				return JsonValue.Create( text );

			default:
				// Engine types / asset refs: empty = null default; otherwise keep
				// the raw text (e.g. a hex color "#4ade80") as a JSON string.
				return string.IsNullOrEmpty( text ) ? null : JsonValue.Create( text );
		}
	}
}
