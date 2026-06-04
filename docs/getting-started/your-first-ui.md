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

You should see six regions: top toolbar, left sidebar (Palette + Hierarchy + Variables), center canvas with Designer/Preview/Code tabs, right Details panel, and bottom panel with Animations/Bindings/Compile Results/Logs tabs.

```
┌──────────────────────────────────────────────────────────────┐
│  [Save] [Compile]  [▶ Test in Play]  [Undo] [Redo]  [Grid]  │  ← top toolbar
├──────────┬──────────────────────────────┬───────────────────┤
│ Palette  │                              │                   │
│          │       Canvas (Designer)      │     Details       │
├──────────┤  [Designer | Preview | Code] │                   │
│Hierarchy │                              │                   │
│          │                              │                   │
├──────────┤                              │                   │
│Variables │                              │                   │
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
**ProgressBar.Value is just a preview value** — it doesn't drive anything at runtime. To drive it from gameplay code, bind it to a **Variable** in the next step.

## 5. Make `Health` a Variable

A **Variable** is a typed piece of UI-local state. A **Binding** connects a Variable to an element property. When the compiler generates the wrapper class, every Variable becomes a `[Property]` mirror you can assign from gameplay code — and the bound element property updates automatically.

In the left sidebar, open the **Variables** panel (third dock, below Hierarchy):

1. Click **+** → **Add Variable**.
2. **Name** → `Health`
3. **Type** → `float`
4. **Default** → `0.75`

Now bind `HealthBar.Value` to it:

1. Select the `HealthBar` element in the Hierarchy.
2. In **Details**, find the **Value** field under the Progress section.
3. Click the **chain icon** next to it → **Bind to Variable** → pick `Health`.
4. Mode → `OneWay` (Variable → element).

The Value field in Details now shows as bound (chain icon filled). The canvas preview still shows the bar at 75% because that's the Variable's default.

## 6. Save and compile

- `Ctrl+S` — saves the `.sui` JSON.
- `Ctrl+B` — runs the validator, generator, and writer.

The **first compile** opens a folder picker asking where the generated files should land. Pick `Code/UI/` (or create the folder via the picker). Subsequent compiles remember the choice.

Open `Code/UI/`. You should see **three** generated files plus a sidecar:

- `MyHudPanel.razor` — the actual `Panel` renderer (`<div class="background sui-background">…</div>` tree)
- `MyHudPanel.razor.scss` — styles (your color choices, sizes, etc.)
- `MyHud.cs` — the **wrapper class** (`SuiPanel<MyHudPanel>`) — this is what your gameplay code touches
- `MyHudPanel.User.scss` — empty boilerplate you can edit to override generated styles (this file is **never overwritten**)

The engine hot-reloads. `Game.UI.MyHud` is now ready to use.

{: .note }
Why three files? `MyHud.cs` exposes a friendly API (`Show()` / `Hide()` / per-Variable `[Property]` mirrors) — this is what you'd type by hand. `MyHudPanel.razor` is the Razor markup the engine paints. The `.razor.scss` is its stylesheet. See [Wrapper generation]({% link concepts/wrapper-generation.md %}) for why.

## 7. Use it from a Component

Open or create any `.cs` Component in your project (or use an existing player Controller):

```csharp
using Sandbox;
using Game.UI; // namespace from your .sui's Output.Namespace

public sealed class HudController : Component
{
    [Property] public MyHud Hud { get; set; } = new();
    [Property] public float MyHealth { get; set; } = 100f;
    [Property] public float MaxHealth { get; set; } = 100f;

    protected override void OnStart()
    {
        Hud.Show();           // mount the panel as a ScreenPanel under the scene root
    }

    protected override void OnUpdate()
    {
        if ( IsProxy ) return;
        // Drive the bound Health Variable — the wrapper auto-syncs into the live ProgressBar.
        Hud.Health = Math.Clamp( MyHealth / MaxHealth, 0f, 1f );
    }

    protected override void OnDisabled()
    {
        Hud.Remove();         // tear down on shutdown
    }
}
```

You never touch the rendered `Panel` directly. Gameplay code only ever assigns to the `[Property]` fields the wrapper exposes per Variable (here, `Hud.Health`) — the binding does the rest.

Drop `HudController` on any GameObject in your scene. Click Play. Your title and red bar appear at the top-left of the screen.

{: .tip }
You don't need to manually add `ScreenPanel` to a GameObject. `Hud.Show()` (provided by the `SuiPanel<TView>` base class) creates a child GameObject, attaches a `ScreenPanel` + host `PanelComponent`, and mounts the rendered `Panel` for you.

## 8. Iterate

Make a change in SUI Designer (e.g. change the bar color from red to yellow) → `Ctrl+S` → `Ctrl+B`. The runtime hot-reloads automatically.

For faster iteration, see [Test in Play]({% link getting-started/test-in-play.md %}) — a one-click workflow that loads a pre-baked scene with a TPS player and your UI mounted as a `ScreenPanel`.

## What you just learned

- A `.sui` document is one JSON file holding the whole element tree.
- The Palette adds elements. The Details panel edits everything about a selected element.
- Anchor + Position + Size place an element in its parent's coordinate space.
- **Variables** hold typed UI-local state; **Bindings** connect them to element properties; the generated wrapper exposes a `[Property]` mirror per Variable so gameplay code reads naturally (`Hud.Health = 0.5f`).
- Compile writes 4 files: `<Name>Panel.razor` (markup), `<Name>Panel.razor.scss` (generated styles), `<Name>.cs` (wrapper class) and `<Name>Panel.User.scss` (your-owned overrides).
- Gameplay code touches the wrapper — `Hud.Show()`, `Hud.SomeVariable = 75` — never the `Panel` directly.

## Next

You now have a Variable-driven HUD. To go further with V1.5 patterns:

- **[Health HUD with converters]({% link tutorials/health-hud-with-converters.md %})** — add a `"75/100 HP"` label via the `Compose` converter and a custom `[SuiConverter]`
- **[Settings screen]({% link tutorials/settings-screen.md %})** — TextEntry + Slider + Toggle + DropDown with the `Apply` API
- [Test in Play]({% link getting-started/test-in-play.md %}) — fast preview without scene wiring
- [Variables]({% link concepts/variables.md %}) — the typed-state mental model
- [Bindings]({% link concepts/bindings.md %}) — connecting Variables to element properties
- [Editor tour]({% link user-guide/editor-tour.md %}) — every panel and toolbar explained
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — the `SuiPanel<TView>` pattern explained
