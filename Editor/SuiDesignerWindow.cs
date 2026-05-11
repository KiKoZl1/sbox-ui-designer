using Editor;
using Sandbox;
using SboxUiDesigner.EditorUi.Canvas;
using SboxUiDesigner.EditorUi.Commands;
using SboxUiDesigner.EditorUi.Widgets;
using SboxUiDesigner.Generation;
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

	/// <summary>
	/// IAssetEditor contract — invoked when a host (e.g. graph editor) asks the
	/// asset editor to focus on a named member of the asset. M5 has no member
	/// navigation surface, so this is intentionally a no-op until M8 wires the
	/// Details panel's per-property editors and gives them stable names.
	/// </summary>
	public void SelectMember( string memberName ) { }

	private Asset _asset;
	private SuiAsset _resource;

	// M5 — controller mediates selection/dirty/commands between widgets.
	private readonly SuiDesignerController _controller = new();

	// Convenience access to the document; comes from the controller.
	private SuiDocument Document => _controller.Document;

	// Region widgets — owned by our custom layout (NO DockManager).
	private SuiPaletteWidget _palette;
	private SuiHierarchyWidget _hierarchy;
	private SuiCenterTabsWidget _centerTabs;
	// Backwards-compatible accessor — returns the canvas inside the center tabs.
	private SuiCanvasWidget _canvas => _centerTabs?.Canvas;
	private SuiDetailsWidget _details;
	private SuiBottomTabsWidget _bottomTabs;
	// Backwards-compatible accessors (so existing code paths don't break).
	private SuiCompileResultsWidget _compileResults => _bottomTabs?.CompileResults;

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

	/// <summary>
	/// Set when the asset's file on disk has size suggesting it should contain
	/// elements, but deserialization produced an empty Document. Cause: invalid
	/// enum value, schema mismatch, or other JSON parse error that
	/// System.Text.Json swallowed silently (it returns null/empty for the
	/// failing collection rather than throwing all the way up to us).
	///
	/// While true, <see cref="Save"/> and <see cref="OnClose"/> REFUSE to write
	/// back to disk — otherwise auto-save would replace the user's file with a
	/// 1-element default doc, destroying their work.
	/// </summary>
	private bool _loadLikelyFailed;

	public void AssetOpen( Asset asset )
	{
		_asset = asset;
		_resource = asset?.LoadResource<SuiAsset>();
		var hadExistingResource = _resource != null;
		_resource ??= new SuiAsset();

		var doc = _resource.Document;
		_loadLikelyFailed = false;

		if ( doc == null || doc.Elements.Count == 0 )
		{
			// Distinguish "brand new asset" from "existing file failed to parse".
			// New asset: no file on disk (or trivially small). Safe to seed with default.
			// Failed parse: file exists with non-trivial size. NEVER replace — that
			// would erase the user's data when OnClose triggers auto-save.
			var path = asset?.AbsolutePath;
			long fileSize = 0;
			if ( !string.IsNullOrEmpty( path ) )
			{
				try { fileSize = new System.IO.FileInfo( path ).Length; } catch { /* ignored */ }
			}

			// 1KB is roughly the size of a minimal valid .sui (just a default root).
			// Anything bigger almost certainly had children that failed to parse.
			const long suspiciousSizeThreshold = 1024;
			var probableLoadFailure = hadExistingResource && fileSize > suspiciousSizeThreshold;

			if ( probableLoadFailure )
			{
				_loadLikelyFailed = true;
				Log.Error(
					$"[Sui] Failed to load '{asset?.Path}' — the file on disk is {fileSize} bytes " +
					$"but parsed to {doc?.Elements?.Count ?? 0} element(s). This usually means an " +
					$"invalid enum value (e.g. FontWeight='Black' — valid values are Normal/Bold/Light/Medium/SemiBold/ExtraBold). " +
					$"Save is DISABLED while in this state to avoid overwriting your file. " +
					$"Close the editor WITHOUT saving and fix the JSON, or recreate the .sui." );
			}

			var nameHint = !string.IsNullOrEmpty( asset?.Name ) ? asset.Name : "NewUi";
			doc = SuiDocument.CreateDefault( nameHint );
			_resource.Document = doc;
		}

		// Apply schema migrations (idempotent) — converts pre-2026-05-08 Text
		// elements with explicit W/H to Fixed mode so Auto doesn't shrink them.
		SuiDocumentMigration.Apply( doc );

		_controller.SetDocument( doc );
		// Controller raises DocumentChanged + SelectionChanged synchronously,
		// which pushes state to all region widgets.
	}

	private void OnControllerDocumentChanged()
	{
		_hierarchy?.SetDocument( Document );
		_centerTabs?.SetDocument( Document );
		_bottomTabs?.SetDocument( Document );
		_details?.SetDocument( Document );
		// IMPORTANT: do NOT cascade into OnControllerSelectionChanged here.
		// DocumentChanged fires on every property edit (e.g. dragging a color
		// slider commits per-frame). If we cascaded, Details would tear down
		// + rebuild all rows 60Hz, leaving LineEdits half-painted.
		//
		// Selection-derived UI (Hierarchy highlight, Details rows) only needs
		// to refresh when the selection itself OR data on the SELECTED element
		// changes — both of those come through the SelectionChanged event
		// independently. Canvas + Hierarchy.SetDocument above already invalidate
		// for repaint.
		RefreshTitle();

		// Note: the preview cache (Code/_sui_preview/...) is NO LONGER
		// regenerated on every doc change. Doing so triggered a full S&Box
		// hot-reload after every palette add / hierarchy move because the
		// engine compiles every .razor under Code/. The cache now refreshes
		// only when the user activates the Preview tab or clicks Refresh
		// inside it (see SuiCenterTabsWidget tab change + Preview button).

		// Sweep any legacy `sui-generated-backups/` inside Code/. Older
		// builds of the writer placed backups there; the engine compiles
		// every .razor under Code/ so each backup turned into a duplicate
		// `partial class <Name>` declaration → CS0111. New backups land in
		// `<projectRoot>/.sui-backups/`; legacy folders are deleted here.
		AutoCleanLegacyBackupsInsideCode();
	}

	/// <summary>
	/// Silent auto-cleanup of legacy backup folders. Logs only when something
	/// is actually deleted (no spam on every doc load).
	/// </summary>
	private void AutoCleanLegacyBackupsInsideCode()
	{
		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) ) return;
		var codeRoot = System.IO.Path.Combine( projectRoot, "Code" );
		if ( !System.IO.Directory.Exists( codeRoot ) ) return;

		int deleted = 0;
		try
		{
			foreach ( var dir in System.IO.Directory.EnumerateDirectories(
				codeRoot, "sui-generated-backups", System.IO.SearchOption.AllDirectories ) )
			{
				try
				{
					System.IO.Directory.Delete( dir, recursive: true );
					deleted++;
				}
				catch ( System.Exception ex )
				{
					Log.Warning( $"[Sui] auto-clean: failed to delete '{dir}': {ex.Message}" );
				}
			}
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Sui] auto-clean walk failed: {ex.Message}" );
		}

		if ( deleted > 0 )
			Log.Info( $"[Sui] auto-cleaned {deleted} legacy backup folder(s) under Code/. New backups now live at <projectRoot>/.sui-backups/." );
	}

	/// <summary>
	/// Always write the preview cache (Code/_sui_preview/...) — used when the
	/// user activates the Preview tab or clicks Refresh inside it. This DOES
	/// trigger an engine hot-reload, which is fine here because it's tied to
	/// an explicit user action, not to every document edit.
	/// </summary>
	private void RegeneratePreviewCacheNow()
	{
		if ( Document == null ) return;
		try
		{
			var r = SuiPreviewCacheWriter.Write( Document );
			if ( r.Ok && (r.RazorChanged || r.ScssChanged) )
				Log.Info( $"[Sui] preview cache refreshed → {r.TypeFullName}" );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Sui] failed to refresh preview cache: {ex.Message}" );
		}
	}

	private void RefreshStalePreviewCacheIfPresent()
	{
		if ( Document == null ) return;

		var rawClass = !string.IsNullOrEmpty( Document.Output?.ClassName )
			? Document.Output.ClassName
			: Document.Name;
		var className = SuiDocumentValidator.SanitizeClassName( rawClass ?? "" );
		if ( string.IsNullOrEmpty( className ) ) return;

		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) ) return;

		var cacheRazor = System.IO.Path.Combine(
			projectRoot, "Code", "_sui_preview", className, $"{className}.razor" );
		if ( !System.IO.File.Exists( cacheRazor ) ) return;

		// Cache exists from a previous Preview session. Re-emit so it reflects
		// the current generator (e.g. picks up the .SuiPreview namespace).
		try
		{
			var r = SuiPreviewCacheWriter.Write( Document );
			if ( r.Ok && (r.RazorChanged || r.ScssChanged) )
				Log.Info( $"[Sui] preview cache refreshed → {r.TypeFullName}" );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Sui] failed to refresh preview cache: {ex.Message}" );
		}
	}

	private void OnControllerSelectionChanged()
	{
		// Push both the primary (for inline rename, scroll-to, etc.) AND the
		// full set (for highlighting + multi-select-aware UI).
		_hierarchy?.SetSelected( _controller.Selected );
		_hierarchy?.SetSelectedSet( _controller.SelectedSet );
		_hierarchy?.Refresh();

		_details?.SetSelectedSet( _controller.Selected, _controller.SelectedCount );
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

		// Guard: if the load failed (suspicious-size file produced empty doc),
		// never write back — the in-memory doc is a placeholder, writing it
		// would replace the user's actual data with an empty document.
		if ( _loadLikelyFailed )
		{
			Log.Warning( "[Sui] save BLOCKED — original file failed to parse and the in-memory document is a placeholder. Fix the .sui JSON on disk before saving." );
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
		// Save() itself checks _loadLikelyFailed; double-guard here to keep the
		// intent visible at the call site.
		if ( !_loadLikelyFailed )
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
		MenuBar.Clear();

		// Tear down old custom layout so it gets rebuilt fresh.
		if ( _rootContainer.IsValid() )
		{
			try { _rootContainer.Destroy(); } catch { }
			_rootContainer = null;
		}

		BuildMenuBar();
		BuildToolBar();
		BuildDocks();

		// Re-fan the controller's current state out to the freshly-rebuilt widgets.
		OnControllerDocumentChanged();
	}

	protected override void RestoreDefaultDockLayout()
	{
		// 100% custom layout — nothing to restore from a dock cookie.
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
		edit.AddOption( "Cut", "content_cut", () => _controller.CutElement() );
		edit.AddOption( "Copy", "content_copy", () => _controller.CopyElement() );
		edit.AddOption( "Paste", "content_paste", () => _controller.PasteElement() );
		edit.AddOption( "Duplicate", "content_copy", () => _controller.DuplicateElement() );
		edit.AddOption( "Delete", "delete", () => _controller.DeleteElement() );
		edit.AddOption( "Rename", "edit", () => _hierarchy?.BeginRenameSelected() );
		edit.AddSeparator();
		var align = edit.AddMenu( "Align" );
		align.AddOption( "Left",         "align_horizontal_left",   () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.Left ) );
		align.AddOption( "Center (H)",   "align_horizontal_center", () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.HCenter ) );
		align.AddOption( "Right",        "align_horizontal_right",  () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.Right ) );
		align.AddSeparator();
		align.AddOption( "Top",          "align_vertical_top",      () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.Top ) );
		align.AddOption( "Center (V)",   "align_vertical_center",   () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.VCenter ) );
		align.AddOption( "Bottom",       "align_vertical_bottom",   () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.Bottom ) );
		align.AddSeparator();
		align.AddOption( "Distribute Horizontally", "horizontal_distribute", () => _controller?.DistributeSelection( SuiDistributeElementsCommand.Axis.Horizontal ) );
		align.AddOption( "Distribute Vertically",   "vertical_distribute",   () => _controller?.DistributeSelection( SuiDistributeElementsCommand.Axis.Vertical ) );

		var view = MenuBar.AddMenu( "View" );
		view.AddOption( "Zoom In", "zoom_in", ZoomIn );
		view.AddOption( "Zoom Out", "zoom_out", ZoomOut );
		view.AddOption( "Fit to Screen", "fit_screen", FitToScreen );

		var tools = MenuBar.AddMenu( "Tools" );
		tools.AddOption( "Validate Document", "rule", ValidateDocument );
		tools.AddOption( "Regenerate Preview", "refresh", RegeneratePreview );
		tools.AddOption( "Clean Preview Cache", "delete_sweep", CleanPreviewCache );
		tools.AddOption( "Clean All SUI Caches (preview + backups)", "broom", CleanLegacyBackups );
		tools.AddSeparator();
		tools.AddOption( "Install Sample Documents", "library_books", InstallSamples );

		var help = MenuBar.AddMenu( "Help" );
		help.AddOption( "Open PRD", "menu_book", () => { } );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Toolbar
	// ─────────────────────────────────────────────────────────────────────

	private SuiTopBar _topBar;
	private SuiTopBarButton _gridBtn;
	private Widget _rootContainer;

	private void BuildToolBar()
	{
		// 100% custom layout — NO DockManager, NO Editor.ToolBar.
		// Root is a Column container holding:
		//   [SuiTopBar]
		//   [Body (Row): leftSidebar | center | rightSidebar]
		//   [BottomPanel]
		// Built by BuildDocks() — this method just creates the top bar.

		if ( _topBar.IsValid() )
		{
			try { _topBar.Destroy(); } catch { }
			_topBar = null;
		}

		_topBar = new SuiTopBar();

		// Group 1: file ops
		_topBar.AddButton( "Save", "save", Save, tooltip: "Save (Ctrl+S)" );
		_topBar.AddButton( "Compile", "auto_fix_high", Compile, tooltip: "Compile (Ctrl+B)" );
		_topBar.AddSeparator();

		// Group 2: preview
		_topBar.AddButton( "Test in Play", "play_arrow", LaunchTestInPlay, hasChevron: false, tooltip: "Compile current .sui, load the preview stage scene (player + ground + light), and enter Play mode with the UI mounted on a ScreenPanel." );
		_topBar.AddSeparator();

		// Group 3: history
		_topBar.AddButton( "Undo", "undo", Undo, tooltip: "Undo (Ctrl+Z)" );
		_topBar.AddButton( "Redo", "redo", Redo, tooltip: "Redo (Ctrl+Y)" );
		_topBar.AddSeparator();

		// Group 4: grid toggle
		// (Snap / Zoom / Screen Size moved to the canvas mini-toolbar — they're
		// canvas-scoped controls and felt redundant in the global top bar.)
		_gridBtn = _topBar.AddButton( "Grid", "apps", ToggleGrid, tooltip: "Toggle grid overlay" );

		_topBar.AddStretch();
		_topBar.AddButton( "Settings", "settings", () => Log.Info( "[Sui] settings dialog — V2" ), hasChevron: true, tooltip: "Editor settings" );

		RefreshGridButton();
	}

	private string SnapValueFor( SuiDocumentSettings s )
	{
		if ( s == null || !s.SnapToGrid ) return "off";
		return $"{s.GridSize}px";
	}

	private string ZoomValueFor()
	{
		var vp = _canvas?.GetViewport();
		var pct = vp != null ? (int)System.Math.Round( vp.Zoom * 100 ) : 100;
		return $"{pct}%";
	}

	private string ScreenValueFor()
	{
		var c = Document?.Canvas;
		if ( c == null ) return "1920 x 1080";
		var w = c.PreviewWidth > 0 ? c.PreviewWidth : c.BaseWidth;
		var h = c.PreviewHeight > 0 ? c.PreviewHeight : c.BaseHeight;
		return $"{w} x {h}";
	}

	private void RefreshGridButton()
	{
		if ( _gridBtn == null ) return;
		_gridBtn.IsActive = Document?.Settings?.ShowGrid == true;
		_gridBtn.Update();
	}

	private void RefreshToolbarLabels()
	{
		// The dropdowns now live inside the canvas mini-toolbar (owned by
		// SuiCenterTabsWidget). Pull them via getters and refresh.
		if ( _centerTabs?.SnapDropdown != null )   _centerTabs.SnapDropdown.Value   = SnapValueFor( Document?.Settings );
		if ( _centerTabs?.ZoomDropdown != null )   _centerTabs.ZoomDropdown.Value   = ZoomValueFor();
		if ( _centerTabs?.ScreenDropdown != null ) _centerTabs.ScreenDropdown.Value = ScreenValueFor();
		RefreshGridButton();
	}

	private void OpenSnapMenuAt( Widget anchor )
	{
		if ( Document?.Settings == null ) return;
		var menu = new Menu( anchor );
		menu.AddOption( "Off", "close", () =>
		{
			Document.Settings.SnapToGrid = false;
			_canvas?.GetViewport()?.Invalidate();
			RefreshToolbarLabels();
		} );
		menu.AddSeparator();
		void Add( int px ) => menu.AddOption( $"{px} px", "grid_4x4", () =>
		{
			Document.Settings.SnapToGrid = true;
			Document.Settings.GridSize = px;
			_canvas?.GetViewport()?.Invalidate();
			RefreshToolbarLabels();
		} );
		Add( 4 ); Add( 8 ); Add( 16 ); Add( 32 ); Add( 64 );
		menu.OpenAtCursor( true );
	}

	private void OpenZoomMenuAt( Widget anchor )
	{
		var vp = _canvas?.GetViewport();
		if ( vp == null ) return;
		var menu = new Menu( anchor );
		void Add( int pct ) => menu.AddOption( $"{pct}%", "zoom_in", () =>
		{
			vp.SetZoom( pct / 100f );
			RefreshToolbarLabels();
		} );
		Add( 25 ); Add( 50 ); Add( 75 ); Add( 100 ); Add( 150 ); Add( 200 ); Add( 400 );
		menu.AddSeparator();
		menu.AddOption( "Fit", "fit_screen", () => { vp.FitCanvas(); RefreshToolbarLabels(); } );
		menu.OpenAtCursor( true );
	}

	private void OpenScreenSizeMenuAt( Widget anchor )
	{
		if ( Document?.Canvas == null ) return;
		var menu = new Menu( anchor );
		void Add( string label, int w, int h ) => menu.AddOption( label, "aspect_ratio", () =>
		{
			Document.Canvas.PreviewWidth = w;
			Document.Canvas.PreviewHeight = h;
			_canvas?.GetViewport()?.Invalidate();
			RefreshToolbarLabels();
		} );
		Add( "1920 × 1080 (FHD)", 1920, 1080 );
		Add( "1280 × 720 (HD)", 1280, 720 );
		Add( "2560 × 1440 (QHD)", 2560, 1440 );
		Add( "3840 × 2160 (4K)", 3840, 2160 );
		Add( "2560 × 1080 (Ultrawide)", 2560, 1080 );
		menu.AddSeparator();
		menu.AddOption( "Reset", "restart_alt", () =>
		{
			Document.Canvas.PreviewWidth = 0;
			Document.Canvas.PreviewHeight = 0;
			_canvas?.GetViewport()?.Invalidate();
			RefreshToolbarLabels();
		} );
		menu.OpenAtCursor( true );
	}

	private void ToggleGrid()
	{
		if ( Document?.Settings == null ) return;
		Document.Settings.ShowGrid = !Document.Settings.ShowGrid;
		_canvas?.GetViewport()?.Invalidate();
		RefreshGridButton();
	}

	private void ToggleRulers()
	{
		if ( Document?.Settings == null ) return;
		Document.Settings.ShowRulers = !Document.Settings.ShowRulers;
		_canvas?.GetViewport()?.Invalidate();
		_centerTabs?.RefreshToggleStates();
	}

	private void ToggleResponsiveDebug()
	{
		if ( Document?.Settings == null ) return;
		Document.Settings.ResponsiveDebug = !Document.Settings.ResponsiveDebug;
		_canvas?.GetViewport()?.Invalidate();
		_centerTabs?.RefreshToggleStates();
	}

	private void ToggleAnchors()
	{
		if ( Document?.Settings == null ) return;
		Document.Settings.ShowAnchors = !Document.Settings.ShowAnchors;
		_canvas?.GetViewport()?.Invalidate();
		_centerTabs?.RefreshToggleStates();
	}

	private void ToggleLayoutBounds()
	{
		if ( Document?.Settings == null ) return;
		Document.Settings.ShowLayoutBounds = !Document.Settings.ShowLayoutBounds;
		_canvas?.GetViewport()?.Invalidate();
		_centerTabs?.RefreshToggleStates();
	}

	// Mini-toolbar Align/Distribute dropdowns — both anchor under the button
	// that triggered them so the menu opens visually attached to the toolbar.
	private void OpenAlignMenuAt( Widget anchor )
	{
		var menu = new Menu( anchor );
		menu.AddOption( "Left",       "align_horizontal_left",   () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.Left ) );
		menu.AddOption( "Center (H)", "align_horizontal_center", () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.HCenter ) );
		menu.AddOption( "Right",      "align_horizontal_right",  () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.Right ) );
		menu.AddSeparator();
		menu.AddOption( "Top",        "align_vertical_top",      () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.Top ) );
		menu.AddOption( "Center (V)", "align_vertical_center",   () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.VCenter ) );
		menu.AddOption( "Bottom",     "align_vertical_bottom",   () => _controller?.AlignSelection( SuiAlignElementsCommand.Mode.Bottom ) );
		menu.OpenAtCursor();
	}

	private void OpenDistributeMenuAt( Widget anchor )
	{
		var menu = new Menu( anchor );
		menu.AddOption( "Horizontally", "horizontal_distribute", () => _controller?.DistributeSelection( SuiDistributeElementsCommand.Axis.Horizontal ) );
		menu.AddOption( "Vertically",   "vertical_distribute",   () => _controller?.DistributeSelection( SuiDistributeElementsCommand.Axis.Vertical ) );
		menu.OpenAtCursor();
	}

	private void OpenSnapMenuFromToolbar()
	{
		if ( Document?.Settings == null ) return;
		var menu = new Menu( this );
		menu.AddOption( "Off", "close", () => { Document.Settings.SnapToGrid = false; _canvas?.GetViewport()?.Invalidate(); } );
		menu.AddSeparator();
		void Add( int px ) => menu.AddOption( $"{px} px", "grid_4x4", () =>
		{
			Document.Settings.SnapToGrid = true;
			Document.Settings.GridSize = px;
			_canvas?.GetViewport()?.Invalidate();
		} );
		Add( 4 ); Add( 8 ); Add( 16 ); Add( 32 ); Add( 64 );
		menu.OpenAtCursor( true );
	}

	private void OpenZoomMenuFromToolbar()
	{
		var vp = _canvas?.GetViewport();
		if ( vp == null ) return;
		var menu = new Menu( this );
		void Add( int pct ) => menu.AddOption( $"{pct}%", "zoom_in", () => vp.SetZoom( pct / 100f ) );
		Add( 25 ); Add( 50 ); Add( 75 ); Add( 100 ); Add( 150 ); Add( 200 ); Add( 400 );
		menu.AddSeparator();
		menu.AddOption( "Fit", "fit_screen", () => vp.FitCanvas() );
		menu.OpenAtCursor( true );
	}

	private void OpenScreenSizeMenuFromToolbar()
	{
		if ( Document?.Canvas == null ) return;
		var menu = new Menu( this );
		void Add( string label, int w, int h ) => menu.AddOption( label, "aspect_ratio", () =>
		{
			Document.Canvas.PreviewWidth = w;
			Document.Canvas.PreviewHeight = h;
			_canvas?.GetViewport()?.Invalidate();
		} );
		Add( "1920 × 1080 (FHD)", 1920, 1080 );
		Add( "1280 × 720 (HD)", 1280, 720 );
		Add( "2560 × 1440 (QHD)", 2560, 1440 );
		Add( "3840 × 2160 (4K)", 3840, 2160 );
		Add( "2560 × 1080 (Ultrawide)", 2560, 1080 );
		menu.AddSeparator();
		menu.AddOption( "Reset", "restart_alt", () =>
		{
			Document.Canvas.PreviewWidth = 0;
			Document.Canvas.PreviewHeight = 0;
			_canvas?.GetViewport()?.Invalidate();
		} );
		menu.OpenAtCursor( true );
	}

	private void OpenInventoryGridWizard()
	{
		if ( Document == null ) return;
		var wizard = new SuiInventoryGridWizard();
		wizard.OnCreate = cfg =>
		{
			// Build the grid + populate with empty slots.
			var grid = _controller.AddElement( SuiElementType.InventoryGrid );
			if ( grid == null ) return;
			grid.Layout.Width = cfg.Columns * cfg.SlotSize + (cfg.Columns - 1) * cfg.Gap;
			grid.Layout.Height = cfg.Rows * cfg.SlotSize + (cfg.Rows - 1) * cfg.Gap;
			grid.Layout.Anchor = cfg.Anchor;
			grid.Layout.PivotX = cfg.Anchor.ToString().Contains( "Center" ) ? 0.5f
				: cfg.Anchor.ToString().Contains( "Right" ) ? 1f : 0f;
			grid.Layout.PivotY = cfg.Anchor.ToString().StartsWith( "Middle" ) ? 0.5f
				: cfg.Anchor.ToString().StartsWith( "Bottom" ) ? 1f : 0f;
			grid.Layout.Gap = cfg.Gap;
			grid.Props.Columns = cfg.Columns;
			grid.Props.Rows = cfg.Rows;
			grid.Props.CellWidth = cfg.SlotSize;
			grid.Props.CellHeight = cfg.SlotSize;
			grid.Props.GridGap = cfg.Gap;

			for ( int i = 0; i < cfg.Columns * cfg.Rows; i++ )
			{
				var slot = _controller.AddElement( SuiElementType.InventorySlot, grid );
				if ( slot == null ) continue;
				slot.Layout.Width = cfg.SlotSize;
				slot.Layout.Height = cfg.SlotSize;
				slot.Props.SlotIndex = i;
			}
		};
	}

	private void LaunchTestInPlay()
	{
		if ( Document == null )
		{
			Log.Warning( "[Sui] cannot launch Test in Play — no document loaded" );
			return;
		}
		SuiPreviewLauncher.Launch( Document );
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Docks
	// ─────────────────────────────────────────────────────────────────────

	private void BuildDocks()
	{
		// 100% custom layout — NO DockManager. Trade-off: panels are fixed,
		// no drag/move, no saved layouts. User explicitly chose this path.
		//
		//   ┌─────────────────────────────────────────────────────┐
		//   │ SuiTopBar                                           │
		//   ├──────────┬──────────────────────────────┬──────────┤
		//   │ Palette  │                              │          │
		//   ├──────────┤    SuiCenterTabsWidget       │ Details  │
		//   │ Hierarchy│   (tabs + mini-toolbar +     │          │
		//   │          │    canvas/preview/code)      │          │
		//   ├──────────┴──────────────────────────────┴──────────┤
		//   │ SuiBottomTabsWidget (Animations/Bindings/...)       │
		//   └─────────────────────────────────────────────────────┘

		_palette = new SuiPaletteWidget();
		_hierarchy = new SuiHierarchyWidget();
		_centerTabs = new SuiCenterTabsWidget();
		_details = new SuiDetailsWidget();
		_bottomTabs = new SuiBottomTabsWidget();

		_details.SetController( _controller );
		_centerTabs.SetController( _controller );

		// When the user activates the Preview tab, regenerate the cache once
		// (lazy — avoids the engine hotload loop that used to fire on every
		// property edit; see comment in OnControllerDocumentChanged).
		_centerTabs.PreviewTabActivated += RegeneratePreviewCacheNow;

		// Wire controller events.
		_hierarchy.ElementSelected += el => _controller.SetSelected( el );
		_palette.ElementRequested += type =>
		{
			if ( type == SuiElementType.InventoryGrid )
			{
				OpenInventoryGridWizard();
				return;
			}
			_controller.AddElement( type );
		};

		_hierarchy.AddChildRequested += ( parent, type ) => _controller.AddElement( type, parent );
		_hierarchy.RenameRequested += ( el, newName ) =>
		{
			if ( newName == null )
			{
				_controller.SetSelected( el );
				_hierarchy.BeginRenameSelected();
			}
			else
			{
				_controller.RenameElement( el, newName );
			}
		};
		_hierarchy.DeleteRequested += el => _controller.DeleteElement( el );
		_hierarchy.DuplicateRequested += el => _controller.DuplicateElement( el );
		_hierarchy.MoveUpRequested += el => _controller.MoveElementUp( el );
		_hierarchy.MoveDownRequested += el => _controller.MoveElementDown( el );
		_hierarchy.ReparentRequested += ( child, newParent, idx ) => _controller.ReparentElement( child, newParent, idx );
		_hierarchy.FlagsChanged += () => _canvas?.GetViewport()?.Invalidate();

		// Wire mini-toolbar (dropdowns + view-toggle buttons + fit) inside the center widget.
		_centerTabs.WireMiniToolbar(
			ScreenValueFor(), OpenScreenSizeMenuAt,
			ZoomValueFor(),   OpenZoomMenuAt,
			SnapValueFor( Document?.Settings ), OpenSnapMenuAt,
			ToggleRulers,
			ToggleResponsiveDebug,
			ToggleAnchors,
			ToggleLayoutBounds,
			OpenAlignMenuAt,
			OpenDistributeMenuAt,
			FitToScreen );
		_centerTabs.RefreshToggleStates();

		// ── Custom layout ────────────────────────────────────────────────
		// Root background — #111111 (rgba 17,17,17). This is the color that
		// shows through any gap between docked widgets (top-bar bottom edge,
		// sidebar margins, etc).
		_rootContainer = new Widget( this );
		_rootContainer.SetStyles( "background-color: rgb(17,17,17);" );
		_rootContainer.Layout = Layout.Column();
		_rootContainer.Layout.Margin = 0;
		_rootContainer.Layout.Spacing = 0;

		// Top bar.
		_rootContainer.Layout.Add( _topBar );

		// Body row: leftSidebar | center | rightSidebar.
		// 4px margin on top + left + right = breathing room around the body
		// so the root #111111 background shows through as thin gutters.
		var body = new Widget( _rootContainer );
		body.SetStyles( "background-color: rgb(17,17,17);" );
		body.Layout = Layout.Row();
		body.Layout.Margin = new Sandbox.UI.Margin( 4, 4, 4, 0 );
		body.Layout.Spacing = 0;

		// Wrap each side panel widget in a SuiDockPanel for the docked-look
		// chrome (header bar + body + border). Pure cosmetic; no real
		// DockManager — this is the "imitação de docker" layer.
		var paletteDock = new SuiDockPanel( "Palette", "category", _palette );
		var hierarchyDock = new SuiDockPanel( "Hierarchy", "account_tree", _hierarchy );
		var detailsDock = new SuiDockPanel( "Details", "tune", _details );

		var leftSidebar = new Widget( body );
		leftSidebar.SetStyles( "background-color: transparent;" );
		leftSidebar.Layout = Layout.Column();
		leftSidebar.Layout.Margin = 0;
		leftSidebar.Layout.Spacing = 4; // 4px gap between Palette and Hierarchy
		leftSidebar.FixedWidth = 280;
		leftSidebar.Layout.Add( paletteDock, 1 );
		leftSidebar.Layout.Add( hierarchyDock, 1 );
		body.Layout.Add( leftSidebar );

		// 4px gap between left sidebar and center.
		var leftGap = new Widget( body );
		leftGap.SetStyles( "background-color: transparent;" );
		leftGap.FixedWidth = 4;
		body.Layout.Add( leftGap );

		// Center column = canvas tabs + bottom panel stacked. The bottom
		// panel sits ONLY under the canvas — sidebars (Palette/Hierarchy on
		// the left, Details on the right) extend full height to the bottom
		// of the window, per user spec.
		var centerColumn = new Widget( body );
		centerColumn.SetStyles( "background-color: transparent; border: none;" );
		centerColumn.Layout = Layout.Column();
		centerColumn.Layout.Margin = 0;
		centerColumn.Layout.Spacing = 4; // 4px gap between canvas and bottom panel
		centerColumn.Layout.Add( _centerTabs, 1 );

		_bottomTabs.FixedHeight = 220;
		centerColumn.Layout.Add( _bottomTabs );

		body.Layout.Add( centerColumn, 1 );

		// 4px gap between center and right sidebar.
		var rightGap = new Widget( body );
		rightGap.SetStyles( "background-color: transparent;" );
		rightGap.FixedWidth = 4;
		body.Layout.Add( rightGap );

		detailsDock.MinimumSize = new Vector2( 320, 0 );
		detailsDock.MaximumSize = new Vector2( 320, int.MaxValue );
		body.Layout.Add( detailsDock );

		_rootContainer.Layout.Add( body, 1 );

		Canvas = _rootContainer;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Stub commands (real implementations land in later milestones)
	// ─────────────────────────────────────────────────────────────────────

	private bool _compileRunning;

	private void Compile()
	{
		if ( Document == null )
		{
			Log.Warning( "[Sui] cannot compile — no document loaded" );
			return;
		}
		if ( _compileRunning )
		{
			Log.Info( "[Sui] compile already in progress" );
			return;
		}

		// First-compile UX: prompt for output folder if not configured.
		if ( !Document.Output.Configured || string.IsNullOrEmpty( Document.Output.RootFolder ) )
		{
			if ( !PromptOutputFolder() )
			{
				Log.Info( "[Sui] compile cancelled — no output folder selected" );
				return;
			}
		}

		_compileRunning = true;
		try
		{
			// Sweep any legacy backup folders under Code/ before compiling so a
			// stale `sui-generated-backups/.../UiTest2.razor` doesn't get pulled
			// into the build alongside the freshly-written final output and
			// trigger CS0111 (duplicate partial class declarations).
			AutoCleanLegacyBackupsInsideCode();

			// Save first so the on-disk .sui matches what the generator sees.
			Save();

			var outputFolderAbs = ResolveOutputFolderAbsolute( Document.Output.RootFolder );

			var ctx = new SuiGenerationContext
			{
				Document = Document,
				Mode = SuiGenerationMode.Final,
				// Don't prefix file.Path with the output folder — the writer
				// joins paths against outputFolderAbs itself, so doubling
				// the prefix here yields `<output>/<output>/file.razor` on
				// disk (CS0111 nightmare).
				OutputFolder = "",
				ClassName = Document.Output.ClassName,
				Namespace = Document.Output.Namespace,
			};

			var generation = SuiGenerationPipeline.Run( ctx );
			var compile = SuiCompileWriter.Run( generation, Document, outputFolderAbs );

			_bottomTabs?.DisplayCompileResult( generation, compile );
			SetCompileBanner( compile );

			if ( compile.Ok )
			{
				Log.Info( $"[Sui] Compile OK — Generated {compile.Generated.Count}, Skipped {compile.Skipped.Count}, Preserved {compile.Preserved.Count}, Obsolete {compile.Obsolete.Count}. Folder: {compile.OutputFolder}" );
				if ( compile.BackupFolder != null )
					Log.Info( $"[Sui] backup of overwritten files: {compile.BackupFolder}" );
			}
			else
			{
				foreach ( var e in compile.Errors ) Log.Error( $"[Sui] {e}" );
				foreach ( var c in compile.Conflicts )
					Log.Warning( $"[Sui] conflict: {c.RelativePath} — {c.ConflictReason}" );
			}
		}
		finally
		{
			_compileRunning = false;
		}
	}

	/// <summary>
	/// Resolve a stored project-relative folder to an absolute on-disk path.
	/// If the stored value already looks absolute, use it as-is.
	/// </summary>
	private static string ResolveOutputFolderAbsolute( string storedFolder )
	{
		if ( string.IsNullOrEmpty( storedFolder ) ) return null;
		if ( System.IO.Path.IsPathRooted( storedFolder ) ) return storedFolder;
		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) ) return storedFolder;
		return System.IO.Path.GetFullPath( System.IO.Path.Combine( projectRoot, storedFolder ) );
	}

	/// <summary>
	/// Open a folder browser for the user to pick the output folder. Stores
	/// the result on the document (project-relative if inside the project root,
	/// absolute otherwise). Returns true on selection, false on cancel.
	/// </summary>
	private bool PromptOutputFolder()
	{
		if ( Document == null ) return false;

		var fd = new FileDialog( null )
		{
			Title = "Choose output folder for generated UI files",
		};
		fd.SetFindDirectory();

		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		var existingAbs = ResolveOutputFolderAbsolute( Document.Output.RootFolder );
		if ( !string.IsNullOrEmpty( existingAbs ) && System.IO.Directory.Exists( existingAbs ) )
			fd.Directory = existingAbs;
		else if ( !string.IsNullOrEmpty( projectRoot ) )
			fd.Directory = System.IO.Path.Combine( projectRoot, "Code" );

		if ( !fd.Execute() ) return false;

		var picked = fd.SelectedFile;
		if ( string.IsNullOrEmpty( picked ) ) return false;

		// Store project-relative when possible; falls back to absolute.
		string stored = picked;
		if ( !string.IsNullOrEmpty( projectRoot ) )
		{
			var rootFull = System.IO.Path.GetFullPath( projectRoot );
			var pickedFull = System.IO.Path.GetFullPath( picked );
			if ( pickedFull.StartsWith( rootFull, System.StringComparison.OrdinalIgnoreCase ) )
			{
				stored = pickedFull.Substring( rootFull.Length ).TrimStart( '/', '\\' ).Replace( '\\', '/' );
			}
		}

		Document.Output.RootFolder = stored;
		Document.Output.Configured = true;
		// Default ClassName/Namespace if user hasn't set them yet.
		if ( string.IsNullOrEmpty( Document.Output.ClassName ) )
			Document.Output.ClassName = SuiDocumentValidator.SanitizeClassName( Document.Name ?? "GeneratedUi" );
		if ( string.IsNullOrEmpty( Document.Output.Namespace ) )
			Document.Output.Namespace = "Game.UI";

		_controller.MarkDirtyExternally();
		Log.Info( $"[Sui] output folder set to '{stored}' (abs: {picked})" );
		return true;
	}

	private void ChangeOutputFolder()
	{
		if ( Document == null )
		{
			Log.Warning( "[Sui] cannot change output — no document loaded" );
			return;
		}
		PromptOutputFolder();
	}

	/// <summary>
	/// Push a banner over the canvas summarising the compile state. Cleared
	/// (banner = null) when compile is fully OK; populated with the most
	/// urgent issue otherwise.
	/// </summary>
	private void SetCompileBanner( SuiCompileResult compile )
	{
		var vp = _canvas?.GetViewport();
		if ( vp == null ) return;

		if ( compile == null || compile.Ok )
		{
			vp.ErrorBanner = null;
			vp.Update();
			return;
		}

		string title;
		string detail;
		if ( compile.Errors.Count > 0 )
		{
			title = $"Compile failed — {compile.Errors.Count} error(s)";
			detail = compile.Errors[0];
		}
		else if ( compile.Conflicts.Count > 0 )
		{
			title = $"Compile blocked — {compile.Conflicts.Count} conflict(s)";
			detail = $"{compile.Conflicts[0].RelativePath}: {compile.Conflicts[0].ConflictReason}";
		}
		else
		{
			title = "Compile produced warnings";
			detail = compile.Warnings.Count > 0 ? compile.Warnings[0] : "(no detail)";
		}

		vp.ErrorBanner = new SuiCanvasErrorBanner
		{
			Title = title,
			Detail = detail,
			OnClick = () => Log.Info( "[Sui] click on banner — see Compile Results dock for full report" ),
			OnDismiss = () => Log.Info( "[Sui] banner dismissed (errors still in Compile Results)" ),
		};
		vp.Update();
	}

	private void OpenGeneratedFolder()
	{
		if ( Document == null )
		{
			Log.Warning( "[Sui] no document loaded" );
			return;
		}
		var abs = ResolveOutputFolderAbsolute( Document.Output?.RootFolder );
		if ( string.IsNullOrEmpty( abs ) || !System.IO.Directory.Exists( abs ) )
		{
			Log.Warning( $"[Sui] output folder not set or doesn't exist: {abs}" );
			return;
		}
		try
		{
			System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo
			{
				FileName = abs,
				UseShellExecute = true,
				Verb = "open",
			} );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Sui] failed to open folder: {ex.Message}" );
		}
	}

	private void Undo() => _controller.Undo();
	private void Redo() => _controller.Redo();

	[Shortcut( "editor.undo", "Ctrl+Z", ShortcutType.Window )]
	private void OnShortcutUndo() => Undo();

	[Shortcut( "editor.redo", "Ctrl+Y", ShortcutType.Window )]
	private void OnShortcutRedo() => Redo();

	[Shortcut( "editor.delete", "DEL", ShortcutType.Window )]
	private void OnShortcutDelete() => _controller.DeleteElement();

	[Shortcut( "editor.rename", "F2", ShortcutType.Window )]
	private void OnShortcutRename() => _hierarchy?.BeginRenameSelected();

	[Shortcut( "editor.duplicate", "Ctrl+D", ShortcutType.Window )]
	private void OnShortcutDuplicate() => _controller.DuplicateElement();

	[Shortcut( "editor.cut", "Ctrl+X", ShortcutType.Window )]
	private void OnShortcutCut() => _controller.CutElement();

	[Shortcut( "editor.copy", "Ctrl+C", ShortcutType.Window )]
	private void OnShortcutCopy() => _controller.CopyElement();

	[Shortcut( "editor.paste", "Ctrl+V", ShortcutType.Window )]
	private void OnShortcutPaste() => _controller.PasteElement();

	[Shortcut( "editor.compile", "Ctrl+B", ShortcutType.Window )]
	private void OnShortcutCompile() => Compile();

	[Shortcut( "editor.zoomin", "Ctrl++", ShortcutType.Window )]
	private void OnShortcutZoomIn() => ZoomIn();

	[Shortcut( "editor.zoomout", "Ctrl+-", ShortcutType.Window )]
	private void OnShortcutZoomOut() => ZoomOut();

	[Shortcut( "editor.fittoscreen", "Ctrl+0", ShortcutType.Window )]
	private void OnShortcutFitToScreen() => FitToScreen();

	// ─────────────────────────────────────────────────────────────────────
	//  View / Tools handlers
	// ─────────────────────────────────────────────────────────────────────

	private void ZoomIn()
	{
		var vp = _canvas?.GetViewport();
		if ( vp == null ) return;
		vp.SetZoom( vp.Zoom * 1.25f );
	}

	private void ZoomOut()
	{
		var vp = _canvas?.GetViewport();
		if ( vp == null ) return;
		vp.SetZoom( vp.Zoom / 1.25f );
	}

	private void FitToScreen()
	{
		_canvas?.GetViewport()?.FitCanvas();
	}

	private void RegeneratePreview()
	{
		if ( Document == null )
		{
			Log.Warning( "[Sui] no document loaded" );
			return;
		}
		// Force a fresh write to the preview cache; modal preview re-opens load it.
		var className = Document.Output?.ClassName ?? Document.Name ?? "Generated";
		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( !string.IsNullOrEmpty( projectRoot ) )
		{
			var cacheFolder = System.IO.Path.Combine( projectRoot, "Code", "_sui_preview", className );
			try
			{
				if ( System.IO.Directory.Exists( cacheFolder ) )
				{
					System.IO.Directory.Delete( cacheFolder, recursive: true );
					Log.Info( $"[Sui] cleared preview cache for '{className}'" );
				}
			}
			catch ( System.Exception ex )
			{
				Log.Warning( $"[Sui] failed to clear preview cache: {ex.Message}" );
			}
		}
		// Re-write via SuiPreviewCacheWriter so the engine hot-loads it.
		var writeResult = SuiPreviewCacheWriter.Write( Document );
		if ( writeResult.Ok )
			Log.Info( $"[Sui] preview regenerated → {writeResult.TypeFullName}" );
		else
			foreach ( var e in writeResult.Errors ) Log.Error( $"[Sui] {e}" );
	}

	/// <summary>
	/// Install the canonical sample .sui documents into the project's
	/// Assets/SuiSamples/ folder (PRD doc 14 § Sample projects). Skips files
	/// that already exist so the user can re-run safely without losing edits.
	/// </summary>
	private void InstallSamples()
	{
		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) )
		{
			Log.Warning( "[Sui] cannot install samples — no project loaded" );
			return;
		}

		var dest = System.IO.Path.Combine( projectRoot, "Assets", "SuiSamples" );
		try { System.IO.Directory.CreateDirectory( dest ); }
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Sui] failed to create samples folder: {ex.Message}" );
			return;
		}

		int installed = 0, skipped = 0;
		foreach ( var (name, doc) in SuiSampleGenerator.All() )
		{
			var path = System.IO.Path.Combine( dest, $"{name}.sui" );
			if ( System.IO.File.Exists( path ) )
			{
				skipped++;
				continue;
			}

			var asset = AssetSystem.CreateResource( "sui", path );
			if ( asset == null )
			{
				Log.Warning( $"[Sui] failed to create sample asset: {path}" );
				continue;
			}

			// Hydrate the resource with the generated document.
			var resource = asset.LoadResource<SuiAsset>();
			if ( resource == null ) resource = new SuiAsset();
			resource.Document = doc;
			asset.SaveToDisk( resource );
			installed++;
		}

		Log.Info( $"[Sui] samples installed: {installed} new, {skipped} skipped (already existed). Folder: {dest}" );
	}

	/// <summary>
	/// Aggressive cleanup — wipes both the preview cache and all legacy
	/// `sui-generated-backups/` folders inside Code/. The engine stops
	/// seeing duplicate `partial class` declarations on next compile.
	///
	/// New backups created by SuiCompileWriter live OUTSIDE Code/ (in
	/// projectRoot/.sui-backups/) so they never reach the compiler;
	/// existing legacy ones are deleted by this tool.
	/// </summary>
	private void CleanLegacyBackups()
	{
		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) ) return;

		var codeRoot = System.IO.Path.Combine( projectRoot, "Code" );
		if ( !System.IO.Directory.Exists( codeRoot ) )
		{
			Log.Info( "[Sui] no Code/ folder, nothing to clean" );
			return;
		}

		int previewCacheDeleted = 0;
		int backupFoldersDeleted = 0;

		// 1) Wipe the entire preview cache root.
		var cacheRoot = System.IO.Path.Combine( codeRoot, "_sui_preview" );
		if ( System.IO.Directory.Exists( cacheRoot ) )
		{
			try
			{
				System.IO.Directory.Delete( cacheRoot, recursive: true );
				previewCacheDeleted = 1;
			}
			catch ( System.Exception ex )
			{
				Log.Warning( $"[Sui] failed to delete preview cache '{cacheRoot}': {ex.Message}" );
			}
		}

		// 2) Delete every `sui-generated-backups/` folder anywhere under Code/.
		try
		{
			foreach ( var dir in System.IO.Directory.EnumerateDirectories(
				codeRoot, "sui-generated-backups", System.IO.SearchOption.AllDirectories ) )
			{
				try
				{
					System.IO.Directory.Delete( dir, recursive: true );
					backupFoldersDeleted++;
				}
				catch ( System.Exception ex )
				{
					Log.Warning( $"[Sui] failed to delete '{dir}': {ex.Message}" );
				}
			}
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Sui] cleanup walk failed: {ex.Message}" );
		}

		Log.Info( $"[Sui] cleanup done: preview cache wiped ({previewCacheDeleted}), legacy backup folders deleted ({backupFoldersDeleted}). Compile again — preview cache rebuilds on next Preview click." );
	}

	private void CleanPreviewCache()
	{
		var projectRoot = Sandbox.Project.Current?.RootDirectory?.FullName;
		if ( string.IsNullOrEmpty( projectRoot ) ) return;
		var cacheRoot = System.IO.Path.Combine( projectRoot, "Code", "_sui_preview" );
		try
		{
			if ( System.IO.Directory.Exists( cacheRoot ) )
			{
				System.IO.Directory.Delete( cacheRoot, recursive: true );
				Log.Info( $"[Sui] cleaned all preview cache: {cacheRoot}" );
			}
			else
			{
				Log.Info( "[Sui] preview cache already empty" );
			}
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Sui] failed to clean preview cache: {ex.Message}" );
		}
	}

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
