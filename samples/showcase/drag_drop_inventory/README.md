# Drag-Drop Inventory

A real **drag-and-drop inventory manager** showcase: two 4×4 grids side-by-side, eight starter items spread between them, and the canonical "mouse-down on item → ghost follows cursor → mouse-up on slot to swap" interaction every survival/RPG game needs.

This is the SUI Designer's heaviest runtime-rendering sample after `inventory_grid_full` — it stress-tests four corners of the pipeline at once:

1. **Multiple `ExposeAsVariable` Panels** (`BackpackGrid`, `StashGrid`, `DragGhost`) — every one needs its `@ref` captured before any runtime mutation works; if the wrong one captures the wrong slot index, the whole drop logic falls over.
2. **Runtime-added child Panels inside a Flex-Row-Wrap parent.** The grid containers are `Mode=Absolute` (positioned inside the card) but their children must flow as a wrapping grid — exercises the codegen `flex-direction`/`flex-wrap` emission fix from v1.5 (without that fix the children stack horizontally and shoot off the right edge).
3. **Mouse position math during native drag.** `Panel.MousePosition` freezes once the engine claims the drag operation, so the ghost reads `Sandbox.Mouse.Position` directly and multiplies by `ScaleFromScreen` to convert raw screen pixels into the ghost's local panel coordinates.
4. **Per-element `AddEventListener` on dynamically-created Panels.** Every one of the 32 runtime slots wires `onmousedown`/`onmouseup` against a captured `(side, index)` tuple — a deliberate stress on the closure-capture in C# loops *and* on Sandbox.UI's event bus when many listeners share the same delegate shape.

## Behavior

1. **Mount.** A 600×400 dark card with two sub-panels appears centred on screen. Title `Inventory Manager` at the top, then `BACKPACK` on the left + 4×4 grid below, `STASH` mirror on the right. 8 seeded items (Sword, Potion, Gold, Helmet, Shield, Bow, Gem, Scroll) are placed across both grids with empty slots between them so there's room to drag into.
2. **Click + hold** on any occupied slot. The drag state machine flips on, the `DragGhost` Panel (a 56×56 floating clone) becomes visible, and an 85% opaque preview of the picked-up item starts tracking the cursor.
3. **Move the mouse.** Each `OnUpdate` reads `Sandbox.Mouse.Position * ghost.ScaleFromScreen` and writes the result minus `HalfSlot` into `ghost.Style.Left/Top` so the cursor sits in the centre of the preview.
4. **Release on any slot.** That slot's `onmouseup` fires first (Sandbox.UI bubbles child → parent), the controller swaps the source and target items in the backing arrays, calls `Rerender()` to rebuild both grids, and hides the ghost.
5. **Release outside any slot.** Only the root-level `onmouseup` fires. The controller cancels the drag, hides the ghost, leaves the inventories untouched.

## Items

| Letter | Kind | Colour |
|---|---|---|
| `S` | Sword | `#ef4444` red |
| `P` | Potion | `#10b981` emerald |
| `G` | Gold | `#fbbf24` amber |
| `H` | Helmet | `#3b82f6` blue |
| `Sh` | Shield | `#a78bfa` violet |
| `B` | Bow | `#84cc16` lime |
| `Gm` | Gem | `#ec4899` pink |
| `Sc` | Scroll | `#e5e7eb` light grey |

All visuals are pure CSS — no PNG/VTEX assets. Replacing `GetItemVisual` with an `ImagePath` lookup is one of the "Extending it" ideas below.

## How to use

1. Open `drag_drop_inventory.sui` in the **SUI Designer** window (`Window → Sbox UI Designer`) and hit **Compile** (Force Regen). This emits `DragDropInventoryPanel.razor` + `.scss` + the `DragDropInventory.cs` wrapper into `Code/Samples/DragDropInventory/` under namespace `Sandbox.Samples`.
2. Add the runtime SCSS that styles the dynamically-created slot Panels — see **Required `User.scss` rules** below.
3. Drop `DragDropInventoryController.cs` into `Code/Samples/DragDropInventory/` (or anywhere under `Code/`).
4. In any scene, attach `DragDropInventoryController` to a GameObject and hit **Play**.

## Required `User.scss` rules

The SUI compiler creates `DragDropInventoryPanel.User.scss` on first Compile and never touches it again — drop the following inside it so the runtime-added slot Panels look right:

```scss
DragDropInventoryPanel {
    .sui-el-backpack-grid,
    .sui-el-stash-grid {
        > .drag-slot {
            // Codegen emits Width/Height per slot from C#, but flex-grow:0
            // and flex-shrink:0 aren't reliably exposed as runtime Style
            // props in every Sandbox.UI build — keep them in CSS so the
            // wrapping grid doesn't stretch the last slot of each row.
            flex-grow: 0;
            flex-shrink: 0;
            background-color: #1f2230;
            border: 1px solid #2a2d3d;
            border-radius: 4px;
            justify-content: center;
            align-items: center;
            transition: background-color 0.12s, transform 0.12s;
        }
        > .drag-slot.occupied {
            // BackgroundColor is set per-item in C# — override the empty-slot
            // border so occupied slots feel "filled" not just tinted.
            border-color: rgba( 255, 255, 255, 0.18 );
            cursor: pointer;
        }
        > .drag-slot:hover {
            transform: scale( 1.04 );
        }
    }

    .drag-item-letter {
        color: #ffffff;
        font-size: 22px;
        font-weight: bold;
        text-align: center;
    }
}
```

