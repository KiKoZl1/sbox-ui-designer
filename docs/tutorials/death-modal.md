---
layout: default
title: Death modal
parent: Tutorials
nav_order: 3
---

# Death modal
{: .no_toc }

Build a full-screen "you died" overlay with a respawn countdown and two action buttons. ~10 minutes.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What we're building

```
┌────────────────────────────────────────────────────────┐
│  (dim red overlay across the entire screen)            │
│                                                        │
│                                                        │
│                    YOU DIED                            │
│                                                        │
│             Killed by Bandit Archer                    │
│                                                        │
│              Respawning in 5...                        │
│                                                        │
│         ┌──────────────┐  ┌──────────────┐             │
│         │   RESPAWN    │  │  MAIN MENU   │             │
│         └──────────────┘  └──────────────┘             │
│                                                        │
│                                                        │
└────────────────────────────────────────────────────────┘
```

A red-tinted full-screen Panel with centered content.

## Step 1 — Document

Create `Assets/UI/death_modal.sui`. Output:

- Class Name: `DeathModal`
- Namespace: `Game.UI`

## Step 2 — Tinted full-screen backdrop

1. Drop a **Panel** on the root.
2. Rename: **Backdrop**
3. Anchor: **Stretch**, X/Y/W/H = `0/0/0/0` (zero margins = full screen).
4. Style:
   - Background: `rgba(80, 0, 0, 0.7)` (dark red, 70% opaque)
   - Pointer Events: **All** (block clicks to the world below)

## Step 3 — Centered content column

Inside Backdrop:

1. Drop a **VerticalBox**.
2. Rename: **ContentColumn**
3. Anchor: **MiddleCenter**
4. X: `0`, Y: `0`, Width: `480`, Height: `380`
5. Gap: `24`
6. AlignItems: `Center`
7. JustifyContent: `Center`

Everything we add now goes inside ContentColumn.

## Step 4 — Title

1. Drop a **Text** in ContentColumn.
2. Rename: **TitleText**
3. Text: `YOU DIED`
4. Font Size: `72`, Font Weight: ExtraBold
5. Color: `#ef4444`
6. TextAlign: Center

## Step 5 — Killed-by line

1. Drop a **Text** in ContentColumn.
2. Rename: **KilledByText**
3. Text: `Killed by Bandit Archer`
4. Font Size: `20`, Font Weight: Normal
5. Color: `#ffffffcc`

## Step 6 — Respawn timer

1. Drop a **Text** in ContentColumn.
2. Rename: **TimerText**
3. Text: `Respawning in 5...`
4. Font Size: `28`, Font Weight: SemiBold
5. Color: `#ffffff`

This will be live-updated by your gameplay code; the static text is just a preview.

## Step 7 — Buttons row

1. Drop a **HorizontalBox** in ContentColumn.
2. Rename: **ButtonsRow**
3. Width: `400`, Height: `60`, Gap: `24`
4. JustifyContent: `Center`

Inside ButtonsRow:

1. Drop a **Button**.
   - Name: **RespawnButton**
   - Button Text: `RESPAWN`
   - Font Size: `18`, Font Weight: Bold
   - Color: `#ffffff`
   - Background: `#dc2626`
   - Border Radius: `8`
   - Width: `180`, Height: `48`

2. Drop another **Button**.
   - Name: **MainMenuButton**
   - Button Text: `MAIN MENU`
   - Font Size: `18`, Font Weight: Bold
   - Color: `#ffffff`
   - Background: `#374151`
   - Border Radius: `8`
   - Width: `180`, Height: `48`

Save (`Ctrl+S`).

## Step 8 — Test in Play

Click **Test in Play**. The death modal overlays the test stage scene. Walk around — the overlay stays full-screen and the buttons (visually) catch hover.

## Step 9 — Declare Variables and bind the text

Switch from a static modal to one driven by gameplay code. We'll declare two **Variables** on the document and bind the text labels.

### Variables tab

Open the **Variables** tab → **+ Add Variable** twice:

| Name | Type | Default | IsPublic | Group |
|---|---|---|---|---|
| `KilledByName` | string | `Unknown` | true | Public |
| `RespawnSeconds` | float | `5` | true | Public |

### Bind KilledByText

Select **KilledByText**. Click the chain icon next to **Text**. Bind popup:

- Source: `KilledByName`
- Converter chain: `builtin.Compose`. In the Compose **+** menu:
  1. Pick **Text** → `"Killed by "` → OK.
  2. Pick **+ → Variable** → `KilledByName`.

Resulting Compose call: `Compose("Killed by ", KilledByName)`. OK.

### Bind TimerText

Select **TimerText**. Click the chain icon next to **Text**. Bind popup:

