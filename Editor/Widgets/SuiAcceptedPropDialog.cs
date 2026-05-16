using System;
using System.Text.Json.Nodes;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Modal for creating/editing a <see cref="SuiAcceptedProp"/> (PRD 19 § 4.3).
/// Pass null to add, or an existing prop to edit. On accept invokes
/// <see cref="OnAccept"/> with the prop and a flag asking the caller to also
/// auto-create a matching <c>FromAcceptedProp</c> Variable so bindings inside
/// the doc can reference it uniformly.
/// </summary>
public sealed class SuiAcceptedPropDialog : Window
{
	/// <summary>Invoked on Create/Save. The bool is true when the user opted to auto-create the matching Variable.</summary>
	public Action<SuiAcceptedProp, bool> OnAccept;

	/// <summary>Same closed set as <see cref="SuiVariableDialog.Types"/> — see PRD 18 § 3.3.</summary>
	private static readonly string[] Types =
	{
		"string", "int", "long", "float", "double", "bool",
		"Color", "Vector2", "Vector3", "Vector4", "Angles", "Rotation", "Transform",
		"Texture", "Resource", "Sound", "Material",
	};

	private readonly bool _isEdit;
	private readonly string _editingPropId;

	private LineEdit _nameEdit;
	private ComboBox _typeCombo;
	private LineEdit _defaultEdit;
	private LineEdit _descEdit;
	private LineEdit _groupEdit;
	private SuiToggleField _requiredToggle;
	private SuiToggleField _autoVariableToggle;
	private Label _error;

	private string _type;

	public SuiAcceptedPropDialog( SuiAcceptedProp existing = null )
	{
		_isEdit = existing != null;
		_editingPropId = existing?.PropId;
		_type = existing?.Type ?? "string";

		Title = _isEdit ? "Edit Accepted Prop" : "New Accepted Prop";
		WindowTitle = Title;
		Size = new Vector2( 460, 420 );
		MinimumSize = new Vector2( 460, 420 );
		SetWindowIcon( _isEdit ? "edit" : "add" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 14;
		Canvas.Layout.Spacing = 8;

		BuildLayout( existing );
		Show();
	}

	private void BuildLayout( SuiAcceptedProp existing )
	{
		_nameEdit = AddTextRow( "Name", existing?.Name ?? "", "Hp" );

		// Type dropdown.
		var typeRow = new Widget( Canvas );
		typeRow.Layout = Layout.Row();
		typeRow.Layout.Spacing = 8;
		var typeLbl = new Label( "Type", typeRow ) { FixedWidth = 110 };
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

		_defaultEdit = AddTextRow( "Default", existing?.Default?.ToJsonString() ?? "", "e.g. 100" );
		_groupEdit   = AddTextRow( "Group", existing?.Group ?? "", "optional category" );
		_descEdit    = AddTextRow( "Description", existing?.Description ?? "", "shown as tooltip in parent" );

		// Required toggle.
		var reqRow = new Widget( Canvas );
		reqRow.Layout = Layout.Row();
		reqRow.Layout.Spacing = 8;
		var reqLbl = new Label( "Required", reqRow ) { FixedWidth = 110 };
		reqLbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		reqRow.Layout.Add( reqLbl );
		_requiredToggle = new SuiToggleField( existing?.Required ?? false, reqRow );
		reqRow.Layout.Add( _requiredToggle );
		reqRow.Layout.AddStretchCell();
		Canvas.Layout.Add( reqRow );

		// Auto-Variable toggle — only meaningful when creating (existing props
		// have already had their Variable created or not on first save). For
		// edit we hide it.
		if ( !_isEdit )
		{
			var avRow = new Widget( Canvas );
			avRow.Layout = Layout.Row();
			avRow.Layout.Spacing = 8;
			var avLbl = new Label( "Create Variable", avRow ) { FixedWidth = 110 };
			avLbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
			avRow.Layout.Add( avLbl );
			_autoVariableToggle = new SuiToggleField( true, avRow );
			avRow.Layout.Add( _autoVariableToggle );
			avRow.Layout.AddStretchCell();
			Canvas.Layout.Add( avRow );

			var hint = new Label(
				"Recommended. A Variable named the same as this prop will be auto-created with Source=FromAcceptedProp so bindings inside this .sui can reference it directly.",
				Canvas );
			hint.WordWrap = true;
			hint.SetStyles( "color: #6b7280; font-size: 10px; padding: 0 0 4px 110px;" );
			Canvas.Layout.Add( hint );
		}

		_error = new Label( "", Canvas );
		_error.SetStyles( "color: #ef4444; font-size: 11px;" );
		_error.Visible = false;
		Canvas.Layout.Add( _error );

		Canvas.Layout.AddStretchCell();

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
		var lbl = new Label( label, row ) { FixedWidth = 110 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );

		var edit = new LineEdit( row ) { Text = value ?? "" };
		edit.FixedHeight = 26;
		if ( !string.IsNullOrEmpty( placeholder ) )
			edit.PlaceholderText = placeholder;
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
			ShowError( "Name must be a valid C# identifier." );
			return;
		}

		var p = new SuiAcceptedProp
		{
			PropId = _editingPropId ?? SuiAcceptedProp.NewPropId(),
			Name = name,
			Type = _type,
			Default = ParseDefault( _type, _defaultEdit.Text ),
			Required = _requiredToggle?.Value ?? false,
			Group = string.IsNullOrWhiteSpace( _groupEdit.Text ) ? null : _groupEdit.Text.Trim(),
			Description = string.IsNullOrWhiteSpace( _descEdit.Text ) ? null : _descEdit.Text.Trim(),
		};

		var createVar = !_isEdit && (_autoVariableToggle?.Value ?? false);
		OnAccept?.Invoke( p, createVar );
		Close();
	}

	private void ShowError( string msg )
	{
		_error.Text = msg;
		_error.Visible = true;
	}

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
				return string.IsNullOrEmpty( text ) ? null : JsonValue.Create( text );
		}
	}
}
