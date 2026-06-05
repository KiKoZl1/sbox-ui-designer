---
layout: default
title: label_clock
parent: Samples
nav_order: 10
permalink: /samples/label_clock/
---

# label_clock
{: .no_toc }

A pure-readout sample that isolates the simplest V1.5 binding shape: a `OneWay` / `OnChange` binding from a controller-owned `string` Variable to a single `Text` element. There are no events, no input, no networking, no SCSS overrides — just `Hud.ClockText = now.ToString(Format)` once per frame, and the generated wrapper propagates the value into the rendered panel. Use it as your reference for "I have a value, I want a label to show it" before reaching for anything more elaborate.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What you'll see

A small 224x96 dark card pinned to the top-left of the canvas at (16, 16). It has 16px of padding, an 8px border radius, and a translucent near-black background (`#0d0d0fcc`) so a gameplay scene shows through. Inside, a muted grey caption reads `Time:` in 14px sans, and underneath, a 24px monospace green readout shows the live wall-clock — `04:37:22`-style by default — in a vivid `#4ade80` that pops against the dark card.

Nothing else moves. There is no border highlight, no cursor change, no hover state. The card is `PointerEvents = None` end-to-end, so the mouse cuts straight through it to whatever is behind.

## Behavior

1. **Mount** — `OnStart` calls `Hud.Show( SuiInputMode.Passive )`. The wrapper instantiates the generated `LabelClockPanel`, parents it under the controller's GameObject, and tells the SUI runtime "render this, but do not capture pointer or keyboard input."
2. **Seed first frame** — `OnStart` also calls `PushTime()` once before the first paint, so the user never sees the default `"00:00:00"` placeholder flash.
3. **Per-frame update** — `OnUpdate` calls `PushTime()` every tick. That reads `DateTime.Now` (or `DateTime.UtcNow` when `UseUtc` is true), formats it with `Format`, and writes to `Hud.ClockText`.
4. **Binding propagation** — Because the `ClockText -> ClockValue.Text` binding is `OnChange`, the wrapper diffs the new string against the previous one and only pushes to the element when it actually differs. At 1-second resolution that's roughly one DOM write per second.
5. **Teardown** — No explicit `OnDestroy`. When the GameObject is destroyed the wrapper's lifecycle removes the panel from its parent and the binding subscription is dropped with it.

## How to use

1. Open `label_clock.sui` in the SUI Designer window (`Window -> Sbox UI Designer`) and hit Compile.
2. Drop `LabelClockController.cs` into `Code/Samples/LabelClock/` (or anywhere under `Code/`).
3. Attach `LabelClockController` to a GameObject in any scene, hit Play. The card appears in the top-left and starts ticking immediately.

No SCSS edits are required — the generated stylesheet covers every visual you see.

## Variables

| Name | Type | Default | Role |
|---|---|---|---|
| `ClockText` | `string` | `"00:00:00"` | The formatted current-time string the controller writes once per frame. Source = `Manual`, IsPublic = true, so the wrapper exposes it as `Hud.ClockText`. |

## Bindings

| Element | Property | Variable | Mode | Update Trigger |
|---|---|---|---|---|
| `ClockValue` (Text) | `Text` | `ClockText` | `OneWay` | `OnChange` |

## Events

None. Every element has an empty `Events` object and `PointerEvents` is `None` on both `Root` and `ClockPanel`. The clock is a pure readout.

## Required `User.scss` rules

N/A — fully driven by the generated SCSS.

## Controller architecture

- **`OnStart`** — `Hud?.Show( SuiInputMode.Passive )` mounts the wrapper without grabbing input, then `PushTime()` seeds the first frame so the placeholder never flashes.
- **`OnUpdate`** — calls `PushTime()` every tick. `PushTime` null-guards `Hud`, picks `DateTime.UtcNow` or `DateTime.Now` based on `UseUtc`, formats with `Format`, and writes the result to `Hud.ClockText`. The OneWay/OnChange binding takes it from there.
- **`[Property]` knobs** — `Hud` (the generated wrapper, defaults to `new()`), `Format` (default `"HH:mm:ss"`, accepts any `DateTime.ToString` format string), `UseUtc` (default `false`, flips the data source to `DateTime.UtcNow`).
- No `OnFixedUpdate`, no `OnDestroy`, no `OnAwake`, no `OnEnabled`/`OnDisabled`. No timers, no hotkeys, no RPCs, no `[Sync]`. Pure local per-frame readout.

## File map

