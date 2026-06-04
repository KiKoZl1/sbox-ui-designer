---
layout: default
title: Overview
parent: Architecture
nav_order: 1
---

# Architecture overview
{: .no_toc }

How SUI Designer is wired internally — from `.sui` file to running ScreenPanel.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The big picture

```
   ┌─────────────────┐   asset open     ┌──────────────────────┐
   │  .sui file      │ ───────────────▶│  SuiAsset            │
   │  (JSON on disk) │   asset save     │  (GameResource)      │
   └─────────────────┘ ◀───────────────└──────┬───────────────┘
                                              │ Document
                                              ▼
                                   ┌──────────────────────────┐
                                   │  SuiDesignerController   │
                                   │  • Document              │
                                   │  • Selection             │
                                   │  • Dirty flag            │
                                   │  • Command stack         │
                                   └────┬─────────────────────┘
                                        │
        ┌───────────────────────────────┼───────────────────────────────┐
        │                               │                               │
        ▼                               ▼                               ▼
 ┌─────────────┐                ┌──────────────┐                ┌─────────────┐
 │ Hierarchy   │                │ Canvas       │                │ Details     │
 │ widget      │                │ widget       │                │ widget      │
 └─────────────┘                └──────┬───────┘                └─────────────┘
                                       │
                                       │ paint
                                       ▼
                            ┌─────────────────────┐
                            │ SuiCanvasRenderer   │ ─── Editor.Paint (Qt)
                            │ + SuiLayoutSolver   │
                            │ + SuiFlexLayout     │
                            └─────────────────────┘

                                       │ Ctrl+B
                                       ▼
                            ┌─────────────────────┐
                            │ SuiGenerationPipeline│
                            │ → Razor + SCSS      │
                            └──────┬──────────────┘
                                   │
                       ┌───────────┴────────────┐
                       ▼                        ▼
            ┌───────────────────┐   ┌─────────────────────┐
            │ SuiCompileWriter  │   │ SuiPreviewCacheWriter│ ── Test in Play
            │ → Code/UI/        │   │ → Code/_sui_preview/ │
            └───────────────────┘   └─────────────────────┘
```

## Subsystems

### Persistence (`Code/Runtime/`)

`.sui` files are persisted via `SuiAsset` — a Sandbox `GameResource`. The asset holds a single `SuiDocument` which is the in-memory representation: a header (id, name, schema version), a list of `SuiElement` nodes (flat with `ParentId`), and per-document settings (canvas size, output folder, generated-file manifest).

Read more: [Document model]({% link architecture/document-model.md %}).

### Controller (`Editor/SuiDesignerController.cs`)

Single source of truth while a document is open. Owns:

- The active `SuiDocument`
- The selection (single + multi-set)
- The dirty flag
- The command stack (`SuiCommandStack`)

Widgets read state via the controller and mutate it ONLY through `Execute(ISuiCommand)`. This keeps undo, dirty-tracking, and event propagation coherent — no widget directly pokes another widget. Events the controller fires:

- `DocumentChanged` — document was loaded/swapped
- `SelectionChanged` — selection set or primary changed
- `DirtyChanged` — dirty flag flipped
- `CommandsChanged` — undo/redo state changed

### Commands (`Editor/Commands/`)

Every mutation against the document is an `ISuiCommand`. The stack stores Apply/Undo pairs so Ctrl+Z works for free. See [Undo / Redo and commands]({% link workflows/undo-redo-commands.md %}).

### Widgets (`Editor/Widgets/`)

UI components that the designer window docks together. Each widget reads from the controller and emits commands. Notable widgets:

- `SuiHierarchyWidget` — left tree
- `SuiPaletteWidget` — palette of element types
- `SuiCanvasWidget` — canvas viewport + interaction
- `SuiDetailsWidget` — right inspector
- `SuiCompileResultsWidget` — bottom tab showing generator output
- `SuiCodeTabWidget` — code preview tab (Razor + SCSS read-only)

### Canvas (`Editor/Canvas/`)

The visual surface. Two layers:

