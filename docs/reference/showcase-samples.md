---
layout: default
title: Showcase samples
parent: Reference
nav_order: 11
---

# Showcase samples
{: .no_toc }

The five beginner showcase samples shipped with V1.5 — each one is the smallest possible end-to-end example of a single SUI Designer concept. Open the `.sui`, drop the companion `Component` on a `GameObject`, and you have a working UI in seconds.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

Showcase samples live in [`samples/showcase/`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase) in the source repo. Each sample folder ships a `.sui` document, a companion `<Name>Controller.cs`, and a per-sample `README.md` with the full setup walkthrough.

Work through them in the order presented — they build on each other conceptually, even though each one stands alone code-wise.

---

## empty_canvas

Minimum viable SUI document — proves the wrapper-mounting plumbing works end-to-end before you start adding complexity.

**What you'll see** — a single line of bold white text reading "Hello SUI!" rendered at 48pt in the dead center of the screen. No background, no border, no decoration — just text floating on top of whatever the game is drawing behind it. The panel uses `SuiInputMode.Passive`, so it never steals focus or swallows pointer events.

### Variables

| Name | Type | Role |
|------|------|------|
| _(none)_ | — | This sample intentionally has no Variables. The `"Hello SUI!"` string is baked into the document as a literal Prop. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| _(none)_ | — | — | — |

[Read the full `empty_canvas` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/empty_canvas).

---

## label_clock

`OneWay` binding from gameplay → UI Variable. The simplest useful pattern: one `Text` element bound to a `string` Variable, with a tiny companion `Component` driving the Variable every frame.

**What you'll see** — a small dark pill anchored to the top-right of the screen. The label `Time:` sits above a large monospace value (e.g. `14:07:42`) that ticks every frame. The value text is green-ish (`#4ade80`) over a translucent near-black background with rounded corners. Pure passive readout — nothing reacts to the mouse, nothing blocks input.

### Variables

| Name | Type | Role |
|------|------|------|
| `ClockText` | `string` | The rendered time string. Driven from `OnUpdate` in C#. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| `ClockValue` | `Text` | `ClockText` | OneWay (`UpdateTrigger = OnChange`) |

[Read the full `label_clock` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/label_clock).

---

## health_bar

`OneWay` `ProgressBar` binding + `float` Variable. A minimal player-health HUD — one `ProgressBar` plus two `Text` widgets, bound to two Variables driven by the companion component.

**What you'll see** — a small dark panel anchored to the top-left of the screen with the label `HP`, a green fill bar, and a `current / max` numeric readout. The bar shrinks and the text updates whenever `Health` changes — no manual UI plumbing, just Variable assignments. Call `hud.TakeDamage(25f)` from anywhere in gameplay and the bar drops to 75%.

### Variables

| Name | Type | Role |
|------|------|------|
| `HealthFraction` | `float` | Normalized 0..1 value driving the ProgressBar fill. |
| `HealthLabel` | `string` | Display string like `"100 / 100"` rendered in the value text. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| `HealthBar` | `ProgressPreviewValue` | `HealthFraction` | OneWay |
| `ValueText` | `Text` | `HealthLabel` | OneWay |

[Read the full `health_bar` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/health_bar).

---

## counter_button

`Button.OnClick` (Code mode) + Variable update from event. The smallest possible "click does something, UI reflects it" example — a single button increments a counter stored in C# and displayed via a one-way Variable binding.

**What you'll see** — a small dark card floats in the middle of the screen. It contains a large green `0` and a green `+1` button below it. Every click bumps the number by one and the label updates instantly. The Component's `Count` property is exposed in the Inspector if you want to inspect or seed it from the editor.

### Variables

| Name | Type | Role |
|------|------|------|
| `CountText` | `string` | Stringified counter value. Written by the companion after each click; `CountLabel` binds to it. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| `CountLabel` | `Text` | `CountText` | OneWay |

### Events

| Element | Event | Mode | Handler |
|---------|-------|------|---------|
| `IncrementButton` | `OnClick` | Code | `OnIncrementClick` (resolved on the companion `Component`) |

[Read the full `counter_button` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/counter_button).

---

## toggle_pause

`Toggle` `TwoWay` binding to `bool` Variable. The smallest possible "user input flips a flag, UI reacts" example — a `Toggle` widget is `TwoWay`-bound to a `bool` Variable, and a `Text` element next to it mirrors the toggle state with a coloured label.

**What you'll see** — a small dark bar appears at the top centre of the screen. It contains a "Pause" toggle and a status label. Frame zero the label reads `Running` in green. Click the toggle and the label instantly flips to `Paused`. Click again to flip back. No timers, no animation, no networking — just the binding round-trip.

### Variables

| Name | Type | Role |
|------|------|------|
| `IsPaused` | `bool` | The pause flag. `TwoWay`-bound to the toggle's `Checked` property — the user flips it from the UI, the controller reads it. |
| `StatusText` | `string` | Human-readable mirror of `IsPaused`. Written by the controller in `OnUpdate` whenever `IsPaused` changes. |

### Bindings

| Element | Property | Variable | Mode |
|---------|----------|----------|------|
| `PauseToggle` | `Checked` | `IsPaused` | TwoWay (`UpdateTrigger = OnChange`) |
| `StatusText` | `Text` | `StatusText` | OneWay |

[Read the full `toggle_pause` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/toggle_pause).

---

## Coming soon

A second wave of samples is planned to cover intermediate and advanced patterns. None of these ship in V1.5.

**Intermediate** (V1.5.1)

- `settings_screen` — every V1.5 M4 input widget (`TextEntry`, `Slider`, `Toggle`, `DropDown`) on one panel + the **Apply API**.
- `chat_panel` — scrollable message log + `TextEntry` submit + RPC fan-out.
- `inventory_grid` — `ForEach` over a `List<Item>` Variable + slot composition via `SuiReference`.

**Advanced** (V1.5.1)

- `survival_hud_full` — health / hunger / stamina / thirst bars + ammo counter + pickup toast, all driven from a single `PlayerStats` component.
- `death_respawn_modal` — full-screen overlay, respawn countdown, two action buttons, modal focus capture.
- `quest_journal` — tabbed quest log with active / completed / failed lists, expandable entries, scroll-to-active.

---

## See also

- [Sample index]({% link reference/sample-index.md %}) — the high-level catalog with difficulty + difficulty groupings.
- [Tutorials]({% link tutorials/index.md %}) — guided walkthroughs that pair with the showcase samples.
- [Bindings]({% link concepts/bindings.md %}) — the model behind `OneWay` / `TwoWay`.
- [Events & Actions]({% link concepts/events-and-actions.md %}) — what `counter_button` is exercising.
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how a `.sui` becomes the C# `new()`able wrapper the samples mount.