```text
Code/Samples/LabelClock/
  LabelClock.cs               (generated wrapper — do not edit)
  LabelClockPanel.razor       (generated markup — do not edit)
  LabelClockPanel.razor.scss  (generated styles — do not edit)
  LabelClockController.cs     (you ship this — drives the wrapper)
```

No `LabelClockPanel.User.scss` — this sample doesn't need one.

## Element tree at a glance

```text
Root (Canvas, 1920x1080, PointerEvents=None)
  ClockPanel (Panel, 224x96 @ 16,16, dark card, PointerEvents=None)
    ClockLabel (Text, "Time:", 14px, #9ca3af)
    ClockValue (Text, 24px monospace, #4ade80)   <-- bound to ClockText
```

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Card shows `00:00:00` forever | `OnUpdate` isn't running — controller is on a disabled GameObject, or the Component itself is disabled. | Enable the GameObject and the `LabelClockController` component. Confirm `OnStart` ran via a single `Log.Info` if needed. |
| Compile error: `LabelClock` not found | Controller landed before the Designer generated the wrapper. | Open `label_clock.sui` in the SUI Designer and click Compile / Force Regen so `LabelClock.cs` is emitted, then build. |
| Clock visible but text never changes | The Variable's Update Trigger was switched to `Manual`. | Either flip it back to `OnChange` in the Designer, or call `Hud.Apply.All()` (or `Hud.Apply.ClockText()`) at the end of `PushTime`. |
| Card appears in the wrong corner | Anchor / Pivot got edited. The sample uses Anchor `TopLeft` with the panel positioned at (16, 16). | Reset `ClockPanel.Anchor` to `TopLeft` and confirm `X=16 Y=16` in absolute layout. |
| Card blocks clicks on the gameplay scene | `PointerEvents` got bumped off `None` on `Root` or `ClockPanel`. | Set both back to `PointerEvents = None`, OR keep them as-is and rely on `SuiInputMode.Passive`. |
| Text is the wrong width / wraps | `TextSizeMode` got switched off `Fixed`. | Set `ClockValue.Props.TextSizeMode = Fixed` and confirm width is `192`. |

## Extending it

- **Change the format.** Swap `Format` on the controller — `"HH:mm"` for hours+minutes only, `"hh:mm:ss tt"` for a 12-hour clock with AM/PM, `"dddd HH:mm"` for "Tuesday 14:07".
- **Server-style clock.** Flip `UseUtc = true` so every player sees the same wall time regardless of their machine timezone — useful for shared scoreboard / round-end displays.
- **Recolor for emphasis.** Edit `ClockValue.Props.Color` in the Designer and bump `FontSize` to 40 to repurpose the panel as a respawn countdown placeholder.
- **Add a date row.** Drop a second `Text` child under `ClockPanel`, add a `DateText : string` Variable, bind its `Text` to `DateText` OneWay, and write `Hud.DateText = now.ToString("dddd, MMM d")` in `PushTime`.
- **Match-remaining-time.** Replace `DateTime.Now` with `TimeSpan.FromSeconds( RoundEndsAt - Time.Now )` and feed the formatted result into `ClockText` — same wrapper, same binding, different data source.
- **Two-clock readout.** Duplicate `ClockPanel` for a UTC and local pair, each with its own bound Variable, sharing one controller and one `PushTime` call.
- **Animate on change.** Add a `pulse` CSS animation in `LabelClockPanel.User.scss` keyed off a Variable like `IsCritical : bool` that the controller flips when remaining time drops under 10 seconds.

## Related

- [`empty_canvas`]({% link samples/empty_canvas.md %}) — even smaller: a starter canvas with no binding at all. Good first read if `OnChange` propagation is new.
- [`counter_button`]({% link samples/counter_button.md %}) — adds an event handler and a TwoWay-style write-back to the same OneWay foundation shown here.
- [`health_bar`]({% link samples/health_bar.md %}) — same OneWay/OnChange shape, but driving a numeric `Width` property instead of a text label.

## See also

- [Read the full `label_clock` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/label_clock).
- [Showcase samples]({% link reference/showcase-samples.md %}) — the doc-site-native catalog with every sample inlined.
- [Sample index]({% link reference/sample-index.md %}) — quick reference table of all V1.5 showcase samples.
- [`empty_canvas`]({% link samples/empty_canvas.md %}) — the no-binding starter canvas, the predecessor to this binding-driven readout.
- [`counter_button`]({% link samples/counter_button.md %}) — extends the OneWay foundation with an event and a writeback.
- [`health_bar`]({% link samples/health_bar.md %}) — same binding shape applied to a numeric `Width`.
