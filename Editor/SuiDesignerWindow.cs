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

	// M5 — controller mediates selection/dirty/commands between widgets.
	private readonly SuiDesignerController _controller = new();

	// Convenience access to the document; comes from the controller.
	private SuiDocument Document => _controller.Document;

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

		_controller.DocumentChanged += OnControllerDocumentChanged;
		_controller.SelectionChanged += OnControllerSelectionChanged;
		_controller.DirtyChanged += OnControllerDirtyChanged;

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

		var doc = _resource.Document;
		if ( doc == null || doc.Elements.Count == 0 )
		{
			var nameHint = !string.IsNullOrEmpty( asset?.Name ) ? asset.Name : "NewUi";
			doc = SuiDocument.CreateDefault( nameHint );
			_resource.Document = doc;
		}

		_controller.SetDocument( doc );
		// Controller raises DocumentChanged + SelectionChanged synchronously,
		// which pushes state to all region widgets.
	}

	private void OnControllerDocumentChanged()
	{
		_hierarchy?.SetDocument( Document );
		_canvas?.SetDocument( Document );
		_details?.SetDocument( Document );
		// Selection might also be affected (e.g. when document changes).
		OnControllerSelectionChanged();
		RefreshTitle();
	}

	private void OnControllerSelectionChanged()
	{
		_hierarchy?.SetSelected( _controller.Selected );
		_details?.SetSelected( _controller.Selected );
	}

	private void OnControllerDirtyChanged()
	{
		RefreshTitle();
	}

	private void RefreshTitle()
	{
		var docName = Document?.Name ?? "(unsaved)";
		var dirtyMark = _controller.IsDirty ? " *" : "";
		WindowTitle = $"Sbox UI Designer — {docName}{dirtyMark}";
		Title = WindowTitle;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Save
	// ─────────────────────────────────────────────────────────────────────

	private void Save()
	{
		if ( _asset == null || _resource == null || Document == null )
		{
			Log.Warning( "[Sui] cannot save — no document loaded" );
			return;
		}

		var report = SuiDocumentValidator.Validate( Document );
		foreach ( var err in report.Errors )
			Log.Warning( $"[Sui] validation: {err}" );

		_resource.Document = Document;
		_asset.SaveToDisk( _resource );
		Log.Info( $"[Sui] saved {_asset.Path}" );
		_controller.MarkSaved();
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

		// All widget actions route through the controller so undo/redo, dirty
		// state, and selection stay coherent across the editor.
		_hierarchy.ElementSelected += el => _controller.SetSelected( el );
		_palette.ElementRequested += type => _controller.AddElement( type );

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

	private void Undo() => _controller.Undo();
	private void Redo() => _controller.Redo();

	[Shortcut( "editor.undo", "Ctrl+Z", ShortcutType.Window )]
	private void OnShortcutUndo() => Undo();

	[Shortcut( "editor.redo", "Ctrl+Y", ShortcutType.Window )]
	private void OnShortcutRedo() => Redo();

	[Shortcut( "editor.delete", "Del", ShortcutType.Window )]
	private void OnShortcutDelete() => _controller.DeleteElement();

	private void ValidateDocument()
	{
		if ( Document == null )
		{
			Log.Warning( "[Sui] no document loaded" );
			return;
		}
		var report = SuiDocumentValidator.Validate( Document );
		if ( report.IsValid )
		{
			Log.Info( $"[Sui] document is valid ({Document.Elements.Count} elements)" );
		}
		else
		{
			foreach ( var e in report.Errors ) Log.Error( $"[Sui] {e}" );
		}
	}
}
