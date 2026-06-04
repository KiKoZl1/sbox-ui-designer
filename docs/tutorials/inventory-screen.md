---
layout: default
title: Inventory screen
parent: Tutorials
nav_order: 2
---

# Inventory screen
{: .no_toc }

Build a full-screen inventory with a 6×4 backpack grid, an equipment slot column, and a hotbar. ~30 minutes.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What we're building

```
┌────────────────────────────────────────────────────────────┐
│                                                            │
│   ┌──────────┐    ┌────────────────────────────────────┐   │
│   │ EQUIP    │    │  BACKPACK                          │   │
│   │ ┌──┐     │    │  ┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐          │   │
│   │ │HD│     │    │  └──┘└──┘└──┘└──┘└──┘└──┘          │   │
│   │ └──┘     │    │  ┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐          │   │
│   │ ┌──┐ ┌──┐│    │  └──┘└──┘└──┘└──┘└──┘└──┘          │   │
│   │ │WP│ │OF││    │  ┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐          │   │
│   │ └──┘ └──┘│    │  └──┘└──┘└──┘└──┘└──┘└──┘          │   │
│   │ ┌──┐     │    │  ┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐          │   │
│   │ │CH│     │    │  └──┘└──┘└──┘└──┘└──┘└──┘          │   │
│   │ └──┘     │    └────────────────────────────────────┘   │
│   └──────────┘                                             │
│                                                            │
│            ┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐                │
│            └──┘└──┘└──┘└──┘└──┘└──┘└──┘└──┘                │
│            (Hotbar — pinned to bottom)                     │
└────────────────────────────────────────────────────────────┘
```

Three regions:
- **Equipment column** (left) — 4 InventorySlots for armor pieces.
- **Backpack grid** (center-right) — 6×4 InventoryGrid for general loot.
- **Hotbar** (bottom) — 8-slot Hotbar pinned to the bottom center.

## Prerequisites

You will need two small PNG icons (~64×64) at `Assets/ui/icons/iron_helmet.png` and `Assets/ui/icons/health_potion.png`. Any PNG works — substitute paths to icons you already have if not.

## Step 1 — Document setup

1. Create `Assets/UI/inventory_screen.sui`.
2. Set Output:
   - Class Name: `InventoryScreen`
   - Namespace: `Game.UI`

## Step 2 — Backdrop

Inventory is a modal screen — dim the world behind it.

1. Drop a **Panel** on the root.
2. Rename: **Backdrop**
3. Anchor: **Stretch**, X/Y/W/H = `0/0/0/0` (zero margins on all sides = full screen)
4. Style:
   - Background: `rgba(0, 0, 0, 0.7)`
   - Pointer Events: **All** (catches stray clicks)

## Step 3 — Equipment column

1. Drop a **VerticalBox** on the root (NOT on the backdrop).
2. Rename: **EquipmentColumn**
3. Anchor: **MiddleLeft**
4. X: `100`, Y: `0`, Width: `160`, Height: `400`
5. Gap: `12`, AlignItems: `Center`, JustifyContent: `Center`
6. Padding: `16` (all sides)
7. Style:
   - Background: `#0f0f12cc`
   - Border: `#3b82f6`, Border Width: `1`, Border Radius: `8`

Now drop 4 **InventorySlot** elements inside EquipmentColumn:

| Slot | Width | Height | Notes |
|---|---|---|---|
| HelmetSlot | 64 | 64 | armor head |
| ChestSlot | 64 | 64 | armor chest |
| WeaponSlot | 64 | 64 | offhand pair below |
| OffhandSlot | 64 | 64 | side-by-side with WeaponSlot |

For Weapon + Offhand side-by-side, wrap them in a **HorizontalBox** child of EquipmentColumn instead.

Each InventorySlot:

- Style.Background: `#1a1a20`
- Style.Border: `#3b82f6`, Border Width: `1`, Border Radius: `6`

Optionally, add an **ItemIcon** inside HelmetSlot to preview content:

- Image Path: `ui/icons/iron_helmet.png` (or any image you have)
- Anchor: Stretch (with 8px margins) — fills the slot

## Step 4 — Backpack grid

1. Drop an **InventoryGrid** on the root.
2. Rename: **BackpackGrid**
3. Anchor: **MiddleCenter**
4. X: `120`, Y: `0` (offset to the right of the equipment column)
5. Width: `420`, Height: `280`
6. Props:
   - Columns: `6`
   - Rows: `4` (computed, but set explicitly for clarity)
   - Cell Width: `64`, Cell Height: `64`
   - Grid Gap: `4`
