---
layout: default
title: Sample tour
parent: Getting started
nav_order: 5
---

# Sample tour
{: .no_toc }

Guided learning path through six samples that build on each other — from "just mount a panel" to "wire a full settings flow with Apply.All()". About 60 minutes total if you read along.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Prerequisites

You should already have completed [Your first UI]({% link getting-started/your-first-ui.md %}) — open the editor, place an element, hit Compile. If those steps feel new, do that page first.

The tour assumes you can open a `.sui` file in the SUI Designer dock, regenerate the wrapper after editing, and run the host scene in Play mode. You don't need any C# experience beyond reading a method body — every controller in the tour is under 50 lines.

If you get stuck, each sample page has its own troubleshooting section. The order below is deliberate: skipping ahead is fine, but each step assumes the previous step's concepts are familiar.

---

## 1. empty_canvas

**What you'll learn:** Minimum mount lifecycle. How a wrapper class hooks into a GameObject, when Show() runs, when the Panel becomes visible.

**Time:** ~5 minutes.

**Walkthrough:** Open the sample at [samples/empty_canvas]({% link samples/empty_canvas.md %}). Read the Behavior section first — three bullets describe the mount cycle (OnStart → Hud.Show → render). Then attach the controller to a GameObject and press Play. The "Hello SUI!" label appears centered. There's nothing else — that's the point.

The reason this sample is first: every other sample in the tour reuses this exact mount pattern. Once you've seen `Hud = new MyHudPanel(); Hud.Show(this);` happen in OnStart and produce a visible panel, the rest of the framework stops being magic. The wrapper is just a class, Show() just adds a child Panel to your scene root, and your controller holds the reference.

**Next:** [Step 2 — label_clock]({% link samples/label_clock.md %}) introduces bindings.

---

## 2. label_clock

**What you'll learn:** First OneWay binding from gameplay state into a UI Variable. The OnChange trigger pattern. How the controller's Time.Now value reaches the Label without an explicit Refresh call.

**Time:** ~7 minutes.

**Walkthrough:** Open [samples/label_clock]({% link samples/label_clock.md %}). Skim the Variables table (one `ClockText` string) and the Bindings table (`ClockLabel.Text` bound OneWay). In the controller, OnUpdate just sets `Hud.ClockText = FormatTime(Time.Now)` — the binding does the rest. Press Play; the time ticks every frame.

The lesson here is the indirection: your controller never touches `ClockLabel` directly. It writes to a Variable, the wrapper notices the change via setter, and the binding system pushes the new value onto the element. That decoupling is what lets you redesign the panel later without touching gameplay code — move the Label, rename it, or replace it with a Text element, and your controller stays identical.

**Next:** [Step 3 — counter_button]({% link samples/counter_button.md %}) adds clicks.

---

## 3. counter_button

**What you'll learn:** First Code-mode OnClick handler. The "assign delegate before Hud.Show" gotcha (SyncFieldsTo copies delegates at mount time). How an Action property on the wrapper gets wired from the controller.

**Time:** ~10 minutes.

**Walkthrough:** [samples/counter_button]({% link samples/counter_button.md %}). Look at the Events table — one OnClick wired to OnIncrementClick. Then in the controller: `Hud.OnIncrementClick = OnIncrementClick` is on the line *before* `Hud.Show(...)`. Try moving it to *after* — the click silently stops working. That's SyncFieldsTo's contract.

This is the first sample where you'll feel a real footgun. The framework gives you a clean delegate-based API (assign a method to a property), but the timing matters because the wrapper snapshots delegates during Show(). Memorize the rule now and you'll save yourself a debugging session later: *delegates before Show, data updates after*.

**Next:** [Step 4 — toggle_pause]({% link samples/toggle_pause.md %}) introduces TwoWay binding.

---

## 4. toggle_pause

**What you'll learn:** Smallest possible TwoWay binding. How a user-driven widget (Toggle) round-trips a value back to a Variable. The OnChange update trigger as the default for non-text inputs.

**Time:** ~10 minutes.

