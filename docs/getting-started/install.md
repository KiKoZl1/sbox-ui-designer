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

## Verify the install

After restart, in the s&box editor:

1. Open **File → Library Manager** — `kikozl.sbox_ui_designer` should be listed and enabled.
2. Open the **Asset Browser** → right-click any folder → **New** — you should see **Sbox UI Document** under the UI category.

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
