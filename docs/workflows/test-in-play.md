---
layout: default
title: Test in Play workflow
parent: Workflows
nav_order: 1
---

# Test in Play workflow

A deeper dive on Test in Play — see [Getting Started · Test in Play]({% link getting-started/test-in-play.md %}) for the basic walkthrough.

## Full sequence

```
1. User clicks "Test in Play"
2. SuiPreviewLauncher.Launch( document )
3. SuiPreviewCacheWriter.Write( document )
     ├── Generate Razor + SCSS via SuiGenerationPipeline (Mode.Preview)
     ├── Write to Code/_sui_preview/<ClassName>/<ClassName>.razor
     └── Write to Code/_sui_preview/<ClassName>/<ClassName>.razor.scss
4. Engine hot-reloads → new PanelComponent type registered in TypeLibrary
5. Launcher polls TypeLibrary up to 6 seconds for the generated type
6. SuiPreviewState.PendingTypeFullName = "Game.UI.SuiPreview.<ClassName>"
7. AssetSystem.FindByPath("sui_preview/preview_stage.scene") → asset.OpenInEditor()
8. EditorScene.Play( SceneEditorSession.Active )
9. Scene loads — SuiPreviewMount's OnAwake fires:
     ├── Reads SuiPreviewState.PendingTypeFullName
     ├── TypeLibrary.GetType(fqn) → typeDesc
     ├── Creates child GameObject "ScreenPanelHost"
     ├── Adds ScreenPanel component
     ├── Components.Create(typeDesc) → instance of your generated PanelComponent
     └── SuiPreviewState.Clear()
10. Player walks around, UI overlays as ScreenPanel
```

## What lives where

| File | Purpose |
|---|---|
| `Code/Runtime/SuiPreviewState.cs` | Static handoff slot for the type FQN |
| `Code/Runtime/SuiPreviewMount.cs` | Component in the scene that does the mount on OnAwake |
| `Editor/SuiPreviewLauncher.cs` | Editor-side orchestrator (compile → poll → load scene → play) |
| `Editor/SuiPreviewCacheWriter.cs` | Writes the preview cache to `Code/_sui_preview/<ClassName>/` |
| `Assets/sui_preview/preview_stage.scene` | The bundled stage scene |

## Path conventions

- **Preview cache** lives at `<projectRoot>/Code/_sui_preview/<ClassName>/<ClassName>.razor` (+ `.scss`).
  - Inside `Code/` so the engine's Razor compiler picks them up automatically (must be in compilable path).
  - Sub-namespace `Game.UI.SuiPreview` avoids colliding with the final-compiled `Game.UI.<ClassName>` (different namespace, different type even if class name matches).
- **Final output** is wherever you picked in **File → Change Output Folder**. Typically `Code/UI/`.

## Preview-mode SCSS specifics

The preview cache writer uses `SuiGenerationMode.Preview`. Difference from `Final`:

- **Preview mode does NOT emit `@import "<ClassName>.User.scss"`** at the bottom of the SCSS. The sidecar file doesn't exist in the preview path, and `@import` of a missing file would silently break SCSS compilation in the runtime.
- **Final mode emits the @import** so user customizations cascade.

This means **Test in Play uses generator output only** — your `.User.scss` overrides are visible only in actual Compile + Play, not Test in Play.

## Limitations

1. **No scene auto-restore.** After Stop, you remain on `preview_stage.scene`. Open your original scene manually from Asset Browser if needed.
2. **One Play at a time.** s&box doesn't support nested play sessions.
3. **No window minimize.** SUI Designer stays where it was. To see the Scene Viewport that's playing, click its tab or rearrange your dock layout.
4. **Stack count badges missing.** [ISSUE-005](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md#issue-005) — `PreviewCount` on InventorySlot/ItemIcon paints in canvas but not in runtime yet.
5. **`<label>` rgba alpha quirk.** [ISSUE-004](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md#issue-004) — `<label>` elements may render `background-color` rgba as opaque. Workaround: wrap Text in a Panel.

## Troubleshooting

Open the engine console (`View → Console`) and look for `[SuiPreviewLauncher]` / `[SuiPreviewMount]` lines:

| Message | Meaning |
|---|---|
| `Compiled 'Game.UI.SuiPreview.MyHud'` | Step 3 OK |
| `Type loaded: Game.UI.SuiPreview.MyHud` | Hot-reload + TypeLibrary lookup OK |
| `Opened preview stage scene.` | Step 7 OK |
| `Mounted 'Game.UI.SuiPreview.MyHud' on ScreenPanel.` | Step 9 OK — UI should appear |
| `Preview stage not found at 'sui_preview/preview_stage.scene'.` | Library install broken — addon's Assets folder missing |
| `Timed out waiting for type … to compile` | Check console for SCSS/Razor compile errors |
| `Already in Play.` | Stop the current play session first |
| `[SuiPreviewMount] No PendingTypeFullName set` | Launcher didn't set state — usually means a partial trigger. Click Test in Play again |