**Walkthrough:** [samples/toggle_pause]({% link samples/toggle_pause.md %}). One Toggle, one `IsPaused` bool Variable, TwoWay binding. The controller's OnUpdate reads `Hud.IsPaused` and adjusts gameplay accordingly — no event listener needed; the binding propagates the user's click automatically.

Compare this to counter_button: there, you needed an explicit OnClick delegate to react to user input. Here, the Toggle's binding *is* the listener. TwoWay bindings collapse the read and write paths into a single Variable — the controller treats `Hud.IsPaused` like any other field, and the framework handles direction. Use TwoWay when the widget's value *is* the state; use OnClick when the widget's value is just a trigger.

**Next:** [Step 5 — health_bar]({% link samples/health_bar.md %}) is the first ProgressBar.

---

## 5. health_bar

**What you'll learn:** ProgressBar bound to a normalized 0..1 float. The canonical "fraction + label" pattern (two bound Variables driving one visual). When to use OneWay + OnChange vs OneWay + every frame.

**Time:** ~10 minutes.

**Walkthrough:** [samples/health_bar]({% link samples/health_bar.md %}). The ProgressBar.Value is bound to `HealthFraction` (float), and a Text element is bound to `HealthText` (string). The controller computes both from gameplay state — `Hud.HealthFraction = current / max` and `Hud.HealthText = $"{current} / {max}"` — and the bindings push them onto the elements.

The "two Variables for one visual" pattern shows up everywhere once you start looking: stamina bars, XP gauges, reload progress. The fraction drives the visual, the text drives the label, both update from the same gameplay tick. Resist the temptation to compute the text inside a binding converter — keeping the formatting in the controller makes localization and unit changes trivial later.

**Next:** [Step 6 — settings_full]({% link samples/settings_full.md %}) puts it all together.

---

## 6. settings_full

**What you'll learn:** Every input widget (TextEntry / Slider / Toggle / DropDown) wired with the right Update Trigger. The Apply.All() pattern for Manual-mode TextEntry. Dirty-state detection. The full Save/Cancel/Reset loop a shipped settings menu needs.

**Time:** ~20 minutes.

**Walkthrough:** [samples/settings_full]({% link samples/settings_full.md %}). This sample is long because it covers everything — read the Behavior section first to understand the surface, then walk through the Variables and Bindings tables to see how each widget maps to a Variable. Pay attention to which bindings use OnChange (Slider, Toggle, DropDown) vs Manual (TextEntry) — the asymmetry is intentional.

TextEntry uses Manual because flushing on every keystroke would fight the user's typing — you'd see partial values like `"127.0.0"` get parsed as broken numbers. Manual mode lets the user finish, then `Hud.Apply.All()` collects every pending change and writes them in one batch. Save commits the dirty Variables to disk; Cancel reverts to the snapshot taken at Show(); Reset restores defaults. This is the production loop.

---

## After the tour

You now have the V1.5 fundamentals. The advanced samples take these patterns and combine them:

- **[drag_drop_inventory]({% link samples/drag_drop_inventory.md %})** — ExposeAsVariable + runtime AddChild + cursor-following ghost + hit-test on mouseup. The "never mutate source mid-drag" rule comes from this sample.
- **[dialog_system]({% link samples/dialog_system.md %})** — Variable + binding for typewriter text, deferred mutation outside the event dispatch loop, and the User.scss override for runtime button styling.
- **[notification_toast_queue]({% link samples/notification_toast_queue.md %})** — CSS transitions owned in User.scss + frame-staggered class flips (.toast → .toast.show) so the intro animation actually plays.

## See also

- [Showcase samples gallery]({% link reference/showcase-samples.md %}) — browse by category
- [Concept map]({% link reference/concept-map.md %}) — find a sample by concept
- [Sample index]({% link reference/sample-index.md %}) — short catalog
- [Bindings]({% link concepts/bindings.md %}) — the model the tour samples are exercising
- [Events & Actions]({% link concepts/events-and-actions.md %}) — Code-mode and Doo-mode handlers
