# Inventory Grid (Full)

The flagship showcase of the **InventoryGrid + InventorySlot** pair plus the
**Expose-as-Variable + per-child wire-up** pattern. A 6×4 backpack lands in
the middle of the screen with four seeded items; hover surfaces a tooltip,
left-click logs a select, right-click drops the item, double-click triggers
a use — all driven from a single companion Component.

## Pitch

The previous showcase samples (`counter_button`, `toggle_pause`, `health_bar`,
`label_clock`) each demonstrate one piece of the SUI loop in isolation. This
sample wires **all of them together** on a realistic surface:

- **InventoryGrid** as the layout container, with `Columns=6 Rows=4` to
  produce a 24-cell wrapped flex grid.
- **24 InventorySlots**, four of them seeded with icons + counts so the
  canvas preview, the SCSS background-image, and the runtime view all show
  identical art on frame zero.
- **Three SUI Variables** (`SlotCountText : string`, `ItemTooltip : string`,
  `TooltipVisible : bool`) bound to a header subtitle and a footer tooltip
  panel via three `OneWay` Bindings (one of which uses the universal
  `Visibility : bool` property to hide/show the tooltip card).
- **Two grid-level Code-mode events** (`OnClick`, `OnRightClick`) declared
  in the `.sui` Events block, kept as fallbacks. The real per-slot routing
  happens in the Component after mount — see *Why the per-slot wire-up
  lives in C#* below.
- **The grid is exposed as a Variable** so `Hud.View?.BackpackGrid` is a
  real `Sandbox.UI.Panel` the controller can walk to attach hover/click
  listeners to every child slot, with the slot index captured by position
  in the children list.

## What you'll see

A dark card (640×540) appears centred on the screen behind a translucent
black scrim. The card header reads **Inventory** and a green subtitle
**`4 / 24 slots`** sits just below it. A 6×4 grid of 84-pixel slots fills
the middle of the card; the first four slots show a sword, a health potion,
a loaf of bread, and a gold coin (the rest are empty cells with a thin
border). At the bottom of the card a thin tooltip strip is hidden until you
hover an item.

Hover the sword → the tooltip strip slides into view and reads
`Iron Sword  x1`. Hover the potion → `Health Potion  x5`. Click the
potion → console logs `Select slot #1: Health Potion (x5)`. Right-click it →
the slot's icon disappears, the subtitle ticks down to `3 / 24 slots`, and
the console logs `Drop slot #1: Health Potion (x5)`. Double-click the bread →
console logs `Use slot #2: Bread`.

## How to use

1. Open `inventory_grid_full.sui` once in the **SUI Designer** window
   (`Window → Sbox UI Designer`) and hit **Compile**. This writes
   `InventoryGridFullPanel.razor` + `InventoryGridFullPanel.scss` +
   `InventoryGridFull.cs` (the wrapper) into
   `Code/Samples/InventoryGridFull/` of your project.
2. Drop `InventoryGridFullController.cs` into the same folder (or anywhere
   under `Code/`).
3. Drop four placeholder icons into `Assets/ui/icons/`:
   `sword.png`, `health_potion.png`, `bread.png`, `gold_coin.png`. Any
   64×64 PNG works — substitute paths in the `.sui` Props (or in the
   Component's `StartingItems` list) if you already have icons.
4. In any scene, add a new GameObject and attach the
   **InventoryGridFullController** Component to it.
5. Press **Play**. The inventory card appears centred. Hover, left-click,
   right-click, and double-click slots and watch the console + tooltip
   react.

The Component's `SlotCapacity` and `StartingItems` properties are exposed
in the Inspector if you want to reshape the demo from the editor without
touching code (e.g. seed 10 items, drop capacity to 12, swap icons).

## Variables

| Name | Type | Default | Role |
|---|---|---|---|
| `SlotCountText` | `string` | `"0 / 24 slots"` | Header subtitle. Controller writes `"<filled> / <capacity> slots"` after every mutation. |
| `ItemTooltip` | `string` | `""` | Tooltip text body. Controller writes `"<name>  x<count>"` on slot hover, clears on unhover. |
| `TooltipVisible` | `bool` | `false` | Flips the tooltip card's `Visibility` (OneWay, bool — the universal property). |

> **`List<ItemEntry>` is NOT a SUI Variable.** The inventory's item list
> lives entirely in C# as `private List<ItemEntry> _items` on the controller.
> V1.5 only supports primitive Variable types (`string` / `int` / `bool` /
> `float` / `Color`); custom POCO lists go in the component. The pattern
> when SUI can't model your data: SUI owns the *view state* (counts, flags,
> visible strings); the controller owns the *domain state* and pushes the
> view-facing derivatives into Variables.

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