1. **Layout** — `SuiLayoutSolver` (Absolute / Anchor / Pivot math) + `SuiFlexLayout` (flex container math). Produces a final `Rect` for every element relative to the canvas.
2. **Renderer** — `SuiCanvasRenderer` walks the resolved tree and paints via `Editor.Paint` (Qt). Independent of the runtime CSS render — uses the same SCSS values but its own implementation.

Read more: [Canvas renderer]({% link architecture/canvas-renderer.md %}) · [Layout solver]({% link architecture/layout-solver.md %}).

### Generator (`Code/Generation/`)

Pure functions: take a `SuiDocument` + `SuiGenerationMode`, return an in-memory `SuiGenerationResult` (Razor + SCSS + **wrapper C#** + diagnostics). Does not touch the filesystem.

V1.5 emits four artefacts per `.sui`:

1. **`<Name>Panel.razor`** — the `Panel`-derived renderer (renamed from `<Name>` per DEVIATIONS D-014).
2. **`<Name>.razor.scss`** — stylesheet.
3. **`<Name>.cs`** — the wrapper class extending `SuiPanel<<Name>Panel>` with `Add` / `Show` / `Hide` / `Remove`, per-Variable `[Property]` mirrors, named-instance fields for embedded SuiReferences, an `Apply` namespace for `Manual` triggers, and a recursive `ContentHash` (DEVIATIONS D-015).
4. **`<Name>.User.scss`** sidecar (created once if missing, then never overwritten).

Two modes:

- `Final` — emits `@import "<ClassName>.User.scss"` at the bottom. For real compile.
- `Preview` — no User import. For Test in Play.

Read more: [Generator pipeline]({% link architecture/generator.md %}) and [Wrapper generation]({% link concepts/wrapper-generation.md %}).

### Compile writer (`Editor/SuiCompileWriter.cs`)

Takes the generator result and writes to disk under the user-chosen output folder. Owns:

- File ownership via `SUI:GENERATED` header
- The User.scss sidecar (created once, never overwritten)
- Backups under `.sui-backups/` outside `Code/`
- The manifest at `.sui-manifest/<DocumentId>.json`

Read more: [Compile writer]({% link architecture/compile-writer.md %}).

### Preview system (`Editor/SuiPreviewLauncher.cs` + `Code/Runtime/SuiPreview*.cs`)

Test in Play workflow:

1. Generator runs in `Preview` mode.
2. `SuiPreviewCacheWriter` writes to `Code/_sui_preview/<ClassName>/`.
3. Engine hot-reloads → the generated `Panel` subclass (V1.5-M2-K7: `@inherits Panel`, NOT `PanelComponent`) registers in `TypeLibrary`.
4. Launcher polls for the type, sets `SuiPreviewState.PendingTypeFullName`.
5. Opens `sui_preview/preview_stage.scene` and plays it.
6. `SuiPreviewMount` (in-scene component) reads the state in `OnStart`, spawns a `ScreenPanelHost` child GO via `Scene.CreateObject(true)`, adds a `ScreenPanel` + a `SuiHostPanelComponent`, instantiates the generated `Panel` via `TypeDescription.Create`, and attaches it via `hostComponent.Panel.AddChild(renderedPanel)`.

Read more: [Preview system]({% link architecture/preview-system.md %}).

## File / namespace map

| Folder | Namespace | Contents |
|---|---|---|
| `Code/Runtime/` | `SboxUiDesigner.Runtime` | Document model, asset, validator, enums, preview mount/state |
| `Code/Generation/` | `SboxUiDesigner.Generation` | Pipeline, Razor + SCSS emitters, allowed-property list |
| `Editor/` | `SboxUiDesigner.EditorUi` | Window, controller, compile writer, preview launcher |
| `Editor/Canvas/` | `SboxUiDesigner.EditorUi.Canvas` | Layout solver, flex layout, renderer, viewport, error banner |
| `Editor/Commands/` | `SboxUiDesigner.EditorUi.Commands` | Command interface + concrete commands |
| `Editor/Widgets/` | `SboxUiDesigner.EditorUi.Widgets` | All dockable widgets |
| `Editor/Tools/` | `SboxUiDesigner.EditorUi.Tools` | Tools menu actions (Clean caches, etc.) |

### Asset Registry — runtime vs editor split (V1.5)

`SuiAssetRegistry` (`Code/Runtime/`) keeps a stable GUID → path/name map of every `.sui` in the project. Drives:

- The dynamic **USER WIDGETS** palette category (DEVIATIONS D-002).
- The **SuiReference** resolver — looks up the target child by stable GUID.
- Cascade compile — when a child compiles, every parent that depends on it is recompiled.

**Why the split exists.** PRD-22 originally planned a single registry that walked the filesystem itself. The s&box runtime sandbox blocks `System.IO`, so any type that calls `Directory.EnumerateFiles` / `File.ReadAllText` / `FileSystemWatcher` cannot ship in `Code/Runtime/`. M0 DEVIATIONS D-004 therefore split the design into two cooperating types:

**Runtime — `SuiAssetRegistry` (`Code/Runtime/SuiAssetRegistry.cs`)**
Sandbox-clean. Zero `System.IO`. The whole surface is in-memory bookkeeping + serialization through strings:

- `AddOrUpdate(guid, projectRelativePath, name, lastSeenIso = null)` — insert or replace an entry.
- `UpdatePath(guid, newProjectRelativePath)` — rename without re-keying GUIDs.
- `RemoveByPath(projectRelativePath)` — drop the entry whose path matches (file deleted).
- `ToJson()` / `LoadFromJson(json)` — IO-free persistence; the caller decides where the string comes from / goes to.
- Events (`Added` / `Updated` / `Removed`) — multicast to downstream consumers (palette, reference resolver, cascade compile).

**Editor — `SuiAssetRegistryService` (`Editor/SuiAssetRegistryService.cs`)**
The disk layer for the registry. Singleton, lazily initialised on first SUI Designer window open via `EnsureInitialized()` (idempotent). Owns:

- The **cold build** — walks `<projectRoot>` for `.sui` files, reads every header, and pushes each one into the runtime registry via `AddOrUpdate`.
- The **JSON cache** at `<projectRoot>/.sui-cache/asset-registry.json` — persisted via `Registry.ToJson()`; rehydrated via `Registry.LoadFromJson(...)` on startup when fresh.
- The **file-system watcher** (`SuiAssetRegistryWatcher`) — translates `Created` / `Changed` / `Renamed` / `Deleted` events into `AddOrUpdate` / `UpdatePath` / `RemoveByPath` calls.

**Hand-off contract on editor startup.** First SUI Designer window open ⇒ `SuiAssetRegistryService.Instance.EnsureInitialized()` ⇒ resolve project root ⇒ `TryLoadCache()` rehydrates the runtime registry from `.sui-cache/asset-registry.json` (or runs a cold build if missing / stale) ⇒ `StartWatcher()` so subsequent disk edits flow `FileSystemWatcher → Service → Registry.AddOrUpdate/RemoveByPath/UpdatePath → events → palette + resolver + cascade compile`. Game-build code only ever sees the runtime registry — it never compiles against the service.

## Why this split?

- `Code/Runtime/` is the only namespace allowed to ship in the game build. Anything in `Editor/*` is gated behind `#if EDITOR` style restrictions (Sandbox auto-excludes Editor code from the runtime DLL).
- `Code/Generation/` is also runtime-safe — it has no Editor dependencies — so it can be reused if SUI ever needs to generate at runtime (currently it doesn't, but the boundary is preserved).
- The controller owns the document and the command stack. Widgets are thin views over that state. This is what makes undo, dirty tracking, and selection multicast work cleanly.

## Performance budgets

Soft release-time gates ratified across milestones. Exceeding one flags a regression worth investigating; it does **not** block release on its own.

| Surface | Budget |
|---|---|
| Canvas paint | ≤ 8 ms |
| Layout solver (200 elements) | ≤ 4 ms |
| Single-document compile | ≤ 200 ms |
| 5-level reference-tree compile | ≤ 1 s |
| `BuildHash` (recursive content hash) | ≤ 0.5 ms |
| Asset Registry cold build (500 docs) | ≤ 1 s |

## See also

- [Document model]({% link architecture/document-model.md %}) — the data
- [Generator pipeline]({% link architecture/generator.md %}) — `.sui` → Razor + SCSS
- [Compile writer]({% link architecture/compile-writer.md %}) — writing to disk safely