7. Style:
   - Background: `#0f0f12cc`
   - Border: `#3b82f6`, Border Width: `1`, Border Radius: `8`
   - Padding: `8` (so cells don't touch the border)

InventoryGrid auto-generates 24 cells (6×4). You can drop ItemIcon children into specific slots to preview content.

To add a preview icon to the first slot:

1. Expand BackpackGrid in Hierarchy → click into the first cell.
2. Drop an **ItemIcon** in.
3. Configure:
   - Anchor: Stretch, margins `4`
   - Image Path: `ui/icons/health_potion.png`
   - Preview Count: `5`

In canvas, you'll see a potion icon + "5" badge in the top-left cell.

## Step 5 — Hotbar at the bottom

1. Drop a **Hotbar** on the root.
2. Rename: **Hotbar**
3. Anchor: **BottomCenter**
4. X: `0`, Y: `40` (40px above the bottom edge)
5. Width: `560`, Height: `72`
6. Props:
   - Columns: `8`
   - Cell Width: `64`, Cell Height: `64`
   - Grid Gap: `4`
7. Padding: `4`
8. Style:
   - Background: `rgba(0, 0, 0, 0.5)`
   - Border Radius: `8`

The Hotbar auto-creates 8 InventorySlot children.

For a tactile look, give each slot a slight inner border. To avoid editing 8 slots individually, do this in `.User.scss` after first compile:

```scss
InventoryScreen {
  Hotbar .inventory-slot {
    border: 1px solid #ffffff20;
    border-radius: 6px;
  }
}
```

## Step 6 — Title strip

A "BACKPACK" label above the grid:

1. Drop a **Text** on the root.
2. Rename: **TitleStrip**
3. Anchor: **TopCenter**
4. X: `0`, Y: `40`, Width: `420`
5. Text: `BACKPACK`
6. Font Size: `24`, Font Weight: ExtraBold
7. Color: `#ffffff`
8. TextAlign: Center

## Step 7 — Save + Test in Play

`Ctrl+S`, then click **Test in Play**.

You should see the inventory overlay rendered on a real player. Walk around — the UI stays put.

## Step 8 — Toggle visibility from gameplay code

In your C# game code, you'd want this UI to appear when the player presses Tab/I.

`InventoryScreen` is a V1.5 wrapper (`SuiPanel<InventoryScreenPanel>`), so the gameplay toggle goes through `Show()` / `Hide()` (with an `IsShown` check) — there's no `Enabled` property to flip. Declare the wrapper as a `[Property]` field on your Component with a default `new()` so it serializes on a fresh prefab:

```csharp
using Sandbox;
using Game.UI;

public sealed class InventoryToggle : Component
{
    [Property] public InventoryScreen Hud { get; set; } = new();

    protected override void OnUpdate()
    {
        if ( Input.Pressed( "Inventory" ) )
        {
            if ( Hud.IsShown ) Hud.Hide();
            else Hud.Show( SuiInputMode.MouseOnly );
        }
    }
}
```

Define an `Inventory` input action in Project Settings → Input → bind to `Tab` or `I`.

Only the `InventoryToggle` Component goes in your scene — the wrapper mounts its own `ScreenPanel` host the first time `Show()` runs.

## Step 9 — Drag-and-drop scaffolding

V1 doesn't include drag-and-drop logic out of the box — that's gameplay code. But the InventorySlot elements set `pointer-events: all` by default, so they catch clicks.

Drag-and-drop is gameplay code — expose each slot via the Events tab (`OnClick` / `OnRightClick`) or **Expose as Variable** to get a typed `@ref`, then implement the drag state in your controller. See [Events & Element refs]({% link workflows/events-and-refs.md %}).

## What you learned

- Grid + InventoryGrid + Hotbar for repeating slot layouts.
- Stretch anchor for full-screen modal backdrops.
- Padding to keep cells off container borders.
- Per-slot ItemIcon for previewing content.
- `.User.scss` for batch-styling generated slots.

## Next

- [Death modal]({% link tutorials/death-modal.md %}) — full-screen overlays with timed actions.

## See also

- [Composition]({% link concepts/composition.md %}) — embed an InventorySlot sub-UI as a SuiReference and iterate via ForEach (V1.5 pattern)
- [InventorySlot]({% link elements/inventory-slot.md %}) — per-slot element reference
- [ItemIcon]({% link elements/item-icon.md %}) — per-icon element reference
- [Sample index]({% link reference/sample-index.md %}) — `inventory_screen.sui` + `inventory_basic.sui` are the shipped references
