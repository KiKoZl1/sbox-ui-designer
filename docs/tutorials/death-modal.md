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

## Step 9 — Wire up the buttons

After **Compile** (`Ctrl+B`), open `Code/UI/DeathModal.razor`:

```razor
@inherits PanelComponent
@attribute [StyleSheet]

<root>
  <div class="sui-elem-1 backdrop">
    <div class="sui-elem-2 content-column">
      <label class="title-text">YOU DIED</label>
      <label class="killed-by-text">Killed by Bandit Archer</label>
      <label class="timer-text">Respawning in 5...</label>
      <div class="sui-elem-6 buttons-row">
        <button class="respawn-button">RESPAWN</button>
        <button class="main-menu-button">MAIN MENU</button>
      </div>
    </div>
  </div>
</root>
```

You don't edit this file. Instead, create a sibling partial in `Code/UI/`:

```csharp
// Code/UI/DeathModal.Logic.cs
using Sandbox;

namespace Game.UI;

public partial class DeathModal
{
    public string KilledByName { get; set; } = "Unknown";
    public float RespawnSeconds { get; set; } = 5f;

    public event System.Action OnRespawnClick;
    public event System.Action OnMainMenuClick;

    protected override void OnAwake()
    {
        base.OnAwake();
        // Find the buttons by their generated class names and wire them up
        var respawn = Panel.Descendants.FirstOrDefault(p => p.HasClass("respawn-button"));
        var menu = Panel.Descendants.FirstOrDefault(p => p.HasClass("main-menu-button"));
        if (respawn != null) respawn.AddEventListener("onclick", () => OnRespawnClick?.Invoke());
        if (menu != null) menu.AddEventListener("onclick", () => OnMainMenuClick?.Invoke());
    }

    protected override int BuildHash() => System.HashCode.Combine( KilledByName, (int)RespawnSeconds );
}
```

(V1.5 will expose buttons as proper `[Property]` references so you won't need the Descendants scan.)

## Step 10 — Live countdown

To update the timer text from gameplay code, you'd typically use a binding:

```csharp
// Where you spawn the modal
var modal = ScreenPanelHost.Components.Create<DeathModal>();
modal.KilledByName = killer.DisplayName;
modal.RespawnSeconds = 5f;

// Per frame, refresh the timer
async Task CountdownAsync()
{
    while ( modal.RespawnSeconds > 0 )
    {
        modal.RespawnSeconds -= Time.Delta;
        await Task.Frame();
    }
    Respawn();
}
```

But for the text to actually update in the panel, you need to bind it. V1 emits a static label; **V1.5 will introduce `[Property]` bindings via the Bindings tab** which makes this trivial:

```
TimerText.Text  ←  $"Respawning in {(int)Math.Ceiling(RespawnSeconds)}..."
```

Until then, in your DeathModal partial:

```csharp
protected override int BuildHash() => System.HashCode.Combine( RespawnSeconds );

public override void Tick()
{
    base.Tick();
    var timer = Panel.Descendants.FirstOrDefault(p => p.HasClass("timer-text")) as Label;
    if (timer != null) timer.Text = $"Respawning in {(int)Math.Ceiling(RespawnSeconds)}...";
}
```

Forces a re-render every time `RespawnSeconds` changes.

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

  // A subtle fade-in when the modal appears
  &:intro {
    opacity: 0;
    transition: opacity 0.3s ease-out;
  }
}
```

Recompile + Play to see hovers and the fade-in.

## What you learned

- Stretch anchor with all-zero margins for full-screen overlays.
- Centered content via `Anchor: MiddleCenter` + flex `JustifyContent: Center`.
- Buttons with hover effects via `.User.scss`.
- Manual partial class for event wiring (until V1.5 bindings ship).
- `:intro` transition for entrance animation.

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
