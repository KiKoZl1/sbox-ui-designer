---
layout: default
title: Wrapper API
parent: Reference
nav_order: 7
---

# Wrapper API
{: .no_toc }

The `SuiPanel<TView>` base class — what every generated wrapper provides for free. Source: `Code/Runtime/SuiPanel.cs`.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What it is

`SuiPanel<TView>` is the **runtime base** that every generated `<Name>.cs` extends:

```csharp
public sealed class MyHud : SuiPanel<MyHudPanel> { /* generated */ }
```

`TView` is the `<Name>Panel` renderer (a `Panel` subclass). The wrapper handles mount lifecycle + visibility + property mirroring + the recursive `ContentHash`.

## Lifecycle

| Member | Notes |
|---|---|
| `Add(GameObject parent = null)` | Mount the panel as a child of `parent` (or scene root if null). Mount is created **hidden** — call `Show()` to make it visible. Idempotent: a second `Add()` while mounted is a no-op. |
| `Show(GameObject parent = null)` | Mark visible. For a standalone wrapper this auto-mounts. For an embedded wrapper this only flips `IsShown`; parent re-renders. |
| `Show(GameObject parent, SuiInputMode mode)` | Show + set input mode in one call (UEFN-like). |
| `Show(SuiInputMode mode)` | Keyword form — `Hud.Show( mode: SuiInputMode.MouseOnly );` |
| `Hide()` | Mark hidden. Standalone: `View.Style.Display = None`. Embedded: flag only; parent re-renders with inline style. |
| `Remove()` | Destroy the mount entirely. A subsequent `Show()` / `Add()` spawns a fresh one. |
| `RefreshView()` | Re-push every wrapper field value to the live View. Call after a batch edit if auto-sync per-set isn't granular enough. |

## State queries

| Member | Notes |
|---|---|
| `IsMounted` (bool) | True while a mount exists (Add/Show called, Remove not yet). |
| `IsShown` (bool) | True when the wrapper considers itself visible. For embedded wrappers this is hashed into `ContentHash`. |
| `IsEmbedded` (bool) | True when this wrapper has been claimed as the child of another wrapper (via the generated `SyncFieldsTo`). |
| `MountedObject` (GameObject) | The host GameObject. Hidden from inspector + JSON via `[Hide]` / `[JsonIgnore]`. |
| `Host` (SuiHostPanelComponent) | The internal host PanelComponent. Hidden. |
| `View` (TView) | The live Panel rendering. Hidden. Public so a parent wrapper can reference a child's View. |

## Input mode

```csharp
public enum SuiInputMode
{
    Passive,    // UI renders but never touches the cursor — gameplay input unaffected
    MouseOnly,  // Show the cursor so the user can click panels with pointer-events: all
    All,        // Show cursor + accept focus so the panel can receive keyboard
}
```

| Member | Notes |
|---|---|
| `InputMode` (SuiInputMode) | Current input mode. |
| `SetInputMode(SuiInputMode mode)` | Change while shown. Idempotent. Hide/Remove restores the cursor visibility from before this wrapper captured it. |

`Show(SuiInputMode mode)` is the one-shot helper — combines Show + SetInputMode. Cursor visibility cooperates with other wrappers — Hide drops to `MouseVisibility.Auto`, not `Hidden`, so a second shown HUD doesn't lose its cursor.

## Embedded vs standalone

```csharp
public void MarkEmbedded()   // generator hook — called from parent's SyncFieldsTo
```

A wrapper transitions from standalone → embedded when a parent calls `MarkEmbedded()` on it. From that moment:

- `Show()` doesn't auto-mount (only flips `IsShown`).
- `Hide()` doesn't touch `View.Style.Display` directly — `ContentHash` propagates and the parent's next render emits the child's tag wrapped in `@if (child == null || child.IsShown) { ... }`.

Calling `Remove()` on an embedded wrapper is a caller error (the parent owns the mount). The runtime emits a warning.

## Generator-driven members

Each generated wrapper class **also** provides:

### Per-Variable `[Property]` mirrors

One `[Property]` field per `Manual` Variable on the document. The setter writes the backing field AND pushes the new value through to the live View. Example:

```csharp
[Property, Group("Internal")] public int Health { get; set; } = 100;
```

### Per-SuiReference named-instance fields

For each `SuiReference` element on the parent canvas, a `[Property]` field typed as the child's wrapper class:

```csharp
[Property, Group("Children")]
public global::Game.UI.ProgressBar StaminaBar { get; set; } = new();
```

