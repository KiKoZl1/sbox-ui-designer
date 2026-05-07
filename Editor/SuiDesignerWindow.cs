using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Asset editor window for .sui documents. Registered for the "sui" extension
/// via <see cref="EditorForAssetTypeAttribute"/>; the editor opens on
/// double-click in the Asset Browser.
///
/// M3 (current) — minimum skeleton: window opens, loads the document, saves
/// on Ctrl+S, shows a placeholder body.
/// M4 — full layout: menu bar, toolbar, palette, hierarchy, canvas (with
/// SceneRenderingWidget preview host), details, bottom dock.
/// M5 — wires the SuiDesignerController for selection, dirty state, commands.
/// </summary>
[EditorForAssetType( "sui" )]
public class SuiDesignerWindow : Window, IAssetEditor
{
	public bool CanOpenMultipleAssets => false;

	private Asset _asset;
	private SuiAsset _resource;
	private SuiDocument _document;

	private Label _placeholderLabel;
	private Label _statusLabel;

	public SuiDesignerWindow()
	{
		DeleteOnClose = true;
		Title = "Sbox UI Designer";
		Size = new Vector2( 1600, 900 );
		SetWindowIcon( "view_quilt" );

		// Window's Layout cannot host child Widgets directly — we always
		// build inside an explicit Canvas Widget per the s&box editor pattern.
		Canvas = new Widget( null );
		Canvas.Layout = Layout.Column();
		Canvas.Layout.Spacing = 0;

		BuildPlaceholderUi();

		Show();
	}

	private void BuildPlaceholderUi()
	{
		// Top toolbar — temporary, M4 replaces with the full toolbar widget.
		var toolbarRow = Canvas.Layout.AddRow();
		toolbarRow.Margin = 6;
		toolbarRow.Spacing = 4;

		var saveBtn = new Button( "Save", "save", Canvas );
		saveBtn.Clicked += Save;
		saveBtn.ToolTip = "Save the .sui document (Ctrl+S)";
		toolbarRow.Add( saveBtn );

		var compileBtn = new Button( "Compile", "build", Canvas );
		compileBtn.Clicked += () => { /* M9 will hook the generator here */ };
		compileBtn.ToolTip = "Generate .razor / .razor.scss (not implemented in M3)";
		compileBtn.Enabled = false;
		toolbarRow.Add( compileBtn );

		toolbarRow.AddStretchCell();

		_statusLabel = new Label( "(no document loaded)", Canvas );
		toolbarRow.Add( _statusLabel );

		// Body — placeholder for now.
		_placeholderLabel = new Label( "Sbox UI Designer — M3 skeleton.\nFull editor shell lands in M4.", Canvas );
		_placeholderLabel.Alignment = TextFlag.Center;
		Canvas.Layout.Add( _placeholderLabel, 1 );
	}

	public void AssetOpen( Asset asset )
	{
		_asset = asset;
		_resource = asset?.LoadResource<SuiAsset>();

		if ( _resource == null )
		{
			_resource = new SuiAsset();
		}

		_document = _resource.Document;
		if ( _document == null || _document.Elements.Count == 0 )
		{
			// Asset is brand new (just created via right-click → New) — populate
			// with a default Root canvas so the user sees something on open.
			var nameHint = !string.IsNullOrEmpty( asset?.Name ) ? asset.Name : "NewUi";
			_document = SuiDocument.CreateDefault( nameHint );
			_resource.Document = _document;
		}

		RefreshTitleAndStatus();
	}

	public void Save()
	{
		if ( _asset == null || _resource == null || _document == null )
		{
			Log.Warning( "[Sui] cannot save — no document loaded" );
			return;
		}

		// Validate before writing — we don't block save on warnings, but errors
		// are surfaced to the user (M11/M12 will turn this into the Compile Results
		// panel; for M3 we just log).
		var report = SuiDocumentValidator.Validate( _document );
		if ( !report.IsValid )
		{
			foreach ( var err in report.Errors )
				Log.Warning( $"[Sui] validation: {err}" );
		}

		_resource.Document = _document;
		_asset.SaveToDisk( _resource );
		Log.Info( $"[Sui] saved {_asset.Path}" );
		RefreshTitleAndStatus();
	}

	protected override bool OnClose()
	{
		// Auto-save on close. Pattern lifted from DialogueEditorWindow
		// (clover_meadows_sbox). M11/M12 may revisit this with a "save changes?"
		// dialog when the dirty-state is wired through.
		Save();
		return true;
	}

	private void RefreshTitleAndStatus()
	{
		var docName = _document?.Name ?? "(unsaved)";
		Title = $"Sbox UI Designer — {docName}";
		if ( _statusLabel != null )
		{
			var elementCount = _document?.Elements.Count ?? 0;
			_statusLabel.Text = $"{docName}  ·  {elementCount} element{(elementCount == 1 ? "" : "s")}";
		}
	}

	[Shortcut( "editor.save", "Ctrl+S", ShortcutType.Window )]
	private void OnShortcutSave() => Save();
}
