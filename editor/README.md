# Editor

s&box editor integration. Lives in `Editor.*` namespace. Builds the visual designer window and tools.

Files (planned):

- `SuiAssetEditor.cs` — `[EditorForAssetType("sui")]` entry point
- `SuiDesignerWindow.cs` — main `Editor.Window`
- `SuiDesignerController.cs` — document controller, selection, dirty state, command stack
- `Widgets/` — region widgets (palette, hierarchy, canvas, details, etc.)
- `Tools/` — designer commands (selection, move, resize, snap, zoom/pan)

Editor code can reference Runtime types but Runtime cannot reference Editor types. Generated output (Razor/SCSS) must contain zero `Editor.*` references.