ForEach SuiReferences become `List<TWrapper>`:

```csharp
[Property] public List<global::Game.UI.ChatLine> Messages { get; set; } = new();
```

### Per-event `[Property]` slots

Code-mode events:

```csharp
[Property, Group("Events")] public Action OnFireClick { get; set; }
```

Doo-mode events:

```csharp
[Property, Group("Events"), Doo.ArgumentHint<float>("value")]
public global::Sandbox.Doo OnVolumeChanged { get; set; } = /* default body */;
```

### `SyncFieldsTo` override

```csharp
protected override void SyncFieldsTo( TView view )
{
    view.Health = Health;
    view.StaminaBar = StaminaBar;
    StaminaBar.MarkEmbedded();
    view.OnFireClick = OnFireClick;
}
```

Called on `Add()` / `Show()` / `RefreshView()` / per-setter mutations.

### `ContentHash` override (recursive)

```csharp
public override int ContentHash()
    => base.ContentHash()                  // includes IsShown
       ^ HashCode.Combine( Health, MaxHealth )
       ^ (StaminaBar?.ContentHash() ?? 0)
       ^ (Messages?.Count ?? 0)
       ^ Messages?.Aggregate( 0, ( acc, m ) => acc ^ (m?.ContentHash() ?? 0) ) ?? 0;
```

The renderer's `BuildHash()` mirrors this set. Mutations at any depth propagate up. See [Composition]({% link concepts/composition.md %}#recursive-contenthash-deviations-d-015).

## The `Apply` namespace (V1.5 D-029)

Generated **only** when at least one binding on the document is `UpdateTrigger.Manual` AND on a TextEntry / Slider element (Toggle + DropDown Manual bindings produce no Apply method — see "Known gap" below). One method per qualifying Manual binding + `All()`:

```csharp
// Hierarchy: TextEntry "PlayerNameField", Slider "VolumeSlider", both bound Manual.
public sealed class ApplyApi
{
    private readonly SettingsPanel _w;
    internal ApplyApi( SettingsPanel w ) { _w = w; }

    public void PlayerNameFieldValue()  { /* read view.PlayerNameFieldRef.Text → _w.PlayerName */ }
    public void VolumeSliderValue()     { _w.View?.CommitVolume(); }

    public void All()
    {
        PlayerNameFieldValue();
        VolumeSliderValue();
    }
}

private ApplyApi _apply;
public ApplyApi Apply => _apply ??= new ApplyApi( this );
```

The method name = the **element's Name in the Hierarchy** with `"Value"` appended. Not the Variable name. Not the property name. Source: `Code/Generation/SuiWrapperEmitter.cs` `EmitManualCommitMethods`.

Wrappers without any qualifying Manual binding have no `Apply` property — `wrapper.Apply.X` compile-errors clearly.

### Known gap — Toggle + DropDown Manual

`Apply` codegen only fires for `TextEntry.Value` and `Slider.Value` bindings (the `EmitManualCommitMethods` filter). Toggle + DropDown Manual bindings still get an `@ref` but no Apply method — user code reads the widget directly through the renderer's `<ElementName>Ref` field. Future release will close this gap.

See [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %}).

## Lifecycle summary diagram

```
   new MyHud()
       │
       │  Add()           ──→  MountedObject (hidden)
       │                       Host (SuiHostPanelComponent)
       │                       View (TView mounted under Host.Panel)
       │
       │  Show()          ──→  MountedObject visible (Display = Flex)
       │                       (or Add() first if not mounted)
       │
       │  SetInputMode(X) ──→  Mouse.Visibility / Host.Panel.AcceptsFocus
       │
       │  Hide()          ──→  MountedObject still alive, Display = None
       │                       Cursor released back to Auto
       │
       │  Remove()        ──→  MountedObject destroyed
       │                       Cursor released
       v

   For embedded wrappers (claimed by parent):
       │
       │  Show()          ──→  IsShown = true; parent re-renders
       │  Hide()          ──→  IsShown = false; parent re-renders w/ @if
       │
       (Add / Remove are caller errors — parent owns the mount.)
```

## See also

- [Wrapper generation concept]({% link concepts/wrapper-generation.md %})
- [Composition]({% link concepts/composition.md %})
- [Manual commit with Apply]({% link workflows/manual-commit-with-apply.md %})
- [Input & Update triggers]({% link concepts/input-and-update-triggers.md %})
- [`Code/Runtime/SuiPanel.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Runtime/SuiPanel.cs) — source of truth
