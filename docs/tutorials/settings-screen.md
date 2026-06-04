---
layout: default
title: Settings screen
parent: Tutorials
nav_order: 4
---

# Tutorial — Settings screen with input widgets
{: .no_toc }

Build a settings panel that exercises every V1.5 M4 input widget: `TextEntry`, `Slider`, `Toggle`, `DropDown`, plus the **Apply API** for explicit commits. ~20 minutes.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What we'll build

A 400×460 panel with:

- "Settings" title (Text).
- **Player Name** — TextEntry → `PlayerName: string`, UpdateTrigger `Manual`.
- **Master Volume** — Slider → `Volume: float (0..100)`, UpdateTrigger `OnRelease`.
- **Music Enabled** — Toggle → `MusicEnabled: bool`, UpdateTrigger `OnChange` (atomic).
- **Graphics Preset** — DropDown (Low / Medium / High / Ultra) → `GraphicsPreset: int`, UpdateTrigger `Manual`.
- **Apply** button + **Cancel** button.

Save → commits PlayerName + GraphicsPreset (the two Manual bindings). The slider releases on its own. The toggle is always live.

## Prerequisites

- [SUI Designer installed]({% link getting-started/install.md %}) and the editor running.
- Comfortable with [Variables]({% link concepts/variables.md %}) and [Bindings]({% link concepts/bindings.md %}).

## 1. Create the `.sui`

Asset Browser → right-click → **New** → **Sbox UI Document**. Name it `SettingsPanel`. Double-click to open.

In the Output section: Class Name = `SettingsPanel`, Namespace = `Game.UI`.

## 2. Declare the Variables

Bottom panel → **Variables** tab → **+ Add Variable** four times:

| Name | Type | Default | IsPublic | Group |
|---|---|---|---|---|
| `PlayerName` | string | `"Player"` | false | Settings |
| `Volume` | float | `50` | false | Settings |
| `MusicEnabled` | bool | `true` | false | Settings |
| `GraphicsPreset` | int | `1` | false | Settings |

## 3. Build the layout

Add a root **Panel** with size 400×460, anchored MiddleCenter. Background `rgba(15, 15, 18, 0.95)`, BorderRadius 8, padding 24px.

Inside, add a **VerticalBox** (Flex column) filling the panel — gap 16px.

Inside the VerticalBox:

### Title

**Text** — Text "Settings", FontSize 24, FontWeight Bold, Color #ffffff, TextSizeMode Fixed, Height 40.

### Player Name row

**HorizontalBox** (Flex row) gap 12, AlignItems Center, Height 32.
- **Text** "Name:" — Width 80.
- **TextEntry** — Name `PlayerNameField`. PlaceholderText "Type your name". Width 280.

### Master Volume row

**HorizontalBox** gap 12, AlignItems Center, Height 32.
- **Text** "Volume:" — Width 80.
- **Slider** — Name `VolumeSlider`. Min 0, Max 100, Step 1, Show Value true. Width 280.

### Music toggle row

**HorizontalBox** gap 12, AlignItems Center, Height 32.
- **Toggle** — Name `MusicToggle`. ToggleLabelText "Music enabled".

### Graphics preset row

**HorizontalBox** gap 12, AlignItems Center, Height 32.
- **Text** "Graphics:" — Width 80.
- **DropDown** — Name `GraphicsField`. Options ["Low", "Medium", "High", "Ultra"]. Width 280.

### Buttons row

**HorizontalBox** gap 12, JustifyContent FlexEnd, Height 48. (Push to bottom — set Flex grow on a Panel filler above if you want.)
- **Button** — Name `CancelButton`. ButtonText "Cancel". Width 100. Hover state: brighter bg.
- **Button** — Name `ApplyButton`. ButtonText "Apply". Width 100. BackgroundColor #4ade80. Hover state: brighter green.

## 4. Bind the inputs

Select **PlayerNameField** → Details → click chain icon next to **Value** → Bind popup:

- Source: `PlayerName`
- Mode: TwoWay (default)
- **UpdateTrigger: Manual** ← important
- OK.

Select **VolumeSlider** → click chain icon next to **Value**:

- Source: `Volume`
- Mode: TwoWay
- **UpdateTrigger: OnRelease**
- OK.

Select **MusicToggle** → click chain icon next to **Checked**:

- Source: `MusicEnabled`
- Mode: TwoWay
- UpdateTrigger: OnChange (only choice).
- OK.

Select **GraphicsField** → click chain icon next to **Value**:

