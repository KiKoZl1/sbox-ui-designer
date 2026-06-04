---
layout: default
title: Installation
parent: Getting Started
nav_order: 1
---

# Installation
{: .no_toc }

Install SUI Designer into an s&box project as a library (addon).
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Prerequisites

Before installing, make sure you have:

| Requirement | Notes |
|---|---|
| **s&box editor** | Minimum tested build: `s&box-dev 1.0.1+50a05caa8fe89592` (snapshot 2026-05-06, the V1.5 baseline pin recorded in [`README.md`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/README.md#engine-compatibility)). Newer dev builds usually work; Editor APIs are not version-stable, so very old builds may fail to load editor widgets. |
| **An s&box project** (`.sbproj`) | SUI Designer is installed as a library *inside* a host project's `Libraries/` folder — there is no standalone install. |
| **`local.dooeditor`** | Optional but recommended companion library. Required if you want **Doo** graph bodies for events; **Code** mode events work without it. See [Required companion library: dooeditor](#required-companion-library-dooeditor) below. |
| **Git** (optional) | Only needed for Option 1 (clone) and `git pull` updates. Option 2 below covers the no-git path. |

## Option 1 — Clone the repository

Recommended for development or staying on the bleeding edge.

```bash
# From your project's root (where the .sbproj lives):
cd Libraries
git clone https://github.com/KiKoZl1/sbox-ui-designer.git kikozl.sbox_ui_designer
```

The folder name **must be `kikozl.sbox_ui_designer`** — the `<org>.<ident>` form expected by s&box's library mounter. Mismatched names won't load.

Restart the s&box editor. The library should appear under **Library Manager**.

## Option 2 — Manual download

If you don't have git:

1. On GitHub, click the green **Code** button → **Download ZIP**.
2. Extract the ZIP.
3. Rename the top-level folder from `sbox-ui-designer-main` to **`kikozl.sbox_ui_designer`**.
4. Move the folder into your project's `Libraries/` directory.
5. Restart the s&box editor.

## Required companion library: dooeditor

V1.5 events can be wired to a visual [Doo](https://docs.facepunch.com/s/sbox-dev/doc/doo) graph stored inside the `.sui` document. The editor UI for Doo bodies is provided by Facepunch's `local.dooeditor` library, which SUI Designer declares as an `EditorReferences` entry in its [`sbox_ui_designer.sbproj`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/sbox_ui_designer.sbproj):

```json
"EditorReferences": [
  "local.dooeditor"
]
```

The `local.` prefix is the s&box convention for libraries the engine resolves from the host project's `Libraries/` folder (as opposed to a published `org.ident` package fetched from sbox.game). Concretely, the editor expects to find:

```
YourProject/
└── Libraries/
    └── local.dooeditor/
        └── dooeditor.sbproj
```

Doo and `local.dooeditor` ship with Facepunch's official sample projects — if you already have a recent s&box editor install with the sample/template projects, the easiest path is to copy that `local.dooeditor` folder into your project's `Libraries/`. Check the Facepunch [Doo documentation](https://docs.facepunch.com/s/sbox-dev/doc/doo) for the current canonical source.

Without `local.dooeditor` installed, the **Events** Details section still works in **Code** mode, but the **Doo** mode picker will be unavailable and any existing Doo bodies in a `.sui` document won't be editable from the designer.

## Verify the install

After restart, in the s&box editor:

1. Open **File → Library Manager** — `kikozl.sbox_ui_designer` should be listed and enabled.
2. Open the **Asset Browser** → right-click any folder → **New** — you should see **Sbox UI Document** under the UI category.
3. Library Manager also lists `local.dooeditor` (or you accept that Doo-mode events will be unavailable).

If the entry is missing, the library failed to load. Check:

- Folder name is exactly `kikozl.sbox_ui_designer` (case-sensitive).
- The folder contains a `sbox_ui_designer.sbproj` at its root.
- The project compiles cleanly (open **Output** → **Console** in s&box to see compile errors).

## Update / sync

If you cloned via git:

```bash
cd Libraries/kikozl.sbox_ui_designer
git pull
```

Restart the editor (or trigger a hot-reload by editing any `.cs` in the addon).

### Upgrading from V1.0

If your project has `.sui` documents from V1.0, the first time you open any `.sui` in the V1.5 editor an upgrade prompt appears offering to migrate every document at once. See the [V1.0 → V1.5 upgrade guide]({% link UPGRADE_V1_0_TO_V1_5.md %}) for the full procedure.

## Project files

After install, your project should contain:

```
YourProject/
├── YourProject.sbproj
└── Libraries/
    └── kikozl.sbox_ui_designer/
        ├── sbox_ui_designer.sbproj      # library manifest
        ├── Code/                        # runtime classes (SuiDocument, SuiPreviewMount, generator)
        ├── Editor/                      # editor-only classes (window, canvas, details panel, …)
        ├── Assets/sui_preview/          # bundled scene used by Test in Play
        ├── docs/                        # this site
        ├── samples/ui/                  # reference .sui samples
        └── README.md
```

## What gets installed where

| Folder | Contents | Notes |
|---|---|---|
| `Code/Runtime/` | `SuiAsset`, `SuiDocument`, `SuiPanel<TView>`, `SuiHostPanelComponent`, `SuiAssetRegistry`, `SuiVariable`, `SuiBinding`, `SuiBuiltinConverters`, `SuiConverterCatalog`, `SuiBindingModeMatrix`, schemas, preview helpers | Compiled into every game that mounts the library |
| `Code/Generation/` | Razor + SCSS + wrapper emitters | Editor-only |
| `Editor/` | `SuiDesignerWindow`, `SuiCanvasViewport`, `SuiPreviewLauncher`, all widgets and commands | Editor-only — stripped from packaged games |
| `Assets/sui_preview/` | `preview_stage.scene` | Used by **Test in Play** workflow |

{: .note }
The runtime `Code/` is light — only what's needed at game runtime. Editor-only code is fenced behind the `Editor/` folder convention so it doesn't pad packaged game assemblies.

## Removing the library

Delete the `Libraries/kikozl.sbox_ui_designer/` folder. Any `.sui` files in your project will become orphaned (the engine won't know how to open them), but the underlying JSON survives.

## Next steps

- [Your first UI]({% link getting-started/your-first-ui.md %}) — build a HUD from scratch
- [Test in Play]({% link getting-started/test-in-play.md %}) — preview your UI on a real player
