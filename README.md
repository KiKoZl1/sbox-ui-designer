# Sbox UI Designer

Visual UI Designer for [s&box](https://sbox.game). Author UI layouts in a UMG-like editor and generate native `.razor` and `.razor.scss` files. The `.sui` document is the source of truth; generated files are disposable; user-owned files are protected.

**Status:** M0–M12 done, M13 polish in progress. See [`STATUS.md`](STATUS.md) and [`ISSUES.md`](ISSUES.md) for current state.

---

## Quick start

1. **Install the library** — clone this repo into your s&box project's `Libraries/` folder, or pull as a published package. Editor picks it up on next launch.
2. **Open the editor in s&box** — File → New Project → ensure your `.sbproj` references the library.
3. **Create your first .sui** — right-click in Asset Browser → New → **Sbox UI Document**. Name it (e.g. `InventoryUI`).
4. **Open it** — double-click. The Sbox UI Designer window appears.
5. **Drag elements from the Palette onto the canvas** OR click to add them at root.
6. **Edit properties in the Details panel** on the right (anchor, position, size, color, text content, etc.).
7. **Click Compile** in the toolbar (or `Ctrl+B`). First time: a folder picker asks where the generated files should land (typically `Code/UI/` or similar inside your project).
8. **The engine hot-loads the new `.razor` + `.razor.scss`** — your `PanelComponent` type is now available to mount in your scene.
9. **Use the Preview button** to see the live render in a modal without leaving the designer.

---

## Concepts

- **`.sui` is the source of truth.** All edits go here. The schema is documented in [`docs/prd/05_sui_asset_and_schema.md`](docs/prd/05_sui_asset_and_schema.md).
- **`.razor` / `.razor.scss` are generated.** Each generated file carries a `SUI:GENERATED` header so the compile pipeline can detect manual edits and refuse to overwrite them.
- **Manifest tracks ownership.** `<output>/.sui-manifest/<DocumentId>.json` records which files this document owns + their hashes at last write. Recompile-with-changes triggers a backup before overwrite, recompile-without-changes is a no-op (Skipped).
- **Backups are outside `Code/`.** Backups land in `<projectRoot>/.sui-backups/<DocName>/<UTC-timestamp>/...` so the engine never compiles them as duplicate `partial class` declarations.
- **Preview cache is separate from final output.** Preview cache lives at `<projectRoot>/Code/_sui_preview/<ClassName>/` and uses a sub-namespace `.SuiPreview` to avoid colliding with the final-compiled type.

---

## Sample documents

The Tools menu has **Install Sample Documents** which writes 4 canonical samples into `Assets/SuiSamples/`:

- **`simple_panel.sui`** — minimal panel + centered text. Anchor + pivot basics.
- **`inventory_basic.sui`** — InventoryGrid + 15 InventorySlot composition.
- **`hotbar_basic.sui`** — bottom-anchored Hotbar with 9 slots, first one highlighted.
- **`hud_survival.sui`** — composite top-left HUD with Health / Stamina / Hunger ProgressBars + labels.

Open any of them to see the schema in action.

---

## Editor walkthrough

### Toolbar (top)
- **Save** (`Ctrl+S`)
- **Compile** (`Ctrl+B`) — runs validator + generator + writes to disk + updates manifest
- **Preview** — opens a modal window with the live runtime render
- **Undo** (`Ctrl+Z`) / **Redo** (`Ctrl+Y`)

### Canvas toolbar (inline, top of canvas)
- **Screen size** dropdown — preview resolution (1080p / 720p / 1440p / 4K / 16:10 / Ultrawide / custom Base)
- **Zoom** dropdown — 25%–400%, also via mouse wheel
- **Snap** dropdown — off / 4 / 8 / 16 / 32 / 64 px
- **Grid** toggle — dot overlay at snap intervals
- **Rulers** toggle — pixel rulers on top + left
- **Fit** button — center+scale canvas to viewport (`Ctrl+0`)

### Palette (left top)
Click an item to add it at root, or drag onto the canvas to drop on a container.

### Hierarchy (left bottom)
- Click to select, F2 to rename
- Right-click for context menu (Add Child / Duplicate / Delete / Move Up/Down)
- Drag-and-drop to reparent or reorder

### Details (right)
Property editor for the selected element. Sections collapse/expand. All edits route through the controller's command stack so Ctrl+Z restores any change.

### Bottom panel
- **Compile Results** — categorized: Generated / Skipped / Preserved (with backup path) / Conflicts / Obsolete
- **Animations** / **Bindings** — placeholders, V2

---

## Keyboard shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+S` | Save |
| `Ctrl+B` | Compile |
| `Ctrl+Z` / `Ctrl+Y` | Undo / Redo |
| `Ctrl+C` / `Ctrl+V` / `Ctrl+X` | Copy / Paste / Cut subtree |
| `Ctrl+D` | Duplicate |
| `Del` | Delete |
| `F2` | Rename |
| `Ctrl++` / `Ctrl+-` | Zoom in / out |
| `Ctrl+0` | Fit to screen |

### Canvas mouse
- **Wheel** — zoom anchored at cursor
- **Middle-mouse drag** OR **Alt+Left drag** — pan
- **Left click** — select element (deepest first; if currently selected element is ancestor of hit, keep the ancestor)
- **Drag selected element body** — move
- **Drag corner/edge handles** — resize. Hold **Shift** to lock to dominant axis. Hold **Ctrl** on corner handles for aspect-ratio lock.
- **Hold Alt during drag** — bypass snap-to-grid
- **Drag empty area** — marquee select; Shift adds to current selection
- **Shift+click** — toggle element in/out of multi-selection

---

## Troubleshooting

### CS0111 "Type already defines a member called BuildRenderTree"

The engine sees two `partial class <Name>` declarations. Common causes:

1. **Both preview cache and final output exist with same namespace** — fixed in v0.1.0+ (preview now uses `.SuiPreview` sub-namespace). If you're upgrading, `Tools > Clean All SUI Caches` flushes the legacy cache.
2. **Backups inside `Code/`** from a pre-fix compile — same Tools menu cleans them. New backups go to `<projectRoot>/.sui-backups/`.
3. **You manually copied a generated file somewhere else** — search `Code/` for orphan `<ClassName>.razor` and remove duplicates.

### Compile says "Conflict: file exists without ownership match"

A `.razor` file at the output path doesn't have our `SUI:GENERATED` header (or has another document's header). The compile refuses to touch it. Either:
- Move the file out of the way (rename + recompile)
- Manually delete it (if you don't need its content)
- Change the output folder for this `.sui` (File → Change Output Folder…)

### Preview window is blank

- Check Compile Results dock for errors
- Try `Tools > Regenerate Preview` to force a cache rewrite
- Try `Tools > Clean Preview Cache` then click Preview again

---

## Engine compatibility

Targeted s&box version (baseline pin):

```
s&box-dev 1.0.1+50a05caa8fe89592   (snapshot 2026-05-06)
```

Editor APIs (`Editor.*`) are not version-stable across releases. Runtime UI generation is unlikely to break with newer builds, but the editor shell may need tweaks. Bumping the pin is a deliberate decision.

---

## Layout

```
Code/                          (s&box compilable code root)
  Runtime/                     POCO schema + validator + generator support
  Generation/                  Razor + SCSS generator
Editor/                        (s&box editor code root)
  Canvas/                      Paint-based design canvas (renderer, solver, viewport, toolbar)
  Commands/                    Undo/redo command stack
  Widgets/                     Palette, Hierarchy, Details, CompileResults
samples/ui/                    sample .sui files (also installable via Tools menu)
docs/prd/                      technical PRDs (15 numbered documents)
sbox_ui_designer.sbproj        library manifest
STATUS.md                      current implementation state
ISSUES.md                      known unresolved issues
MILESTONE_M14_REDESIGN.md      planned UI/UX redesign milestone
```

---

## License

TBD (open source vs. paid distribution still being decided).
