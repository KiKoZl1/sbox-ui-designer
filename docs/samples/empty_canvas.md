---
layout: default
title: empty_canvas
parent: Samples
nav_order: 7
permalink: /samples/empty_canvas/
---

# empty_canvas
{: .no_toc }

The smallest possible SUI document — one Canvas, one centered Panel, one Text element, and zero of everything else. This sample isolates the single V1.5 concept that every other sample takes for granted: **mounting a generated wrapper into the scene**. No Variables, no Bindings, no Events, no animations, no Code-mode handlers. If this doesn't render, nothing else in the showcase will, so it doubles as the smoke test for your SUI Designer install. Treat it as the "hello world" pass: prove the pipeline from `.sui` to compiled wrapper to live `RootPanel` works end-to-end, then graduate to a sample that actually exposes Variables.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What you'll see

A single line of bold white text reading **"Hello SUI!"** rendered at 48pt, anchored dead-center on a 1920x1080 canvas with `ScreenHeight1080` scaling. The text floats on whatever the underlying scene is drawing — there is no background, no border, no card chrome, no drop shadow. The host panel that wraps the text is 480x120 and fully transparent, so visually you only perceive the glyphs themselves.

Because the root canvas has `PointerEvents = None` and the controller mounts in `SuiInputMode.Passive`, the cursor passes straight through the HUD to the world beneath. You should be able to look around, fire weapons, or interact with anything in the scene without the panel intercepting input.

## Behavior

1. **Mount** — `EmptyCanvasController.OnStart` runs once when the component starts. It calls `Hud.Show( SuiInputMode.Passive )`, which instantiates the generated `EmptyCanvas` panel as a `RootPanel` child and registers it with the Sandbox UI tree.
2. **Render** — On the next UI tick, the `EmptyCanvas.razor` markup paints the transparent `CenterPanel` and the `HelloText` child. The literal Prop `Text = "Hello SUI!"` is baked into the generated markup; there is nothing dynamic to refresh.
3. **Idle** — With no `OnUpdate`, no bindings, no timers, and no events, the panel sits there. Each frame the engine repaints the same text without any controller code running.
4. **Teardown** — When the component is destroyed (scene unload, GameObject deletion, or hot-reload), `OnDestroy` calls `Hud.Hide()`, which removes the panel from the UI tree so no orphan `RootPanel` leaks into the next scene.

## How to use

1. Open `empty_canvas.sui` in the SUI Designer window (`Window -> Sbox UI Designer`) and hit Compile. This generates `EmptyCanvas.cs` and `EmptyCanvas.razor` / `.razor.scss` under `Code/Samples/EmptyCanvas/`.
2. Drop `EmptyCanvasController.cs` into `Code/Samples/EmptyCanvas/` (or anywhere under `Code/`).
3. Attach `EmptyCanvasController` to any GameObject in any scene and hit Play. The text appears immediately — no inspector fields to wire.

## Variables

_None._ This sample intentionally declares zero Variables; that is the whole pedagogical point. The first sample that introduces Variables is `counter_button`.

## Bindings

_None._ The `Text` value `"Hello SUI!"` is baked into the document as a literal Prop, not a binding. There is no live data flowing between controller and wrapper.

## Events

_None._ `SuiInputMode.Passive` means the panel does not receive mouse or keyboard input at all, so there is nothing for events to fire on. Events first appear in `counter_button` and `dialog_system`.

## Required `User.scss` rules

N/A — fully driven by the generated SCSS. The sample does not ship a `EmptyCanvasPanel.User.scss`; the look is determined entirely by the Props in the `.sui` document and the SCSS that the Designer emits from those Props.

## Controller architecture

- `[Property] EmptyCanvas Hud { get; set; } = new();` — auto-instantiates the generated wrapper so the inspector shows the field without requiring drag-and-drop assignment.
- `OnStart` — calls `Hud?.Show( SuiInputMode.Passive )` exactly once. Passive mode is chosen because the HUD is render-only and should not steal focus from the game.
- `OnDestroy` — calls `Hud?.Hide()` to remove the panel from the UI tree on teardown.
- No `OnUpdate`, no `OnFixedUpdate`, no `OnAwake`, no input handling, no `[Sync]` fields, no RPCs, no timers, no event handlers. The component file is roughly 20 lines of substance.

## File map

```text
Code/Samples/EmptyCanvas/
  EmptyCanvas.cs               (generated wrapper — do not edit)
  EmptyCanvasPanel.razor       (generated markup — do not edit)
  EmptyCanvasPanel.razor.scss  (generated styles — do not edit)
  EmptyCanvasController.cs     (you ship this — drives the wrapper)
```

