---
layout: default
title: Survival HUD
parent: Tutorials
nav_order: 1
---

# Survival HUD
{: .no_toc }

Build a classic survival HUD: health / hunger / stamina bars in the bottom-left, ammo counter in the bottom-right, minimap frame in the top-right. ~20 minutes.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What we're building

```
┌──────────────────────────────────────────────────────────┐
│                                            ┌──────────┐  │
│                                            │ MINIMAP  │  │
│                                            │  frame   │  │
│                                            └──────────┘  │
│                                                          │
│                                                          │
│                                                          │
│                                                          │
│  ▓▓▓▓▓▓▓░░░  HP  85/100                                  │
│  ▓▓▓▓▓░░░░░  HUNGER  50/100             ╔════════════╗   │
│  ▓▓▓▓▓▓▓▓▓░  STAMINA  90/100            ║  30 / 90   ║   │
│                                          ╚════════════╝   │
└──────────────────────────────────────────────────────────┘
```

Three regions:

- Bottom-left: stack of 3 ProgressBars with labels.
- Bottom-right: ammo counter card.
- Top-right: minimap placeholder.

## Step 1 — Create the document

1. **Asset Browser** → right-click `Assets/UI/` (create it if needed) → **New Asset → Sbox UI Document**.
2. Name it `survival_hud.sui`.
3. Double-click to open in SUI Designer.

You'll see an empty 1920×1080 canvas with just the root `Canvas` element selected.

## Step 2 — The bottom-left stat panel

We'll build a `VerticalBox` containing 3 stat rows. Each row is a `HorizontalBox` with a bar + label.

### Create the outer VerticalBox

1. Drag **VerticalBox** from the Palette onto the canvas.
2. Rename it to **StatsContainer** (F2 on Hierarchy).
3. In Details:
   - Anchor: **BottomLeft**
   - X: `40`, Y: `40` (offset from the bottom-left corner)
   - Width: `360`, Height: `120`
   - Gap: `8` (space between rows)
4. Style:
   - Background: leave empty (the rows have their own bg)

### Create the Health row

1. Drag **HorizontalBox** onto StatsContainer (drop INSIDE it in Hierarchy, or onto its area in the canvas).
2. Rename to **HealthRow**.
3. AlignItems: `Center` (so bar and label line up vertically).
4. Gap: `12`.

Inside HealthRow:

1. Drop a **ProgressBar** in HealthRow.
   - Name: **HealthBar**
   - Width: `200`, Height: `18`
   - Background: `#22222288`
   - Border Radius: `4`
   - Props:
     - Progress Min: `0`, Max: `100`
     - Preview Value: `85`
     - Fill Color: `#ef4444` (red)
     - Direction: LeftToRight

2. Drop a **Text** in HealthRow (after HealthBar).
   - Name: **HealthLabel**
   - Text: `HP  85/100`
   - Font Size: `14`, Font Weight: Bold
   - Color: `#ffffff`

### Duplicate for Hunger and Stamina

1. Select HealthRow in Hierarchy.
2. `Ctrl+D` twice — you now have HealthRow, HealthRow (1), HealthRow (2).
3. Rename them to **HungerRow** and **StaminaRow**.
4. Edit each:
   - HungerRow → ProgressBar Fill Color `#f97316` (orange), Preview Value `50`, Label text `HUNGER  50/100`.
   - StaminaRow → ProgressBar Fill Color `#22c55e` (green), Preview Value `90`, Label text `STAMINA  90/100`.

Save (`Ctrl+S`). The canvas should now show the 3-bar stack in the bottom-left.

## Step 3 — The bottom-right ammo card

1. Drop a **Panel** on the root canvas.
2. Rename to **AmmoCard**.
3. Anchor: **BottomRight**
4. X: `40`, Y: `40`, Width: `160`, Height: `64`
5. Style:
   - Background: `#0f0f12`
   - Border: `#3b82f6`, Border Width: `2`, Border Radius: `8`
6. Layout Mode: **Flex**
7. JustifyContent: `Center`
8. AlignItems: `Center`

Inside AmmoCard:

1. Drop a **Text**.
   - Text: `30 / 90`
   - Font Size: `28`, Font Weight: ExtraBold
   - Color: `#3b82f6`

Save.

## Step 4 — The minimap frame

A placeholder frame for the minimap area. The actual minimap image would come from gameplay code at runtime.

1. Drop a **Panel** on the root.
2. Rename to **MinimapFrame**.
3. Anchor: **TopRight**
4. X: `40`, Y: `40`, Width: `220`, Height: `220`
5. Style:
   - Background: `rgba(0, 0, 0, 0.6)`
   - Border: `#ffffff44`, Border Width: `1`, Border Radius: `8`
6. Drop a **Text** inside it (as a label).
   - Text: `MINIMAP`
   - Anchor: **TopCenter**, X: `0`, Y: `8`
   - Font Size: `12`, Font Weight: SemiBold
   - Color: `#ffffff88`

Save.

## Step 5 — Test in Play

1. Set the **Output** in the right panel:
   - Class Name: `SurvivalHud`
   - Namespace: `Game.UI`
2. Click **Test in Play** in the top toolbar.
3. The preview stage scene opens, you spawn as a TPS character, and the HUD overlay shows your stats.

Walk around. The bars, ammo card, and minimap frame should be exactly where the canvas drew them.

Stop Play (Esc → Stop button) to exit.

## Step 6 — Compile for real

When you're ready to use this HUD in your game:

1. Click **Compile** (`Ctrl+B`).
2. The folder picker asks where to put `SurvivalHud.razor`. Pick `Code/UI/`.
3. The Compile Results panel shows three files written:
   - `SurvivalHud.razor`
   - `SurvivalHud.razor.scss`
   - `SurvivalHud.User.scss` (boilerplate; you can edit this)

In your scene, add a `ScreenPanel` GameObject and add the `SurvivalHud` component. The HUD overlays the screen.

## Step 7 — Hover effects (optional)

Open `Code/UI/SurvivalHud.User.scss` and add:

```scss
SurvivalHud {
  // Pulsing red glow when health is low — flip an "low-health" class from C# code
  .health-bar.low-health {
    box-shadow: 0 0 8px rgba(239, 68, 68, 0.6);
    animation: pulse 0.8s ease-in-out infinite;
  }

  @keyframes pulse {
    0%, 100% { opacity: 1; }
    50%      { opacity: 0.6; }
  }
}
```

Recompile → Play → in your C# component, `HealthBar.SetClass("low-health", currentHealth < 30)`.

## What you learned

- Three layout modes used together: Flex for VerticalBox / HorizontalBox / AmmoCard, Absolute for everything else.
- Anchor-based positioning to pin elements to corners.
- ProgressBar configuration.
- Duplicate + edit pattern for repeating UI elements.
- Test in Play vs Compile workflow.
- `.User.scss` for effects beyond the inspector.

## Next steps

- [Inventory screen]({% link tutorials/inventory-screen.md %}) — grid layouts, slot interactions.

## See also

- [Health HUD with converters]({% link tutorials/health-hud-with-converters.md %}) — modern V1.5 take using bindings + Compose
- [Bindings]({% link concepts/bindings.md %}) — for the V1.5 binding-driven HUD pattern
- [Variables]({% link concepts/variables.md %}) — declare Hp / MaxHp as typed Variables instead of hard-coding
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — the `<Name>.cs` your gameplay code sets fields on
- [Death modal]({% link tutorials/death-modal.md %}) — full-screen overlays with countdown.