> **Note on Code-mode wiring.** The generator emits each grid event as
> `[Property, Group("Events")] public Action OnGridClick { get; set; }`
> on the `InventoryGridFull` wrapper — **not** as a method named-resolved
> on the controller. The controller must explicitly assign the delegate
> *before* `Hud.Show()`:
>
> ```csharp
> Hud.OnGridClick      = OnGridClick;
> Hud.OnGridRightClick = OnGridRightClick;
> Hud.Show( GameObject, SuiInputMode.MouseOnly );
> ```
>
> `Show()` triggers `SyncFieldsTo`, which is the only path that copies
> the wrapper's delegate into the renderer Panel. Assigning after `Show()`
> leaves the renderer with `null` and the click silently no-ops. See the
> full pattern in
> [Events & Actions → Code mode](https://kikozl1.github.io/sbox-ui-designer/concepts/events-and-actions.html#code-mode).

### Why the per-slot wire-up lives in C\#

The matrix lets `InventoryGrid` emit `OnClick`/`OnRightClick`/`OnHover`/
`OnUnhover` — but **not** `OnDoubleClick`, and there's no way to declare
24 separate event handlers (one per slot) without bloating the wrapper
with 24 `Action` properties. The matrix lets `InventorySlot` emit
`OnClick`/`OnRightClick`/`OnDoubleClick`/`OnHover` per slot too, but again
we'd need 24×4=96 declared Actions for full coverage.

The clean V1.5 pattern (documented in the inventory-screen tutorial,
*Step 9*) is:

1. Tick **Expose as Variable** on the grid in the Details panel.
2. The generator emits `@ref="BackpackGrid"` on the markup and a
   `public Sandbox.UI.Panel BackpackGrid { get; private set; }` field on
   the renderer Panel.
3. The controller reaches `Hud.View?.BackpackGrid` after the first render
   pass (we do this in `OnUpdate` with a one-shot `_slotsWired` flag),
   walks `grid.Children`, and assigns hover/click listeners to each child
   with the index captured by closure.

This is the only V1.5 pattern that scales beyond ~3 slots without
generator bloat. The grid-level `OnClick`/`OnRightClick` Code events
declared in the `.sui` are kept as a fallback for clicks that land in
grid padding / gaps.

> **Known gap.** V1.5 wires `OnRightClick` to the plain `onclick` event
> with no mouse-button filter (PRD 21 / ISSUE D-018), so a grid-level
> `OnRightClick` handler also fires on left-clicks. The per-slot routing
> uses `onrightclick` directly which the engine supports natively —
> no false positives.

> **PreviewCount runtime gap (ISSUE-005).** The number badge that
> appears on slots in the Designer canvas is editor-paint only — the
> generator does NOT emit a `<label class="count">…</label>` inside the
> slot at runtime. To ship visible stack counts, manually add
> `<label class="count">@StackCount</label>` inside each `.sui-el-slot-NN`
> in the generated `.User.scss` (or post-compile in the `.razor`). See
> `docs/elements/inventory-slot.md` § *Stack count badge* for the full
> recipe.

## Extending it

- **Use real items from your game** — wire `StartingItems` to a
  `ScriptableObject`-style asset list or pull from a save file in
  `OnStart` before the first `Hud.Show()`.
- **Drag-to-rearrange slots** — track `OnMouseDown` per slot to capture
  the drag source, track the panel under the cursor via
  `Hud.View?.BackpackGrid.GetPanelAt( Mouse.Position )` on mouse-up, and
  swap entries in `_items`. Re-render with `Hud.RefreshView()`. See the
  estudonovo project's `Code/UI/InventoryUI.razor.cs` for a worked
  drag-and-drop reference.
- **Visible stack count badge** — patch the generated `.razor` (or use
  the post-compile SCSS recipe above) to render `@StackCount` inside
  each slot, then mutate the label text from the controller after each
  drop/use.
- **Equipment column + hotbar** — duplicate the grid pattern with two
  more InventoryGrids (one 1×4 for equipment, one 8×1 for the hotbar)
  on the same card. The tutorial in
  `docs/tutorials/inventory-screen.md` walks through the full layout.
- **Filter chips** — add a row of small `Button` elements above the
  grid with categories ("All / Weapons / Consumables / Junk"). Wire
  their `OnClick` to a controller method that hides slots whose item
  doesn't match the filter by toggling `slotPanel.Style.Display =
  DisplayMode.None`.
- **Persist across sessions** — serialise `_items` to JSON and save it
  to `FileSystem.Data` in `OnDestroy()`; reload in `OnStart` before the
  first `Hud.Show()` call so the cold-start state matches the previous
  session.