## Controller architecture

`DragDropInventoryController` keeps two `ItemKind[16]` arrays (`_backpack`, `_stash`) as the source of truth. The flow:

- **`OnStart`** mounts the panel with `SuiInputMode.All` (the grids need cursor input for mouse events) and seeds 4 items per grid.
- **`OnUpdate`** runs two one-shot bootstraps (initial render + root `onmouseup` listener) and, while a drag is in flight, updates the ghost position from `Sandbox.Mouse.Position * ghost.ScaleFromScreen`.
- **`RenderGrid`** wipes the grid container, then creates 16 runtime `Panel` children: each gets `Width/Height = 56px`, the `drag-slot` class, item visuals via `UpdateSlotVisual`, and a single `onmousedown` `AddEventListener` capturing `(side, index)`.
- **`UpdateSlotVisual`** mutates a single slot's visuals (children + class + background colour) WITHOUT destroying the Panel — critical so the engine can still route the eventual `onmouseup` to the same Panel reference that received `onmousedown`.
- **`OnSlotMouseDown`** captures the source `(side, index, item)` and shows the ghost. The source slot is intentionally NOT mutated during the drag (any visual change to the source mid-gesture breaks the engine's mouseup routing — confirmed empirically).
- **`OnRootMouseUp`** is the single drop point. It hit-tests every slot via `Panel.IsInside(Sandbox.Mouse.Position)` to find the target (mouseover events are silenced during the drag so we can't track hover), guards same-source-and-target as a no-op (otherwise the dual write to one cell would vanish the item), then swaps and updates both slots' visuals.

## Troubleshooting

Drag-and-drop in Sandbox.UI has several non-obvious gotchas — these are the ones that bit us first.

| Symptom | Cause | Fix |
|---|---|---|
| Mouseup never fires on the source slot — drag picks up but drop silently does nothing | The controller mutated the source slot's `Style` / class / children during the drag (even a deferred `OnUpdate` tint). Sandbox.UI routes `onmouseup` to the exact `Panel` instance that received `onmousedown`; any mid-gesture mutation desyncs the engine's internal tracking and the mouseup is dropped. | NEVER touch the source slot between `OnSlotMouseDown` and `OnRootMouseUp`. The ghost Panel is the only visual feedback during the drag — leave the source slot frozen. |
| Cannot tell which slot the cursor is hovering during the drag — `onmouseover` listeners on other slots stay silent | Sandbox.UI silences `onmouseover` / `onmouseout` on sibling Panels for the duration of a native drag (only the captured source Panel sees pointer events). Hover-tracking-based drop detection is impossible. | Hit-test live on mouseup instead: iterate every slot Panel in `OnRootMouseUp` and call `slot.IsInside( Sandbox.Mouse.Position )` to find the target. No hover state needed. |
| Dropping an item onto its own slot makes the item vanish entirely | The swap writes `source := targetItem` then `target := dragItem`; when source and target alias the same array cell, the second write commits `ItemKind.None` (the value the first write just placed) and the original item is lost. | Guard with `if ( sourceArr == targetArr && srcIdx == tgtIdx ) return;` before any array write in `OnRootMouseUp`. Treat same-slot drop as a no-op. |
| Ghost preview lags behind / stays pinned to the slot where the drag started | `Panel.MousePosition` is frozen by the engine for the duration of a native drag — it keeps returning the position at mousedown. Per-frame ghost updates that read it never move. | Read `Sandbox.Mouse.Position` (raw screen pixels) in `OnUpdate`, multiply by `ghost.ScaleFromScreen` to convert to ghost-local coords, subtract `HalfSlot`, and write into `ghost.Style.Left/Top` every frame. |
| Controller fails to compile — `DragDropInventoryPanel` type does not exist | The wrapper class and `.razor` / `.scss` are emitted by the SUI Designer on Compile (Force Regen) — until that runs once, the type the controller references is just a missing symbol. | Open `drag_drop_inventory.sui` in the SUI Designer window and hit **Compile** before building. First compile generates the wrapper; subsequent edits to the `.sui` regenerate it. |

## Extending it

- **PNG icons instead of letters.** Replace the letter `Label` inside each occupied slot with `slotPanel.AddChild<Image>()` and set `image.SetTexture( ... )`. Items become asset references on the controller.
- **Item stacking.** Promote `ItemKind` to a `struct ItemStack { Kind Kind; int Count; }` and render a small count label in the bottom-right of occupied slots when `Count > 1`. Drag-onto-same-kind merges stacks up to a cap.
- **Right-click to delete.** Add `slotPanel.AddEventListener( "onrightclick", () => Delete( side, index ) )` and clear that cell.
- **Network sync.** Promote both arrays to `[Sync] NetList<ItemKind>` on a networked `Component`, and call `[Rpc.Broadcast] Swap(side1, idx1, side2, idx2)` from `OnSlotMouseUp`. Every client renders from the authoritative state.
- **Constraints.** Add `ItemKind[] AllowedKinds` per grid (e.g. "Stash only accepts Currency") and short-circuit `OnSlotMouseUp` when the target doesn't accept the dragged item.
- **Drag-from-empty insertion.** A "shop" or "loot pool" panel becomes the source for new items via the same handler shape — pretend the slot is occupied with a virtual item.