This sample has no `User.scss` file; all styling comes from the generated SCSS driven by `.sui` Props.

## Element tree at a glance

```text
- Root (Canvas, 1920x1080, Anchor TopLeft, PointerEvents None)
  - CenterPanel (Panel, 480x120, Anchor MiddleCenter, Pivot 0.5/0.5, transparent)
    - HelloText (Text, 400x64, Anchor MiddleCenter, "Hello SUI!", 48pt Bold #ffffff)
```

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `EmptyCanvasController` won't compile — "type or namespace `EmptyCanvas` could not be found". | The Designer wrapper hasn't been generated yet, so the controller can't find the class it instantiates in the `[Property]`. | Open `empty_canvas.sui` in the Designer and hit Compile (or Force Regen). The `EmptyCanvas.cs` wrapper has to exist before the controller references it. |
| Text doesn't appear on screen when I hit Play. | Either the controller isn't attached to an enabled GameObject in the active scene, or `Show(...)` is being short-circuited because `Hud` was set to `null` in the inspector. | Confirm the GameObject is enabled and the `Hud` `[Property]` is non-null in the inspector. The default `= new()` should already populate it. |
| Text appears but is misaligned / clipped / off-center. | A custom `User.scss` from a different sample is leaking into this one, OR the canvas `ScaleMode` was changed away from `ScreenHeight1080`. | Delete any `EmptyCanvasPanel.User.scss` and revert `ScaleMode` to `ScreenHeight1080` in the `.sui` document. The anchor/pivot pair `MiddleCenter` + `0.5/0.5` only centers correctly when the parent canvas occupies the full screen. |
| Panel intercepts mouse clicks even though it's supposed to be passive. | Someone changed `SuiInputMode.Passive` to `MouseOnly` or `Full` in the controller. | Restore `Hud?.Show( SuiInputMode.Passive )`. Passive is the only mode that lets pointer events pass straight through to the scene. |
| After scene reload, a duplicate `RootPanel` lingers. | `OnDestroy` didn't fire — usually because the component was disabled rather than destroyed. | Make sure the GameObject is actually destroyed (or call `Hud.Hide()` manually from `OnDisabled`). Disabling alone won't trigger `OnDestroy`. |

## Extending it

- **Change the text color.** Open `empty_canvas.sui`, select `HelloText`, and set `Props.Color`. Force Regen, no code change required.
- **Make the text dynamic.** Add a `string` Variable named `Greeting`, bind `HelloText.Text` to it OneWay, and set `Hud.Greeting = $"FPS: {Time.Now:F0}";` from `OnUpdate`. This is the cleanest jump from "static document" to "live HUD".
- **Add a background card.** Give `CenterPanel` a `BackgroundColor` (e.g. `#000000aa`), `BorderRadius` `8`, and a small Padding so the text sits inside a frosted chip instead of floating naked.
- **React to clicks.** Switch `Show(...)` to `SuiInputMode.MouseOnly` and wire an `OnClick` event on `CenterPanel` in the Designer's Events tab — the Designer will generate a partial method stub for you to fill in.
- **Animate the entrance.** Add a `FadeIn` keyframe animation in the Designer's Animations tab and trigger it from `OnStart` immediately after `Show(...)`.
- **Promote to a real HUD.** Add a second Text for a subtitle, a third for an FPS readout, and switch the controller to drive both via Variables instead of one literal Prop. At that point you have outgrown this sample — see `counter_button` for the next step.

## Related

- [`counter_button`]({% link samples/counter_button.md %}) — the natural next sample. Adds one Variable, one OneWay binding, and one OnClick event to the same skeleton.
- [`dialog_system`]({% link samples/dialog_system.md %}) — what a "real" HUD built on top of these primitives looks like once Events and TwoWay bindings enter the picture.
- [`settings_full`]({% link samples/settings_full.md %}) — the far end of the difficulty curve, showing every binding mode and input widget exercising the same mount/teardown contract used here.

## See also

- [Read the full `empty_canvas` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/empty_canvas).
- [Showcase samples]({% link reference/showcase-samples.md %}) — the doc-site-native catalog of every V1.5 sample with Variables / Bindings tables inlined.
- [Sample index]({% link reference/sample-index.md %}) — the lightweight tabular index of all samples.
- [`counter_button`]({% link samples/counter_button.md %}) — the recommended next sample after this one.
- [`dialog_system`]({% link samples/dialog_system.md %}) — a "real" HUD built on the same mount/teardown contract.