- Source: `RespawnSeconds`
- Converter chain:
  1. Add `builtin.Ceil` — feeds the chain (float → float, rounded up).
  2. Add `builtin.FloatToInt` — no extra args.
  3. Add `builtin.Compose`. In the Compose **+** menu:
     - Pick **Text** → `"Respawning in "` → OK.
     - Pick **+ → Chain feed** (the previous step's int).
     - Pick **+ → Text** → `"..."` → OK.

Resulting chain: `RespawnSeconds → Ceil → FloatToInt → Compose("Respawning in ", chain, "...")`.

> See [Health HUD with converters]({% link tutorials/health-hud-with-converters.md %}) for a deeper walkthrough of Compose chains.

### Events tab — wire the buttons

Open the **Events** tab. The matrix lists every event-capable element in the document.

1. On **RespawnButton → OnClick**: pick **Code** mode → handler name `OnRespawnClick`.
2. On **MainMenuButton → OnClick**: pick **Code** mode → handler name `OnMainMenuClick`.

Save (`Ctrl+S`). Compile (`Ctrl+B`).

The generator emits a `DeathModal` wrapper class with:

```csharp
[Property, Group("Public")]  public string KilledByName   { get; set; } = "Unknown";
[Property, Group("Public")]  public float  RespawnSeconds { get; set; } = 5f;
[Property, Group("Events")]  public Action OnRespawnClick { get; set; }
[Property, Group("Events")]  public Action OnMainMenuClick { get; set; }
```

You don't write any partial class. The wrapper is `sealed`; gameplay code touches it through `[Property]` declarations on your own Component (see Step 10).

## Step 10 — Drive the modal from gameplay code

Declare the wrapper as a `[Property]` on any Component, then drive it:

```csharp
using Sandbox;
using Game.UI;
using SboxUiDesigner.Runtime;

public sealed class DeathSequence : Component
{
    [Property] public DeathModal Modal { get; set; } = new();

    public void Trigger( string killerName )
    {
        Modal.KilledByName   = killerName;
        Modal.RespawnSeconds = 5f;
        Modal.OnRespawnClick  = HandleRespawn;
        Modal.OnMainMenuClick = HandleMainMenu;
        Modal.Show( SuiInputMode.All );   // mount + cursor + keyboard focus
        _ = CountdownAsync();
    }

    async Task CountdownAsync()
    {
        while ( Modal.RespawnSeconds > 0 )
        {
            Modal.RespawnSeconds -= Time.Delta;
            await Task.Frame();
        }
        HandleRespawn();
    }

    void HandleRespawn()
    {
        Modal.Hide();
        // ... your respawn logic
    }

    void HandleMainMenu()
    {
        Modal.Hide();
        // ... return to main menu
    }
}
```

A few things to notice:

- `Modal` is a plain wrapper, not a Component. You **never** call `Components.Create<DeathModal>()` — the wrapper auto-mounts a `ScreenPanel` + host on `Show()`.
- Each `Modal.RespawnSeconds = ...` assignment auto-pushes into the live View via `SyncFieldsTo`, so the bound `TimerText` re-renders on the next frame. No `RefreshView()` needed for property edits.
- `Show( SuiInputMode.All )` is the one-shot helper for full-screen modals that need keyboard + mouse focus. See [Wrapper API]({% link reference/wrapper-api.md %}) for the other input modes.

## Step 11 — Hover polish

Open `Code/UI/DeathModal.User.scss`:

```scss
DeathModal {
  .respawn-button, .main-menu-button {
    transition: background-color 0.15s ease, transform 0.1s ease;
    cursor: pointer;
  }

  .respawn-button:hover { background-color: #ef4444; transform: scale(1.04); }
  .respawn-button:active { transform: scale(0.98); }

  .main-menu-button:hover { background-color: #4b5563; transform: scale(1.04); }
  .main-menu-button:active { transform: scale(0.98); }
}
```

Recompile + Play to see the hover and press feedback.

> Sandbox.UI silently drops pseudo-classes the parser doesn't recognise (see [Sandbox.UI CSS limitations]({% link concepts/sandbox-ui-css-limitations.md %})). Stick to the documented set in [Allowed CSS]({% link reference/allowed-css.md %}). For entrance animations, add a class via `Modal.View?.AddClass("fading-in")` and animate that — covered in a future animation tutorial.

## What you learned

- Stretch anchor with all-zero margins for full-screen overlays.
- Centered content via `Anchor: MiddleCenter` + flex `JustifyContent: Center`.
- Buttons with hover effects via `.User.scss`.
- Variables + Compose binding for live text from gameplay code.
- Code-mode Events for `OnClick` handlers wired through the wrapper's `[Property] Action` slots.
- `SuiInputMode.All` for full-screen modals that need mouse + keyboard focus.

## You're done

You've built three real UIs:

1. [Survival HUD]({% link tutorials/survival-hud.md %}) — corners + bars.
2. [Inventory screen]({% link tutorials/inventory-screen.md %}) — grids + multi-region layout.
3. [Death modal]({% link tutorials/death-modal.md %}) — full-screen overlay with logic.

From here, the [Element reference]({% link elements/index.md %}) and the [Concepts]({% link concepts/index.md %}) section cover everything else. The [Architecture]({% link architecture/index.md %}) section is for when you want to extend or modify SUI Designer itself.

## See also

- [Settings screen]({% link tutorials/settings-screen.md %}) — modern V1.5 modal pattern using input widgets + Apply API
- [Events & Actions]({% link concepts/events-and-actions.md %}) — wire the Respawn / Quit buttons via Code or Doo handlers
- [Wrapper API]({% link reference/wrapper-api.md %}) — `Show()` / `Hide()` / `SuiInputMode` for full-screen modals
