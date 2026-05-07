using Editor;
using Sandbox;
using SboxUiDesigner.EditorUi.Widgets;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Asset editor window for .sui documents — registered for the "sui" extension
/// via <see cref="EditorForAssetTypeAttribute"/>. Opens on double-click in the
/// Asset Browser.
///
/// M4 (current) — DockWindow with menu bar, toolbar, and 5 region docks
/// (Palette, Hierarchy, Canvas, Details, Bottom). Region widgets are visually
/// built but not yet wired to a controller.
///
/// M5 — wires <see cref="SuiDesignerController"/> for selection + dirty state +
/// command stack between region widgets.
///
/// M10 (Spike 01) — replaces the Canvas widget's placeholder with
/// Editor.SceneRenderingWidget hosting an editor-owned Scene with
/// ScreenPanel + the generated PanelComponent.
///
/// Pattern reference: Facepunch Sound Editor
/// (sbox-public/game/addons/tools/Code/Editor/SoundEditor/Window.cs).
/// </summary>
[EditorForAssetType( "sui" )]
public class SuiDesignerWindow : DockWindow, IAssetEditor
{
	public bool CanOpenMultipleAssets => false;

	private Asset _asset;
	private SuiAsset _resource;
	private SuiDocument _document;

	// Region widgets — recreated on hotload so we keep references for refresh.
	private SuiPaletteWidget _palette;
	private SuiHierarchyWidget _hierarchy;
	private SuiCanvasWidget _canvas;
	private SuiDetailsWidget _details;
	private SuiBottomDockWidget _bottom;

	private string _defaultDockState;

	public SuiDesignerWindow()
	{
		DeleteOnClose = true;
		WindowTitle = "Sbox UI Designer";
		Title = "Sbox UI Designer";
		Size = new Vector2( 1600, 900 );
		SetWindowIcon( "view_quilt" );

		BuildMenuBar();
		BuildToolBar();
		BuildDocks();

		Show();
	}

	// ─────────────────────────────────────────────────────────────────────
	//  IAssetEditor
	// ─────────────────────────────────────────────────────────────────────

	public void AssetOpen( Asset asset )
	{
		_asset = asset;
		_resource = asset?.LoadResource<SuiAsset>();
		_resource ??= new SuiAsset();

		_document = _resource.Document;
		if ( _document == null || _document.Elements.Count == 0 )
		{
			var nameHint = !string.IsNullOrEmpty( asset?.Name ) ? asset.Name : "NewUi";
			_document = SuiDocument.CreateDefault( nameHint );
			_resource.Document = _document;
		}

		PushDocumentToWidgets();
		RefreshTitle();
	}

	private void PushDocumentToWidgets()
	{
		_hierarchy?.SetDocument( _document );
		_canvas?.SetDocument( _document );
		_details?.SetDocument( _document );
	}

