---
layout: default
title: Canvas
parent: Element reference
nav_order: 1
---

# Canvas (root)

Every `.sui` document has **exactly one** Canvas element — the root. It cannot be deleted, dragged, or anchored; it always fills `Document.Canvas.BaseWidth × BaseHeight`.

## Properties

The Canvas element's **Details panel** shows special **Document** and **Canvas** sections (instead of the usual Transform).

### Document section
| Field | Notes |
|---|---|
| **Name** | The document's display name (matches the `.sui` filename) |
| **Id** | Stable internal ID (`sui_<slug>_<guid8>`) — used for ownership tracking, never changes on rename |
| **Schema** | Schema version (currently `v1`) |
| **Elements** | Count of all elements in the document |

### Canvas section
| Field | Notes |
|---|---|
| **Base Width** | Logical pixel width (default 1920) |
| **Base Height** | Logical pixel height (default 1080) |
| **Scale Mode** | `ScreenHeight1080` (default) / `FixedResolution` / `Stretch` / `DesktopResolution` |

### Output section
| Field | Notes |
|---|---|
| **Class Name** | The C# class name the generator emits (e.g. `MyHud`) |
| **Namespace** | C# namespace (default `Game.UI`) |
| **Root Folder** | Where the compile writer writes the `.razor` + `.razor.scss`. Set on first Compile via folder picker |

## Generated output

```razor
@namespace Game.UI
@using Sandbox;
@using Sandbox.UI;
@inherits PanelComponent

<root>
  <div class="root sui-root">
    ...children...
  </div>
</root>
```

```scss
ClassName {
  .root {
    position: absolute;
    left: 0;
    top: 0;
    right: 0;
    bottom: 0;
    ...children...
  }
}
```

The root `.root` rule always emits `position: absolute; left: 0; top: 0; right: 0; bottom: 0;` — fills the panel regardless of `Base Width` / `Height`. The Base dimensions only affect the canvas paint and the generator's coordinate space.

## Tips

- **Don't change Class Name / Namespace** mid-project unless you re-run Compile and delete the old generated files — orphan files become Conflicts.
- Editing **Base Width / Height** is rare. The default 1920×1080 matches the standard HD canvas. Change only if your design targets a different aspect.
- The Canvas's **Background** under Appearance is the **preview** background only (visible in the editor's canvas). It's NOT emitted to the runtime — runtime is transparent so the `ScreenPanel`/`WorldPanel` over the game world remains see-through.
