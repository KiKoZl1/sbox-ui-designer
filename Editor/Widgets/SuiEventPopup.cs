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
	// WBP-like Doo storage — schema-side Doo Body authored inside the popup
	// via inline BlockTree + DooEditorWidget popup. Persists into the .sui
	// when OK is pressed (SuiEventBinding.DooBody).
	private Sandbox.Doo _dooBody;

	private Button _elementBtn;
	private Button _eventBtn;
	private SuiDropdownField _modeDd;
	private LineEdit _handlerEdit;
	private Label _error;
	// Doo-mode-only widgets — created lazily, hidden when Mode == Code.
	private Widget _dooSection;
	private Editor.DooEditor.BlockTree _dooTree;
	private Label _dooStatus;

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
			_dooBody = existing.DooBody;
		}

		Title = existing != null ? "Edit Event" : "Bind Event";
		WindowTitle = Title;
		Size = new Vector2( 520, 460 );
		MinimumSize = new Vector2( 520, 460 );
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

		BuildDooSection();
		Canvas.Layout.Add( _dooSection, 1 );

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
		RefreshDooSection();
	}

	/// <summary>
	/// Build the Doo section once. Visibility flips with the Mode dropdown.
	/// Contains a status line + "Open Full Editor" button + inline BlockTree
	/// preview that mirrors the engine's BlockTree widget — same rebuild
	/// path on every <c>[EditorEvent.Frame]</c> tick.
	/// </summary>
	private void BuildDooSection()
	{
		_dooSection = new Widget( Canvas );
		_dooSection.Layout = Layout.Column();
		_dooSection.Layout.Spacing = 6;
		_dooSection.Layout.Margin = new Sandbox.UI.Margin( 0, 6, 0, 0 );
		_dooSection.SetStyles(
			"background-color: rgb(28,28,27);" +
			"border: 1px solid rgba(255,255,255,0.06);" +
			"border-radius: 4px;" );

		var header = new Widget( _dooSection );
		header.Layout = Layout.Row();
		header.Layout.Spacing = 6;
		header.Layout.Margin = new Sandbox.UI.Margin( 8, 6, 8, 0 );
		header.FixedHeight = 30;

		_dooStatus = new Label( "Empty", header );
		_dooStatus.SetStyles( "color: #c4b5fd; font-size: 11px; font-weight: 600;" );
		header.Layout.Add( _dooStatus, 1 );

		var openBtn = new Button( "Open Full Editor", "rebase_edit", header );
		openBtn.Clicked = OpenDooEditor;
		header.Layout.Add( openBtn );

		_dooSection.Layout.Add( header );

		// BlockTree gets a parent right away so its [EditorEvent.Frame]
		// rebuild path resolves cleanly. When _dooBody is null we still
		// host an empty BlockTree(null) — the engine guards against null
		// _doo in its ContentHash + BuildNodes.
		EnsureDooBody();
		_dooTree = new Editor.DooEditor.BlockTree( _dooBody );
		_dooTree.MinimumHeight = 180;
		_dooSection.Layout.Add( _dooTree, 1 );
	}

	private void RefreshDooSection()
	{
		if ( _dooSection == null ) return;
		_dooSection.Visible = _mode == SuiEventMode.Doo;
		UpdateDooStatus();
	}

	private void UpdateDooStatus()
	{
		if ( _dooStatus == null ) return;
		if ( _dooBody == null || _dooBody.IsEmpty() )
			_dooStatus.Text = "Empty — click Open Full Editor to author blocks";
		else
			_dooStatus.Text = _dooBody.GetLabel();
	}

	private void EnsureDooBody()
	{
		_dooBody ??= new Sandbox.Doo();
	}

	/// <summary>
	/// Pop the engine's <see cref="Editor.DooEditor.DooEditorWidget"/> as a
	/// floating tool window targeted at our in-memory <see cref="_dooBody"/>.
	/// Edits inside the popup mutate the Doo reference in-place, so on close
	/// our OK button captures the final body without an explicit sync.
	///
	/// <para>Doo standalone <c>SerializedObject</c>s leave <c>ParentProperty</c>
	/// null, which crashes the engine's <c>DooEditorWidget.RebuildUI</c> when
	/// it tries to iterate the (also null) argument hints. To work around
	/// that we wrap the Doo in a holder class that has a real <c>[Property]
	/// Doo Body</c> field, build the SerializedObject from the holder, then
	/// open the editor against the Body property's nested object — same shape
	/// the engine's own <c>DooControlWidget</c> uses.</para>
	/// </summary>
	private void OpenDooEditor()
	{
		EnsureDooBody();

		var holder = new DooEditorHolder { Body = _dooBody };
		var holderSo = EditorTypeLibrary.GetSerializedObject( holder );
		var bodyProp = holderSo?.GetProperty( nameof( DooEditorHolder.Body ) );
		if ( bodyProp == null || !bodyProp.TryGetAsObject( out var dooSo ) || dooSo == null )
		{
			_error.Text = "Could not open Doo Editor — failed to build SerializedObject.";
			return;
		}

		var title = _element != null && !string.IsNullOrEmpty( _eventName )
			? $"{_element.Name}.{_eventName}"
			: "Doo";

		Editor.DooEditor.DooEditorWidget.Open( dooSo, title );
		UpdateDooStatus();
	}

	/// <summary>
	/// Wrapper class — exists only to give the standalone Doo a real
	/// <see cref="SerializedProperty"/> the engine's DooEditor can derive
	/// ArgumentHints from. Without this, the editor RebuildUI NREs on a null
	/// ParentProperty path.
	/// </summary>
	private class DooEditorHolder
	{
		[Property] public Sandbox.Doo Body { get; set; }
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
			// Persist the authored Body. Null + empty Body are both legal
			// (an empty Doo means "slot exists, nothing wired yet") so we
			// drop the field only when the user never opened the editor.
			if ( _dooBody != null && !_dooBody.IsEmpty() )
				binding.DooBody = _dooBody;
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
