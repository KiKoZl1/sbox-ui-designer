---
layout: default
title: Your first UI
parent: Getting Started
nav_order: 2
---

# Your first UI
{: .no_toc }

Build a centered HUD with a health bar and a title text — from a fresh `.sui` to a compiled `PanelComponent`. ~10 minutes.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Prerequisites

- [SUI Designer installed]({% link getting-started/install.md %}) in your project's `Libraries/`.
- The s&box editor open on your project.

## 1. Create a new .sui

In the **Asset Browser**, navigate to where you want the file (e.g. `Assets/UI/`). Right-click → **New** → **Sbox UI Document**. Name it **`MyHud`**. Press Enter.

Double-click the new file. The SUI Designer window opens.

You should see five regions:

```
┌──────────────────────────────────────────────────────────────┐
│  [Save] [Compile]  [▶ Test in Play]  [Undo] [Redo]  [Grid]  │  ← top toolbar
├──────────┬──────────────────────────────┬───────────────────┤
│ Palette  │                              │                   │
│          │       Canvas (Designer)      │     Details       │
├──────────┤  [Designer | Preview | Code] │                   │
│Hierarchy │                              │                   │
│          │                              │                   │
├──────────┴──────────────────────────────┴───────────────────┤
│  [Animations | Bindings | Compile Results | Logs]           │
└──────────────────────────────────────────────────────────────┘
```

The canvas in the center is empty except for a faint outline showing the document's drawable area (1920×1080 by default).

## 2. Add a panel container

In the **Palette** (top-left), under **COMMON**, double-click **Panel**. A 100×32 panel appears at the top-left of the canvas.

In the **Details** panel (right):

- **Name** → `Background`
- **Size** → W: `1920`, H: `1080`
- **Anchor** → click the top-left cell of the 3×3 picker (already selected by default)
- **Background** (in Appearance section) → click the color box, set RGB to `rgba(15, 15, 18, 0.95)`

The whole canvas now has a dark backdrop.

## 3. Add a title

Back in the Palette, double-click **Text**. A `Text` element appears at (0, 0). Drag it to the top-center of the canvas (or set its values manually):

- **Name** → `Title`
- **Anchor** → click the top-center cell
- **Position** → X: `0` Y: `40`
- **Size** → W: `400` H: `48`
- **Text** (in the Text section) → `My HUD`
- **Font Size** → `32`
- **Font Weight** → `Bold`
- **Color** → white (`#ffffff`)
- **Text Align** → Center
- **Vertical Align** → Center
- **Text Size Mode** → Fixed

The title appears centered at the top.

## 4. Add a progress bar

Double-click **ProgressBar** in the palette. Configure:

- **Name** → `HealthBar`
- **Anchor** → top-left
- **Position** → X: `40` Y: `120`
- **Size** → W: `320` H: `24`
- **Background** → `rgba(63, 29, 29, 0.8)` (dark red)
- **Border** → `#7f1d1d`, **Border Width** → `1`, **Border Radius** → `4`
- **Value** → `0.75` (in Progress section)
- **Fill Color** → `#ef4444` (bright red)
- **Direction** → Left to Right

The bar now shows 75% filled with bright red.

{: .tip }
**ProgressBar.Value is just a preview value** — it doesn't drive anything at runtime. In your game, you bind it from code via the Razor template that gets generated.

## 5. Save and compile

- `Ctrl+S` — saves the `.sui` JSON.
- `Ctrl+B` — runs the validator, generator, and writer.

The **first compile** opens a folder picker asking where the generated files should land. Pick `Code/UI/` (or create the folder via the picker). Subsequent compiles remember the choice.

Open `Code/UI/`. You should see **three** generated files plus a sidecar:

- `MyHudPanel.razor` — the actual `Panel` renderer (`<div class="background sui-background">…</div>` tree)
- `MyHud.razor.scss` — styles (your color choices, sizes, etc.)
- `MyHud.cs` — the **wrapper class** (`SuiPanel<MyHudPanel>`) — this is what your gameplay code touches
- `MyHud.User.scss` — empty boilerplate you can edit to override generated styles (this file is **never overwritten**)

The engine hot-reloads. `Game.UI.MyHud` is now ready to use.

{: .note }
Why three files? `MyHud.cs` exposes a friendly API (`Show()` / `Hide()` / per-Variable `[Property]` mirrors) — this is what you'd type by hand. `MyHudPanel.razor` is the Razor markup the engine paints. The `.razor.scss` is its stylesheet. See [Wrapper generation]({% link concepts/wrapper-generation.md %}) for why.

## 6. Use it from a Component

Open or create any `.cs` Component in your project (or use an existing player Controller):

```csharp
using Sandbox;
using Game.UI; // namespace from your .sui's Output.Namespace

public sealed class HudController : Component
{
    [Property] public MyHud Hud { get; set; } = new();

    protected override void OnStart()
    {
        Hud.Show();           // mount the panel as a ScreenPanel under the scene root
    }

    protected override void OnUpdate()
    {
        if ( IsProxy ) return;
        // edit any [Property] field — the wrapper auto-syncs to the live View
    }

    protected override void OnDisabled()
    {
        Hud.Remove();         // tear down on shutdown
    }
}
```

Drop `HudController` on any GameObject in your scene. Click Play. Your title and red bar appear at the top-left of the screen.

{: .tip }
You don't need to manually add `ScreenPanel` to a GameObject. `Hud.Show()` (provided by the `SuiPanel<TView>` base class) creates a child GameObject, attaches a `ScreenPanel` + host `PanelComponent`, and mounts the rendered `Panel` for you.

## 7. Iterate

Make a change in SUI Designer (e.g. change the bar color from red to yellow) → `Ctrl+S` → `Ctrl+B`. The runtime hot-reloads automatically.

For faster iteration, see [Test in Play]({% link getting-started/test-in-play.md %}) — a one-click workflow that loads a pre-baked scene with a TPS player and your UI mounted as a `ScreenPanel`.

## What you just learned

- A `.sui` document is one JSON file holding the whole element tree.
- The Palette adds elements. The Details panel edits everything about a selected element.
- Anchor + Position + Size place an element in its parent's coordinate space.
- Compile writes 4 files: `<Name>Panel.razor` (markup), `<Name>.razor.scss` (generated styles), `<Name>.cs` (wrapper class) and `<Name>.User.scss` (your-owned overrides).
- Gameplay code touches the wrapper — `Hud.Show()`, `Hud.SomeVariable = 75` — never the `Panel` directly.

## Next

- [Test in Play]({% link getting-started/test-in-play.md %}) — fast preview without scene wiring
- [Editor tour]({% link user-guide/editor-tour.md %}) — every panel and toolbar explained
- [Variables]({% link concepts/variables.md %}) — make `Health` a real Variable instead of a baked literal
- [Bindings]({% link concepts/bindings.md %}) — drive `ProgressBar.Value` from your `Health` Variable
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — the `SuiPanel<TView>` pattern explained
