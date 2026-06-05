---
layout: default
title: inventory_grid_full
parent: Samples
nav_order: 9
permalink: /samples/inventory_grid_full/
---

# inventory_grid_full
{: .no_toc }

The flagship showcase of the **InventoryGrid + InventorySlot** pair plus the **Expose-as-Variable + per-child wire-up** pattern. A 6×4 backpack lands in the middle of the screen with four seeded items; hover surfaces a tooltip, left-click logs a select, right-click drops the item, double-click triggers a use — all driven from a single companion Component.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Pitch

The previous showcase samples (`counter_button`, `toggle_pause`, `health_bar`, `label_clock`) each demonstrate one piece of the SUI loop in isolation. This sample wires **all of them together** on a realistic surface:

- **InventoryGrid** as the layout container, with `Columns=6 Rows=4` to produce a 24-cell wrapped flex grid.
- **24 InventorySlots**, four of them seeded with icons + counts so the canvas preview, the SCSS background-image, and the runtime view all show identical art on frame zero.
- **Three SUI Variables** (`SlotCountText : string`, `ItemTooltip : string`, `TooltipVisible : bool`) bound to a header subtitle and a footer tooltip panel via three `OneWay` Bindings (one of which uses the universal `Visibility : bool` property to hide/show the tooltip card).
- **Two grid-level Code-mode events** (`OnClick`, `OnRightClick`) declared in the `.sui` Events block, kept as fallbacks. The real per-slot routing happens in the Component after mount — see *Why the per-slot wire-up lives in C#* below.
- **The grid is exposed as a Variable** so `Hud.View?.BackpackGrid` is a real `Sandbox.UI.Panel` the controller can walk to attach hover/click listeners to every child slot, with the slot index captured by position in the children list.

## What you'll see

A dark card (640×540) appears centred on the screen behind a translucent black scrim. The card header reads **Inventory** and a green subtitle **`4 / 24 slots`** sits just below it. A 6×4 grid of 84-pixel slots fills the middle of the card; the first four slots are tinted in distinct colours and stamped with a single-character glyph (a purple sword glyph, a red cross for the health potion, an amber circle for the bread, and a yellow `$` for the gold coin). The remaining 20 slots are empty cells with a thin border. At the bottom of the card a thin tooltip strip is hidden until you hover an item.

The default seeding uses **glyph + tint** rather than PNG icons so the sample runs out-of-the-box with zero asset shipping. See *Extending it → Using real PNG icons* below for how to swap in actual images.

Hover the sword → the tooltip strip slides into view and reads `Iron Sword  x1`. Hover the potion → `Health Potion  x5`. Click the potion → console logs `Select slot #1: Health Potion (x5)`. Right-click it → the slot's tint + glyph disappear (the slot reverts to its empty `.inv-slot` background), the subtitle ticks down to `3 / 24 slots`, and the console logs `Drop slot #1: Health Potion (x5)`. Double-click the bread → console logs `Use slot #2: Bread`.

## How to use

1. Open `inventory_grid_full.sui` once in the **SUI Designer** window (`Window → Sbox UI Designer`) and hit **Compile**. This writes `InventoryGridFullPanel.razor` + `InventoryGridFullPanel.scss` + `InventoryGridFull.cs` (the wrapper) into `Code/Samples/InventoryGridFull/` of your project.
2. Drop `InventoryGridFullController.cs` into the same folder (or anywhere under `Code/`).
3. In any scene, add a new GameObject and attach the **InventoryGridFullController** Component to it.
4. Press **Play**. The inventory card appears centred with four colour-tinted, glyph-stamped slots (no PNGs required). Hover, left-click, right-click, and double-click slots and watch the console + tooltip react.

> **No icon PNGs ship with this sample.** The four seed items render as tinted slot backgrounds with a centred single-character glyph, applied at runtime by the controller's `ApplySlotVisual` helper. To use real bitmap icons instead, see *Extending it → Using real PNG icons* below.

The Component's `SlotCapacity` and `StartingItems` properties are exposed in the Inspector if you want to reshape the demo from the editor without touching code (e.g. seed 10 items, drop capacity to 12, swap icons).

## Variables

| Name | Type | Default | Role |
|---|---|---|---|
| `SlotCountText` | `string` | `"0 / 24 slots"` | Header subtitle. Controller writes `"<filled> / <capacity> slots"` after every mutation. |
| `ItemTooltip` | `string` | `""` | Tooltip text body. Controller writes `"<name>  x<count>"` on slot hover, clears on unhover. |
| `TooltipVisible` | `bool` | `false` | Flips the tooltip card's `Visibility` (OneWay, bool — the universal property). |