- Source: `GraphicsPreset`
- Mode: TwoWay
- **UpdateTrigger: Manual**
- OK.

## 5. Wire the events

Bottom panel → **Events** tab → **+ Add Event** twice:

| Element | Event | Mode | Handler name |
|---|---|---|---|
| ApplyButton | OnClick | Code | OnApplyClick |
| CancelButton | OnClick | Code | OnCancelClick |

Save (`Ctrl+S`). Compile (`Ctrl+B`).

## 6. Use it from gameplay code

Open or create `Code/SettingsController.cs`:

```csharp
using Sandbox;
using SboxUiDesigner.Runtime;
using Game.UI;

public sealed class SettingsController : Component
{
    [Property] public SettingsPanel Settings { get; set; } = new();

    [Property] public string SavedPlayerName { get; set; } = "Player";
    [Property] public float  SavedVolume { get; set; } = 50f;
    [Property] public bool   SavedMusic { get; set; } = true;
    [Property] public int    SavedGraphics { get; set; } = 1;

    protected override void OnStart()
    {
        // Hydrate the panel from saved values
        Settings.PlayerName     = SavedPlayerName;
        Settings.Volume         = SavedVolume;
        Settings.MusicEnabled   = SavedMusic;
        Settings.GraphicsPreset = SavedGraphics;

        Settings.OnApplyClick  = HandleApply;
        Settings.OnCancelClick = HandleCancel;

        Settings.Show( SuiInputMode.All );  // need keyboard for typing
    }

    void HandleApply()
    {
        // Flush every Manual binding. Apply.All() in V1.5 covers TextEntry
        // (PlayerNameField → Apply.PlayerNameFieldValue) + Slider Manual
        // bindings (none here — Volume is OnRelease). Manual Toggle / DropDown
        // bindings are NOT in Apply yet (V1.5 gap) — we read them directly.
        Settings.Apply.All();
        // Read GraphicsDropdown Manual binding directly (no Apply method yet).
        if ( Settings.View?.GraphicsFieldRef is { } dd && dd.Value is int gp )
            Settings.GraphicsPreset = gp;

        SavedPlayerName = Settings.PlayerName;
        SavedVolume     = Settings.Volume;
        SavedMusic      = Settings.MusicEnabled;
        SavedGraphics   = Settings.GraphicsPreset;

        Log.Info( $"Saved: name={SavedPlayerName}, vol={SavedVolume}, music={SavedMusic}, gfx={SavedGraphics}" );
        Settings.Hide();
    }

    void HandleCancel()
    {
        // Re-push saved values into the panel — discards tentative edits.
        Settings.PlayerName     = SavedPlayerName;
        Settings.Volume         = SavedVolume;
        Settings.MusicEnabled   = SavedMusic;
        Settings.GraphicsPreset = SavedGraphics;
        Settings.RefreshView();
        Settings.Hide();
    }
}
```

Drop `SettingsController` on any GameObject. Click Play.

## 7. Verify each trigger

In Play:

- Type into **Name** — letters appear in the field. Click outside → `Settings.PlayerName` is **still the old value** (Manual). Click **Apply** → Saved log shows the new name.
- Drag **Volume** — slider moves continuously. Release → `Settings.Volume` is the new value (OnRelease).
- Click **Music** — `Settings.MusicEnabled` toggles immediately (OnChange).
- Pick a different **Graphics preset** — selection updates visually but `Settings.GraphicsPreset` is still old (Manual). Click **Apply** → log shows the new preset.
- Type into Name + change Graphics + click **Cancel** → both fields snap back to saved values.

## What you just learned

- **`UpdateTrigger.Manual`** + the **`Apply` API** decouples tentative edits from committed state — the basis of any Apply/Cancel dialog.
- **`UpdateTrigger.OnRelease`** for sliders avoids spamming side effects per drag tick.
- **`UpdateTrigger.OnChange`** for toggles is atomic — no need for Manual.
- The wrapper auto-syncs setter writes (`Settings.PlayerName = ...`) through to the View, so the **Cancel** handler just resets fields + `RefreshView()`.

## Next

- [TextEntry]({% link elements/text-entry.md %}) / [Slider]({% link elements/slider.md %}) / [Toggle]({% link elements/toggle.md %}) / [DropDown]({% link elements/dropdown.md %}) — per-element details
- [Health HUD with converters]({% link tutorials/health-hud-with-converters.md %}) — different tutorial focusing on bindings + Compose
