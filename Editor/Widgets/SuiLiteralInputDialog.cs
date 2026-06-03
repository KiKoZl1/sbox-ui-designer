using System;
using System.Globalization;
using System.Text.Json.Nodes;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Modal that captures a typed literal value for a converter argument
/// (PRD 18 § 11 #4). The dialog adapts its editor to the expected parameter
/// type — LineEdit for string/numeric, two Checkboxes for bool, hex LineEdit
/// for Color, comma-separated triple for Vector3, etc. The result is returned
/// as a <see cref="JsonNode"/> so it can be stored on
/// <see cref="SuiConverterArg.Literal"/> verbatim.
///
/// Issue #7 — without this dialog the user could only feed Variables into
/// converters; constants like <c>Divide(Health, 2)</c> were impossible without
/// declaring a throwaway Variable.
/// </summary>
public sealed class SuiLiteralInputDialog : Window
{
	public Action<JsonNode> OnAccept;

	private readonly string _paramType;
	private readonly JsonNode _initial;
	private LineEdit _textEdit;
	private Checkbox _boolEdit;
	private Label _error;

	public SuiLiteralInputDialog( string paramType, JsonNode initial = null )
	{
		_paramType = NormalizeForUi( paramType ?? "string" );
		_initial = initial;

		Title = $"Literal value ({_paramType})";
		WindowTitle = Title;
		Size = new Vector2( 360, 180 );
		MinimumSize = new Vector2( 360, 180 );
		SetWindowIcon( "edit" );
		DeleteOnClose = true;

		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Margin = 14;
		Canvas.Layout.Spacing = 8;

		BuildLayout();
		Show();
	}

	private void BuildLayout()
	{
		// Hint row — reminds the user what they're typing for.
		var hint = new Label( $"Enter a constant value for an input of type '{_paramType}'.", Canvas );
		hint.SetStyles( "color: #9ca3af; font-size: 11px;" );
		Canvas.Layout.Add( hint );

		switch ( _paramType )
		{
			case "bool":
				BuildBoolEditor();
				break;

			case "Color":
				BuildTextEditor( "#ffffffff", "hex (e.g. #ff00aa or rgba)" );
				break;

			case "Vector2":
				BuildTextEditor( "0, 0", "x, y" );
				break;

			case "Vector3":
				BuildTextEditor( "0, 0, 0", "x, y, z" );
				break;

			case "Vector4":
				BuildTextEditor( "0, 0, 0, 0", "x, y, z, w" );
				break;

			case "Angles":
				BuildTextEditor( "0, 0, 0", "pitch, yaw, roll" );
				break;

			default:
				BuildTextEditor( "", PlaceholderFor( _paramType ) );
				break;
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
		var ok = new Button( "OK", buttons );
		ok.Clicked = Accept;
		buttons.Layout.Add( ok );
		Canvas.Layout.Add( buttons );
	}

	private void BuildTextEditor( string defaultText, string placeholder )
	{
		var row = new Widget( Canvas );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 8;
		var lbl = new Label( "Value", row ) { FixedWidth = 60 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );

		_textEdit = new LineEdit( row );
		_textEdit.Text = InitialAsString( defaultText );
		_textEdit.PlaceholderText = placeholder ?? "";
		_textEdit.FixedHeight = 26;
		_textEdit.SetStyles(
			"background-color: rgb(20,20,19);" +
			"border: 1px solid rgba(255,255,255,0.12);" +
			"border-radius: 3px; padding: 0 6px;" +
			"color: #e5e7eb;" );
		row.Layout.Add( _textEdit, 1 );

		Canvas.Layout.Add( row );
	}

	private void BuildBoolEditor()
	{
		var row = new Widget( Canvas );
		row.Layout = Layout.Row();
		row.Layout.Spacing = 8;
		var lbl = new Label( "Value", row ) { FixedWidth = 60 };
		lbl.SetStyles( "color: #9ca3af; font-size: 11px;" );
		row.Layout.Add( lbl );

		_boolEdit = new Checkbox( "true", row );
		_boolEdit.Value = InitialAsBool();
		row.Layout.Add( _boolEdit, 1 );

		Canvas.Layout.Add( row );
	}

	private string InitialAsString( string fallback )
	{
		if ( _initial == null ) return fallback ?? "";
		try
		{
			// Strings come quoted from ToJsonString — strip outer quotes for editing.
			if ( _paramType == "string" )
			{
				try { return _initial.GetValue<string>(); } catch { /* fallthrough */ }
			}
			var s = _initial.ToJsonString();
			if ( !string.IsNullOrEmpty( s ) && s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"' )
				s = s.Substring( 1, s.Length - 2 );
			return s;
		}
		catch { return fallback ?? ""; }
	}

	private bool InitialAsBool()
	{
		if ( _initial == null ) return false;
		try { return _initial.GetValue<bool>(); } catch { return false; }
	}

	private void Accept()
	{
		try
		{
			JsonNode node;
			switch ( _paramType )
			{
				case "int":
					if ( !long.TryParse( _textEdit.Text ?? "", NumberStyles.Any, CultureInfo.InvariantCulture, out var i ) )
					{
						ShowError( "Not a valid integer." );
						return;
					}
					node = JsonValue.Create( i );
					break;

				case "long":
					if ( !long.TryParse( _textEdit.Text ?? "", NumberStyles.Any, CultureInfo.InvariantCulture, out var lv ) )
					{
						ShowError( "Not a valid long integer." );
						return;
					}
					node = JsonValue.Create( lv );
					break;

				case "float":
				case "double":
					if ( !double.TryParse( _textEdit.Text ?? "", NumberStyles.Any, CultureInfo.InvariantCulture, out var d ) )
					{
						ShowError( "Not a valid number." );
						return;
					}
					node = JsonValue.Create( d );
					break;

				case "bool":
					node = JsonValue.Create( _boolEdit?.Value ?? false );
					break;

				default:
					// Color / Vector*  / strings — keep the raw text. The generation
					// layer (SuiTypeMapper.DefaultLiteral, extended in #8) parses it
					// per-type at codegen time.
					node = JsonValue.Create( _textEdit.Text ?? "" );
					break;
			}

			OnAccept?.Invoke( node );
			Close();
		}
		catch ( Exception ex )
		{
			ShowError( "Could not parse value: " + ex.Message );
		}
	}

	private void ShowError( string msg )
	{
		if ( _error == null ) return;
		_error.Text = msg;
		_error.Visible = true;
	}

	/// <summary>Map converter-catalog C# type names back to the closed UI set.</summary>
	private static string NormalizeForUi( string t )
	{
		return t switch
		{
			"Single"  => "float",
			"Double"  => "double",
			"Int32"   => "int",
			"Int64"   => "long",
			"Boolean" => "bool",
			"String"  => "string",
			"T"       => "string",   // generic — treat as free-form text
			"object"  => "string",
			_         => t,
		};
	}

	private static string PlaceholderFor( string t ) => t switch
	{
		"int"     => "e.g. 42",
		"long"    => "e.g. 1000000000",
		"float"   => "e.g. 1.5",
		"double"  => "e.g. 3.14159",
		"string"  => "text",
		_         => "",
	};
}