> **`List<ItemEntry>` is NOT a SUI Variable.** The inventory's item list lives entirely in C# as `private List<ItemEntry> _items` on the controller. V1.5 only supports primitive Variable types (`string` / `int` / `bool` / `float` / `Color`); custom POCO lists go in the component. The pattern when SUI can't model your data: SUI owns the *view state* (counts, flags, visible strings); the controller owns the *domain state* and pushes the view-facing derivatives into Variables.

## Seed items

The four entries baked into `StartingItems` on the controller. Each item is just a name, a single-character `Glyph`, a hex `Color` for the slot background, and a stack `Count`. No PNG assets ship with the sample — the controller applies the tint + glyph at runtime in `ApplySlotVisual`, called from the slot wire-up loop.

| Slot | Name           | Glyph | Color (hex) | Count |
|------|----------------|-------|-------------|-------|
| 0    | Iron Sword     | `⚔`   | `#7c3aed` (purple) | 1   |
| 1    | Health Potion  | `✚`   | `#ef4444` (red)    | 5   |
| 2    | Bread          | `◉`   | `#d97706` (amber)  | 12  |
| 3    | Gold Coin      | `$`   | `#facc15` (yellow) | 99  |

> The `Glyph` / `Color` fields are plain string properties on `ItemEntry` so they appear in the Inspector — edit them on the Component to reshape the demo without touching code. Bad hex strings are swallowed silently by `Color.Parse`; the slot keeps its authored `.inv-slot` colour in that case.

## Bindings

| Element | Property | Variable | Mode | Notes |
|---|---|---|---|---|
| `Subtitle` (Text) | `Text` | `SlotCountText` | OneWay | Header subtitle reads `"X / 24 slots"`. |
| `Tooltip` (Panel) | `Visibility` | `TooltipVisible` | OneWay | Universal property; flips between `Visible` and `Hidden`. |
| `TooltipText` (Text) | `Text` | `ItemTooltip` | OneWay | Body of the tooltip card. |

## Events

| Element | Event | Mode | Handler |
|---|---|---|---|
| `BackpackGrid` (InventoryGrid) | `OnClick` | Code | `OnGridClick` (assigned in `OnStart` before `Show()`) |
| `BackpackGrid` (InventoryGrid) | `OnRightClick` | Code | `OnGridRightClick` (assigned in `OnStart` before `Show()`) |

> **Note on Code-mode wiring.** The generator emits each grid event as `[Property, Group("Events")] public Action OnGridClick { get; set; }` on the `InventoryGridFull` wrapper — **not** as a method named-resolved on the controller. The controller must explicitly assign the delegate *before* `Hud.Show()`:
>
> ```csharp
> Hud.OnGridClick      = OnGridClick;
> Hud.OnGridRightClick = OnGridRightClick;
> Hud.Show( GameObject, SuiInputMode.MouseOnly );
> ```
>
> `Show()` triggers `SyncFieldsTo`, which is the only path that copies the wrapper's delegate into the renderer Panel. Assigning after `Show()` leaves the renderer with `null` and the click silently no-ops. See the full pattern in [Events & Actions → Code mode](https://kikozl1.github.io/sbox-ui-designer/concepts/events-and-actions.html#code-mode).

### Why the per-slot wire-up lives in C\#

The matrix lets `InventoryGrid` emit `OnClick`/`OnRightClick`/`OnHover`/`OnUnhover` — but **not** `OnDoubleClick`, and there's no way to declare 24 separate event handlers (one per slot) without bloating the wrapper with 24 `Action` properties. The matrix lets `InventorySlot` emit `OnClick`/`OnRightClick`/`OnDoubleClick`/`OnHover` per slot too, but again we'd need 24×4=96 declared Actions for full coverage.

The clean V1.5 pattern (documented in the inventory-screen tutorial, *Step 9*) is:

1. Tick **Expose as Variable** on the grid in the Details panel.
2. The generator emits `@ref="BackpackGrid"` on the markup and a `public Sandbox.UI.Panel BackpackGrid { get; private set; }` field on the renderer Panel.
3. The controller reaches `Hud.View?.BackpackGrid` after the first render pass (we do this in `OnUpdate` with a one-shot `_slotsWired` flag), walks `grid.Children`, and assigns hover/click listeners to each child with the index captured by closure.

This is the only V1.5 pattern that scales beyond ~3 slots without generator bloat. The grid-level `OnClick`/`OnRightClick` Code events declared in the `.sui` are kept as a fallback for clicks that land in grid padding / gaps.

