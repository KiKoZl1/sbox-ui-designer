---
layout: default
title: Test in Play
parent: Getting Started
nav_order: 3
---

# Test in Play
{: .no_toc }

A one-click workflow that loads a pre-baked s&box scene with a controllable TPS player and mounts your UI as a `ScreenPanel` overlay — so you can see your UI under **real Play-mode lifecycle**, not the simulated lifecycle of the embedded Preview tab.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## When to use it

- ✅ **Validate interactive states** — Hover / Pressed / Disabled / Focused visuals only fire under a real input loop. The canvas always renders Normal. (See [Interactive states]({% link concepts/interactive-states.md %}).)
- ✅ **Drive input widgets end-to-end** — see Slider drag / TextEntry edits / DropDown selection actually commit to your Variables under the right UpdateTrigger (OnChange / OnLostFocus / OnSubmit / OnRelease / Manual+Apply).
- ✅ **Test Doo event graphs** — visual scripts only execute under Play; the canvas can't run them.
- ✅ **Validate TwoWay bindings** — Variable round-trip from UI widget → wrapper field requires a live mount.
- ✅ **Final visual validation** before shipping a UI — does it scale at 1080p? Does the SuiReference subtree resolve correctly?
- ❌ **Quick layout tweaking** — for per-property iteration use the inline **Preview** tab; it skips the ~3s Play spin-up.

## How it works

1. SUI Designer compiles your `.sui` to a sandbox path: `<projectRoot>/Code/_sui_preview/<ClassName>/<ClassName>.razor` (+ `.scss`).
2. The engine hot-reloads — your `PanelComponent` type is now in `TypeLibrary`.
3. The launcher polls `TypeLibrary` for the new type (up to ~6s).
4. Once found, it sets a process-local hand-off `SuiPreviewState.PendingTypeFullName = "Game.UI.SuiPreview.<ClassName>"`.
5. It opens the bundled scene `Libraries/kikozl.sbox_ui_designer/Assets/sui_preview/preview_stage.scene`.
6. It calls `EditorScene.Play(SceneEditorSession.Active)` — the engine enters real Play mode.
7. The scene's **SUI Preview Mount** GameObject's `OnAwake` reads `PendingTypeFullName`, creates a `ScreenPanel` host as its child, and mounts your `PanelComponent` on it.
8. You see the player walking around, with your UI overlaid as `ScreenPanel`.

## Step by step

### 1. Click "Test in Play"

In the top toolbar, click the **▶ Test in Play** button.

You'll see logs in the engine console:

```
[SuiPreviewLauncher] Compiled 'Game.UI.SuiPreview.MyHud'
[SuiPreviewLauncher] Type loaded: Game.UI.SuiPreview.MyHud
[SuiPreviewLauncher] Opened preview stage scene.
[SuiPreviewMount] Mounted 'Game.UI.SuiPreview.MyHud' on ScreenPanel.
[SuiPreviewLauncher] EditorScene.Play() invoked. UI should mount in OnAwake of SuiPreviewMount.
```

### 2. The engine enters Play

The Scene Viewport switches from edit mode to play. A Citizen player spawns. Your UI overlays the player view.

WASD moves, mouse rotates the camera. You're in a real game session.

### 3. Stop play to return

Click the engine's **Stop** button. The scene viewport returns to edit mode on the preview stage scene.

To go back to your original scene, open it manually from the Asset Browser (the launcher does not auto-restore — see [workflow notes]({% link workflows/test-in-play.md %})).

## The preview stage scene

Bundled at `Libraries/kikozl.sbox_ui_designer/Assets/sui_preview/preview_stage.scene`. Contains:

- A 50×50 ground cube
- A directional light (sun)
- An ambient light
- A 2D skybox
- A Citizen `PlayerController` with all standard movement components
- An empty **SUI Preview Mount** GameObject — `SuiPreviewMount` component reads `SuiPreviewState` on `OnAwake` and mounts your UI

You can edit this scene if you want a different stage. Save it back to its original path — the launcher always opens this fixed file.

## When something goes wrong

If you click Test in Play and **nothing happens**:

- Open the engine console (`View → Console`). Look for `[SuiPreviewLauncher]` lines.
- `"Preview stage not found at 'sui_preview/preview_stage.scene'"` → library install broken; check [installation]({% link getting-started/install.md %}).
- `"Timed out waiting for type … to compile"` → check the console for SCSS or Razor compile errors. The generator emitted invalid code or the engine rejected the SCSS.
- `"Already in Play"` → there's an active play session; press Stop first.

If Play starts but the UI doesn't appear:

- Look for `[SuiPreviewMount] Mounted …` log. If missing, the `Mount` component never ran (scene didn't load correctly).
- If you see the log but no UI, the `PanelComponent` may have failed to construct — check console for runtime exceptions.
- Width / Height on the root may be 0 — open the `.sui`, check the Canvas section in Details.

## Known limitations

- **Switches scene context.** Your previous scene is closed (asks to save if dirty). When you Stop, you stay on `preview_stage.scene`.
- **One Play at a time.** s&box doesn't support multiple play sessions; if you're already in Play, the launcher refuses.
- **No window minimize / restore.** SUI Designer stays where you left it; you may need to bring the Scene Viewport tab to focus manually.

## Next

- [Editor tour]({% link user-guide/editor-tour.md %}) — every panel explained
- [Compile + output]({% link workflows/compile-and-output.md %}) — what gets written and where
- [Workflows · Test in Play]({% link workflows/test-in-play.md %}) — deeper dive on the workflow itself
