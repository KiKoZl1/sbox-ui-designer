---
layout: default
title: Manual commit with Apply
parent: Workflows
nav_order: 8
---

# Manual commit with `Apply`
{: .no_toc }

`UpdateTrigger.Manual` + the `wrapper.Apply.<Field>()` namespace — the explicit-save pattern for forms, Apply/Cancel dialogs, and any UI where commits shouldn't happen on every change.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The pattern

A typical Settings panel has:

- Multiple input widgets (TextEntry, Slider, Toggle, DropDown).
- A **Save** button that commits every change at once.
- A **Cancel** button that discards every change.

With realtime `OnChange` triggers, every keystroke / drag tick / click would write through to the underlying Variables — there's no "tentative changes vs committed" distinction. With `UpdateTrigger.Manual`, the widget keeps its current visual state but **never writes back** until you call `wrapper.Apply.<Field>()` explicitly.

## Authoring

In the Bind popup for each input widget:

1. **Mode** — TwoWay (default for input widgets).
2. **UpdateTrigger** — **Manual**.
3. OK.

The codegen now emits a widget without bind/handler (renderer keeps the ref but doesn't write back) and the wrapper grows an `Apply` class:

```csharp
public sealed class SettingsPanel : SuiPanel<SettingsPanelView>
{
    [Property] public string PlayerName { get; set; } = "Player";
    [Property] public float  Volume      { get; set; } = 50f;
    [Property] public int    GraphicsPreset { get; set; } = 1;

    public ApplyApi Apply { get; }

    public sealed class ApplyApi
    {
        public void PlayerName()     { /* copy view.PlayerNameRef.Text → wrapper.PlayerName */ }
        public void Volume()         { /* copy view's slider visual → wrapper.Volume */ }
        public void GraphicsPreset() { /* copy view.GraphicsPresetRef.Value → wrapper.GraphicsPreset */ }

        public void All()
        {
            PlayerName();
            Volume();
            GraphicsPreset();
        }
    }
}
```

The class is **only emitted when at least one Manual binding exists**. Wrappers with no Manual bindings have no `Apply` property — autocomplete stays clean.

## Use from gameplay code

```csharp
public sealed class SettingsController : Component
{
    [Property] public Game.UI.SettingsPanel Settings { get; set; } = new();

    protected override void OnStart()
    {
        Settings.Show( SuiInputMode.All );   // need keyboard focus for typing
    }

    public void OnSaveClick()
    {
        Settings.Apply.All();   // flush every Manual binding
        SaveToDisk( Settings.PlayerName, Settings.Volume, Settings.GraphicsPreset );
    }

    public void OnCancelClick()
    {
        // Each Manual widget retains its visual state. To "cancel" — refresh
        // the view from the wrapper's current Variable values:
        Settings.RefreshView();
    }
}
```

`.Apply.All()` invokes every Manual method in declaration order — covers the common Save-button pattern in one call.

For a partial save (only commit one field), call the method directly:

```csharp
Settings.Apply.PlayerName();   // commit only this one — Volume / GraphicsPreset stay tentative
```

## Mixing triggers within a panel

A single document can mix `OnChange` and `Manual` bindings freely. Per-binding control:

- A live preview slider that drives a real-time effect → `OnChange`.
- A text field that should only commit on Apply → `Manual`.

Codegen emits the matching shape per binding. The `Apply` namespace only contains the Manual ones.

## Slider — visual buffer vs Variable

For sliders with `Manual` or `OnRelease`, the wrapper carries a `_<name>Visual` float buffer that the drag handlers update. The bound Variable only updates on commit (release or Apply). Idle Ticks resync the buffer from the Variable so external writes (e.g. you set `Settings.Volume = 75` from code) flow back to the displayed thumb position.

## TextEntry — `@ref` path

Manual TextEntry uses the `@ref` Panel to read the live `.Text`:

```csharp
public sealed class ApplyApi
{
    private readonly SettingsPanel _wrapper;
    public ApplyApi( SettingsPanel w ) { _wrapper = w; }

    public void PlayerName()
    {
        var text = _wrapper.View?.PlayerNameRef?.Text;
        if ( text != null ) _wrapper.PlayerName = text;
    }
}
```

The setter on `[Property] string PlayerName` pushes both the backing field + the View's display, so the next render sees the canonical value.

## Use case — multi-screen Settings dialog

```csharp
public sealed class SettingsScreen : Component
{
    [Property] public Game.UI.SettingsPanel Audio    { get; set; } = new();
    [Property] public Game.UI.SettingsPanel Graphics { get; set; } = new();
    [Property] public Game.UI.SettingsPanel Controls { get; set; } = new();

    void OnApplyAll()
    {
        Audio.Apply.All();
        Graphics.Apply.All();
        Controls.Apply.All();
        SaveAll();
    }
}
```

Each child panel maintains its own Apply API. Combining them is one extra line per child.

## What if there are no Manual bindings?

`Settings.Apply` doesn't exist as a property — code that references it fails to compile with a clear error. This is by design (DEVIATIONS D-029) — autocomplete on `wrapper.Apply.` shows **exactly** the fields that need manual flushing, no top-level wrapper surface pollution.

## See also

- [Input & Update triggers concept]({% link concepts/input-and-update-triggers.md %})
- [Wrapper API reference]({% link reference/wrapper-api.md %}) — the full `SuiPanel<TView>` surface
- [Bindings]({% link concepts/bindings.md %})
- [Settings screen tutorial]({% link tutorials/settings-screen.md %}) — worked example