> **Known gap.** V1.5 wires `OnRightClick` to the plain `onclick` event with no mouse-button filter (PRD 21 / ISSUE D-018), so a grid-level `OnRightClick` handler also fires on left-clicks. The per-slot routing uses `onrightclick` directly which the engine supports natively — no false positives.

> **PreviewCount runtime gap (ISSUE-005).** The number badge that appears on slots in the Designer canvas is editor-paint only — the generator does NOT emit a `<label class="count">…</label>` inside the slot at runtime. To ship visible stack counts, manually add `<label class="count">@StackCount</label>` inside each `.sui-el-slot-NN` in the generated `.User.scss` (or post-compile in the `.razor`). See `docs/elements/inventory-slot.md` § *Stack count badge* for the full recipe.

## Extending it

### Using real PNG icons

The default sample uses glyph + tint instead of bitmap icons so it runs out-of-the-box. To swap in real PNGs:

1. Drop your icon PNGs into `Assets/ui/icons/` of the project consuming the sample (e.g. `Assets/ui/icons/sword.png`, `health_potion.png`, `bread.png`, `gold_coin.png`). Any 64×64 PNG works; keep them in a known folder so the path strings below resolve.
2. Open `inventory_grid_full.sui` in the **SUI Designer**, select each of the first four `InventorySlot` elements (`Slot_06`..`Slot_09` in the canvas), and set their **`PreviewIconPath`** prop to the matching asset path (e.g. `ui/icons/sword.png`). The designer canvas will show the icon immediately; the generator bakes a `background-image: url("ui/icons/sword.png")` into the slot's selector in `.User.scss` on the next Compile.
3. *(Optional — if you also want runtime icon swaps when items move between slots)*: add an `IconPath` property back to `ItemEntry` and, in `ApplySlotVisual`, set `slotPanel.Style.BackgroundImage = $"url(\"{item.IconPath}\")";` (instead of, or alongside, the glyph Label). Don't forget `slotPanel.Style.Dirty()`.
4. Strip the `Glyph` / `Color` defaults from `StartingItems` (or leave them as fallbacks — the glyph Label sits on top of the background image, so both can co-exist).

> Path strings in `PreviewIconPath` are relative to the project's `Assets/` root, exactly how `<img src>` and CSS `url()` resolve in Razor UI. Designer canvas + runtime resolve them the same way, so what you see in the canvas is what you get at runtime.

### Other ideas

- **Use real items from your game** — wire `StartingItems` to a `ScriptableObject`-style asset list or pull from a save file in `OnStart` before the first `Hud.Show()`.
- **Drag-to-rearrange slots** — track `OnMouseDown` per slot to capture the drag source, track the panel under the cursor via `Hud.View?.BackpackGrid.GetPanelAt( Mouse.Position )` on mouse-up, and swap entries in `_items`. Re-render with `Hud.RefreshView()`. See the estudonovo project's `Code/UI/InventoryUI.razor.cs` for a worked drag-and-drop reference.
- **Visible stack count badge** — patch the generated `.razor` (or use the post-compile SCSS recipe above) to render `@StackCount` inside each slot, then mutate the label text from the controller after each drop/use.
- **Equipment column + hotbar** — duplicate the grid pattern with two more InventoryGrids (one 1×4 for equipment, one 8×1 for the hotbar) on the same card. The tutorial in `docs/tutorials/inventory-screen.md` walks through the full layout.
- **Filter chips** — add a row of small `Button` elements above the grid with categories ("All / Weapons / Consumables / Junk"). Wire their `OnClick` to a controller method that hides slots whose item doesn't match the filter by toggling `slotPanel.Style.Display = DisplayMode.None`.
- **Persist across sessions** — serialise `_items` to JSON and save it to `FileSystem.Data` in `OnDestroy()`; reload in `OnStart` before the first `Hud.Show()` call so the cold-start state matches the previous session.

---

## See also

- [Read the full `inventory_grid_full` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/inventory_grid_full).
- [Showcase samples]({% link reference/showcase-samples.md %}) — the full catalog of V1.5 showcase samples.
- [Sample index]({% link reference/sample-index.md %}) — quick reference index for every sample.
- [health_bar]({% link samples/health_bar.md %}) — the OneWay Binding primitive this sample builds on for the subtitle + tooltip text.
- [counter_button]({% link samples/counter_button.md %}) — the Code-mode event primitive this sample extends with per-child wire-up.
- [toggle_pause]({% link samples/toggle_pause.md %}) — the `Visibility : bool` universal property pattern used for the tooltip card.
