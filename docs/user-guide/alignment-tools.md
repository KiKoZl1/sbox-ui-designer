---
layout: default
title: Alignment tools
parent: User Guide
nav_order: 7
---

# Alignment tools

Multi-select 2+ (or 3+ for distribute) elements with the same parent, then trigger an alignment or distribution op.

## Access points

Three places, all wired to the same controller methods:

- **Mini-toolbar** → **Align ▾** dropdown + **Distribute ▾** dropdown
- **Edit menu** → **Align ▶** submenu (8 ops)
- **Right-click on multi-selection** in canvas/hierarchy (V1.1 — not yet)

## Operations

### Align (6 ops)

| Op | Behavior |
|---|---|
| **Left** | All selected → leftmost X of the selection's bounding box |
| **Center (H)** | All selected → horizontal center of the bounding box |
| **Right** | All selected → rightmost edge of bounding box |
| **Top** | All selected → topmost Y |
| **Center (V)** | All selected → vertical center |
| **Bottom** | All selected → bottommost edge |

### Distribute (2 ops)

| Op | Behavior |
|---|---|
| **Horizontally** | Equal horizontal spacing — leftmost and rightmost stay put, middle ones slide to make gaps uniform |
| **Vertically** | Same for vertical axis |

## Filters

Both ops filter the selection to:

- **Absolute-mode elements** only (Flex children are flow-positioned, alignment doesn't apply)
- **Same parent** — mixed parents → log says `"Align needs ≥2 absolute-mode elements with the same parent."` and op is skipped
- **Unlocked** — `Flags.Locked = true` elements are skipped

## Undo

Each Align / Distribute op is **one undo entry** (the command snapshots all members' positions before applying, restores them on undo).

## When you'll use them

- Pixel-aligning a row of HUD icons (Align Top, Distribute Horizontally)
- Stacking a column of menu items (Align Left, Distribute Vertically)
- Centering a callout (Align Center H + Center V relative to 2 reference elements)

## Reference

- Source: [`Editor/Commands/SuiAlignElementsCommand.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Commands/SuiAlignElementsCommand.cs)
- Source: [`Editor/Commands/SuiDistributeElementsCommand.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Commands/SuiDistributeElementsCommand.cs)
- Controller: `SuiDesignerController.AlignSelection`, `DistributeSelection`, `CollectAlignableSelection`