	private void RefreshTitle()
	{
		var docName = _document?.Name ?? "(unsaved)";
		WindowTitle = $"Sbox UI Designer — {docName}";
		Title = WindowTitle;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Save
	// ─────────────────────────────────────────────────────────────────────

	private void Save()
	{
		if ( _asset == null || _resource == null || _document == null )
		{
			Log.Warning( "[Sui] cannot save — no document loaded" );
			return;
		}

		var report = SuiDocumentValidator.Validate( _document );
		foreach ( var err in report.Errors )
			Log.Warning( $"[Sui] validation: {err}" );

		_resource.Document = _document;
		_asset.SaveToDisk( _resource );
		Log.Info( $"[Sui] saved {_asset.Path}" );
		RefreshTitle();
	}

	protected override bool OnClose()
	{
		Save();
		return true;
	}

	[Shortcut( "editor.save", "Ctrl+S", ShortcutType.Window )]
	private void OnShortcutSave() => Save();

	// ─────────────────────────────────────────────────────────────────────
	//  Hotload — DockWindow + IAssetEditor pattern requires rebuilding
	//  menu/toolbar/docks after a hotload (per the Sound Editor reference).
	// ─────────────────────────────────────────────────────────────────────

	[EditorEvent.Hotload]
	public void OnHotload()
	{
		SaveToStateCookie();

		DockManager.Clear();
		MenuBar.Clear();

		BuildMenuBar();
		BuildToolBar();
		BuildDocks();

		PushDocumentToWidgets();
		RefreshTitle();
	}

	protected override void RestoreDefaultDockLayout()
	{
		if ( !string.IsNullOrEmpty( _defaultDockState ) )
		{
			DockManager.State = _defaultDockState;
			SaveToStateCookie();
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Menu bar
	// ─────────────────────────────────────────────────────────────────────

	private void BuildMenuBar()
	{
		var file = MenuBar.AddMenu( "File" );
		file.AddOption( "Save", "save", Save, "editor.save" );
		file.AddOption( "Compile", "build", Compile );
		file.AddSeparator();
		file.AddOption( "Change Output Folder…", "folder_open", ChangeOutputFolder );
		file.AddOption( "Open Generated Folder", "folder", OpenGeneratedFolder );
		file.AddSeparator();
		file.AddOption( "Close", "close", Close );

		var edit = MenuBar.AddMenu( "Edit" );
		edit.AddOption( "Undo", "undo", Undo, "editor.undo" );
		edit.AddOption( "Redo", "redo", Redo, "editor.redo" );
		edit.AddSeparator();
		edit.AddOption( "Cut", "content_cut", () => { } );
		edit.AddOption( "Copy", "content_copy", () => { } );
		edit.AddOption( "Paste", "content_paste", () => { } );
		edit.AddOption( "Duplicate", "content_copy", () => { } );
		edit.AddOption( "Delete", "delete", () => { } );

		var view = MenuBar.AddMenu( "View" );
		view.AddOption( "Zoom In", "zoom_in", () => { } );
		view.AddOption( "Zoom Out", "zoom_out", () => { } );
		view.AddOption( "Fit to Screen", "fit_screen", () => { } );

		var tools = MenuBar.AddMenu( "Tools" );
		tools.AddOption( "Validate Document", "rule", ValidateDocument );
		tools.AddOption( "Regenerate Preview", "refresh", () => { } );
		tools.AddOption( "Clean Preview Cache", "delete_sweep", () => { } );

		var help = MenuBar.AddMenu( "Help" );
		help.AddOption( "Open PRD", "menu_book", () => { } );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Toolbar
	// ─────────────────────────────────────────────────────────────────────

	private void BuildToolBar()
	{
		var toolbar = new ToolBar( this, "SuiDesignerToolbar" );
		AddToolBar( toolbar, ToolbarPosition.Top );

		toolbar.AddOption( "Save", "save", Save ).StatusTip = "Save (Ctrl+S)";
		toolbar.AddOption( "Compile", "build", Compile ).StatusTip = "Compile to .razor / .razor.scss (M9 wires this)";
		toolbar.AddSeparator();
		toolbar.AddOption( "Refresh Preview", "refresh", () => { } ).StatusTip = "Regenerate preview cache (M10)";
		toolbar.AddOption( "Validate", "rule", ValidateDocument ).StatusTip = "Run schema validation";
		toolbar.AddSeparator();
		toolbar.AddOption( "Undo", "undo", Undo ).StatusTip = "Undo (Ctrl+Z) — M5 wires this";
		toolbar.AddOption( "Redo", "redo", Redo ).StatusTip = "Redo (Ctrl+Y) — M5 wires this";
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Docks
	// ─────────────────────────────────────────────────────────────────────

	private void BuildDocks()
	{
		_palette = new SuiPaletteWidget( this );
		_hierarchy = new SuiHierarchyWidget( this );
		_canvas = new SuiCanvasWidget( this );
		_details = new SuiDetailsWidget( this );
		_bottom = new SuiBottomDockWidget( this );

		// Wire selection: clicking the hierarchy updates Details. M5 routes this
		// through SuiDesignerController so palette ↔ canvas ↔ details share state.
		_hierarchy.ElementSelected += el => _details.SetSelected( el );

		// Wire palette: M6 will hook ElementRequested into the controller's
		// AddElement command. For now we just log so the user sees it works.
		_palette.ElementRequested += type => Log.Info( $"[Sui] palette requested: {type} (M6 wires AddElement)" );

		DockManager.RegisterDockType( "Palette", "category", null, false );
		DockManager.RegisterDockType( "Hierarchy", "account_tree", null, false );
		DockManager.RegisterDockType( "Canvas", "crop_free", null, false );
		DockManager.RegisterDockType( "Details", "tune", null, false );
		DockManager.RegisterDockType( "Bottom", "build", null, false );

		// Layout, left-to-right: Palette + Hierarchy stacked on Left, Canvas in center,
		// Details on the Right, BottomDock spans below.
		DockManager.AddDock( null, _canvas, DockArea.Left, DockManager.DockProperty.HideOnClose );
		DockManager.AddDock( _canvas, _palette, DockArea.Left, DockManager.DockProperty.HideOnClose, 0.18f );
		DockManager.AddDock( _palette, _hierarchy, DockArea.Bottom, DockManager.DockProperty.HideOnClose, 0.5f );
		DockManager.AddDock( _canvas, _details, DockArea.Right, DockManager.DockProperty.HideOnClose, 0.22f );
		DockManager.AddDock( null, _bottom, DockArea.BottomOuter, DockManager.DockProperty.HideOnClose, 0.22f );

		DockManager.Update();
		_defaultDockState = DockManager.State;

		if ( StateCookie != "SuiDesigner" )
		{
			StateCookie = "SuiDesigner";
		}
		else
		{
			RestoreFromStateCookie();
		}
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Stub commands (real implementations land in later milestones)
	// ─────────────────────────────────────────────────────────────────────

	private void Compile()
	{
		Log.Info( "[Sui] Compile clicked — generator lands in M9" );
	}

	private void ChangeOutputFolder()
	{
		Log.Info( "[Sui] Change Output — picker lands in M12" );
	}

	private void OpenGeneratedFolder()
	{
		Log.Info( "[Sui] Open Generated Folder — wired in M12" );
	}

	private void Undo()
	{
		Log.Info( "[Sui] Undo — command stack lands in M5" );
	}

	private void Redo()
	{
		Log.Info( "[Sui] Redo — command stack lands in M5" );
	}

	private void ValidateDocument()
	{
		if ( _document == null )
		{
			Log.Warning( "[Sui] no document loaded" );
			return;
		}
		var report = SuiDocumentValidator.Validate( _document );
		if ( report.IsValid )
		{
			Log.Info( $"[Sui] document is valid ({_document.Elements.Count} elements)" );
		}
		else
		{
			foreach ( var e in report.Errors ) Log.Error( $"[Sui] {e}" );
		}
	}
}
