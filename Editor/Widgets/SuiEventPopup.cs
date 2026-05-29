using System;
using System.Collections.Generic;
using Editor;
using Sandbox;
using SboxUiDesigner.Generation;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// V1.5 M3 (PRD 20 § 4.2) — popup for wiring an event slot. Same shape as
/// <see cref="SuiBindPopup"/>: pick element → pick event from the matrix →
/// pick mode (Code/Doo) → type handler name → OK fires
/// <see cref="OnAccept"/> with (elementId, eventName, binding).
///
/// <para>The Designer drives this from the Events bottom tab's "+ Add Event"
/// button (new) and the per-row Edit buttons (pre-fills the form). The result
/// flows through <c>SuiSetEventCommand</c> for undo/redo.</para>
/// </summary>
public sealed class SuiEventPopup : Window
{
	/// <summary>Fired on OK with (elementId, eventName, binding).</summary>
	public Action<string, string, SuiEventBinding> OnAccept;

	private readonly SuiDocument _doc;
	private readonly string _lockedElementId;
	private readonly string _lockedEventName;

	private SuiElement _element;
	private string _eventName;
	private SuiEventMode _mode = SuiEventMode.Code;
	private string _handler = "";
	private string _dooProperty = "";

	private Button _elementBtn;
	private Button _eventBtn;
	private SuiDropdownField _modeDd;
	private LineEdit _handlerEdit;
	private Label _error;

	public SuiEventPopup(
		SuiDocument doc,
		string elementId = null,
		string eventName = null,
		SuiEventBinding existing = null )
	{
		_doc = doc;
		_lockedElementId = elementId;
		_lockedEventName = eventName;

		_element = string.IsNullOrEmpty( elementId ) ? null : doc?.GetElement( elementId );
		_eventName = eventName;
		if ( existing != null )
		{
			_mode = existing.Mode;
			_handler = existing.Handler ?? "";
			_dooProperty = existing.DooPropertyName ?? "";
		}

		Title = existing != null ? "Edit Event" : "Bind Event";
		WindowTitle = Title;
		Size = new Vector2( 460, 260 );
		MinimumSize = new Vector2( 460, 260 );
		SetWindowIcon( "flash_on" );
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
		// Element picker — disabled when the popup was opened with a locked
		// elementId (Edit mode keeps the user from changing the target).
		_elementBtn = new Button( _element?.Name ?? "Pick element…", "widgets", Canvas );
		_elementBtn.Enabled = string.IsNullOrEmpty( _lockedElementId );
		_elementBtn.Clicked = OpenElementMenu;
		Canvas.Layout.Add( _elementBtn );

		_eventBtn = new Button( _eventName ?? "Pick event…", "flash_on", Canvas );
		_eventBtn.Enabled = _element != null && string.IsNullOrEmpty( _lockedEventName );
		_eventBtn.Clicked = OpenEventMenu;
		Canvas.Layout.Add( _eventBtn );

		// Mode dropdown.
		_modeDd = new SuiDropdownField( Canvas );
		_modeDd.SetOptions( new[] { "Code", "Doo" } );
		_modeDd.Value = _mode == SuiEventMode.Doo ? "Doo" : "Code";
		_modeDd.ValueSelected += v =>
		{
			_mode = v == "Doo" ? SuiEventMode.Doo : SuiEventMode.Code;
			RefreshHandlerField();
		};
		Canvas.Layout.Add( _modeDd );

		_handlerEdit = new LineEdit( Canvas );
		_handlerEdit.PlaceholderText = "OnFooClick";
		_handlerEdit.SetStyles(
			"background-color: rgb(20,20,19);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 3px;" );
		_handlerEdit.TextEdited += t =>
		{
			if ( _mode == SuiEventMode.Code ) _handler = t ?? "";
			else _dooProperty = t ?? "";
		};
		Canvas.Layout.Add( _handlerEdit );

		_error = new Label( "", Canvas );
		_error.SetStyles( "color: #ef4444; font-size: 11px;" );
		Canvas.Layout.Add( _error );

		Canvas.Layout.AddStretchCell();

		// OK / Cancel
		var btns = new Widget( Canvas );
		btns.Layout = Layout.Row();
		btns.Layout.Spacing = 8;
		var cancel = new Button( "Cancel", "close", btns );
		cancel.Clicked = Close;
		btns.Layout.AddStretchCell();
		btns.Layout.Add( cancel );
		var ok = new Button( "OK", "check", btns );
		ok.Clicked = OnOk;
		btns.Layout.Add( ok );
		Canvas.Layout.Add( btns );

		RefreshHandlerField();
	}

	private void OpenElementMenu()
	{
		var menu = new Menu();
		if ( _doc?.Elements != null )
		{
			foreach ( var el in _doc.Elements )
			{
				if ( el == null ) continue;
				if ( SuiEventMatrix.For( el.Type ).Count == 0 ) continue;
				var label = el.Name ?? el.Id;
				var cap = el;
				menu.AddOption( label, "widgets", () =>
				{
					_element = cap;
					_elementBtn.Text = cap.Name ?? cap.Id;
					_eventBtn.Enabled = true;
					_eventName = null;
					_eventBtn.Text = "Pick event…";
				} );
			}
		}
		menu.OpenAtCursor();
	}

	private void OpenEventMenu()
	{
		if ( _element == null ) return;
		var menu = new Menu();
		foreach ( var entry in SuiEventMatrix.For( _element.Type ) )
		{
			var capName = entry.Name;
			menu.AddOption( entry.Name, "flash_on", () =>
			{
				_eventName = capName;
				_eventBtn.Text = capName;
				// Default handler name if user hasn't typed yet.
				if ( string.IsNullOrEmpty( _handler ) && _mode == SuiEventMode.Code )
				{
					_handler = SuggestName( _element, capName );
					_handlerEdit.Text = _handler;
				}
				if ( string.IsNullOrEmpty( _dooProperty ) && _mode == SuiEventMode.Doo )
				{
					_dooProperty = SuggestName( _element, capName );
					_handlerEdit.Text = _dooProperty;
				}
			} );
		}
		menu.OpenAtCursor();
	}

	private void RefreshHandlerField()
	{
		_handlerEdit.Text = _mode == SuiEventMode.Code ? _handler : _dooProperty;
		_handlerEdit.PlaceholderText = _mode == SuiEventMode.Code ? "OnFooClick" : "DooPropertyName";
	}

	private void OnOk()
	{
		if ( _element == null )    { _error.Text = "Pick an element first."; return; }
		if ( string.IsNullOrEmpty( _eventName ) ) { _error.Text = "Pick an event."; return; }

		var binding = new SuiEventBinding { Mode = _mode };
		if ( _mode == SuiEventMode.Code )
		{
			if ( string.IsNullOrEmpty( _handler ) )
			{
				_error.Text = "Handler name is required in Code mode.";
				return;
			}
			binding.Handler = _handler;
		}
		else
		{
			if ( string.IsNullOrEmpty( _dooProperty ) )
			{
				_error.Text = "DooPropertyName is required in Doo mode.";
				return;
			}
			binding.DooPropertyName = _dooProperty;
		}

		OnAccept?.Invoke( _element.Id, _eventName, binding );
		Close();
	}

	private static string SuggestName( SuiElement el, string eventName )
	{
		var elName = SuiNameSanitizer.ToCSharpIdentifier( el?.Name ?? "" );
		if ( string.IsNullOrEmpty( elName ) ) return eventName ?? "OnEvent";
		var suffix = eventName != null && eventName.StartsWith( "On" )
			? eventName.Substring( 2 ) : eventName;
		return "On" + elName + suffix;
	}
}
