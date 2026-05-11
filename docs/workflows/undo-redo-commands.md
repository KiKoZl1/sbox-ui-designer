---
layout: default
title: Undo / Redo and commands
parent: Workflows
nav_order: 4
---

# Undo / Redo and commands
{: .no_toc }

How every edit becomes a reversible command, and how the undo stack tracks them.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## The principle

Every mutation against a SUI document goes through a **command**. Direct mutation is forbidden — even adding a single element runs an `SuiAddElementCommand`. This invariant is what makes undo trivially correct: every change is paired with the inverse needed to reverse it.

The command stack is owned by `SuiDesignerController`. It exposes:

- `Ctrl+Z` → undo
- `Ctrl+Y` (or `Ctrl+Shift+Z`) → redo

…and the **Edit** menu shows the next undo/redo description.

## Built-in commands

Each user action maps to one command type:

| Action | Command |
|---|---|
| Drag from Palette → Canvas | `SuiAddElementCommand` |
| Delete (Del) | `SuiDeleteElementCommand` |
| Drag element on canvas | `SuiMoveElementCommand` |
| Resize via handles | `SuiResizeElementCommand` |
| Drop into a different parent | `SuiReparentElementCommand` |
| Drag in Hierarchy (reorder siblings) | `SuiReorderElementCommand` |
| Rename in Hierarchy | `SuiRenameElementCommand` |
| Edit any field in Details | `SuiSetPropertyCommand` |
| Click an Anchor cell | `SuiSetAnchorCommand` |
| Ctrl+D | `SuiDuplicateElementCommand` |
| Ctrl+V | `SuiPasteElementCommand` |
| Align menu (left/center/…) | `SuiAlignElementsCommand` |
| Distribute menu (h/v) | `SuiDistributeElementsCommand` |

Source: see [Editor/Commands/](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/Editor/Commands).

## How a command works

Each command implements `ISuiCommand`:

```csharp
public interface ISuiCommand
{
    string Description { get; }      // shown in Edit > Undo "<desc>"
    void Apply( SuiDocument doc );
    void Undo( SuiDocument doc );
}
```

**Apply** mutates the document forward. **Undo** restores the prior state — the command captures whatever values it needs at construction (old position, old text, old parent ID, etc.).

Example: dragging a button from `(10, 10)` to `(120, 80)` constructs

```csharp
new SuiMoveElementCommand(
    elementId: "btn_42",
    oldPosition: (10, 10),
    newPosition: (120, 80)
);
```

Apply sets the new position. Undo restores the old.

## The stack

`SuiCommandStack` is a classic two-stack design:

```
[ undo stack ]      [ redo stack ]
   bottom              bottom
   ↓                   ↓
   cmd 1               cmd 7  ← redo restores this
   cmd 2               cmd 6
   cmd 3               cmd 5
   cmd 4
   cmd 5               ← undo reverses this
   top                 top
```

Behavior:

- **Push** (apply a new command) → push onto undo, **clear redo stack**.
- **Undo** → pop undo, run `Undo()`, push onto redo.
- **Redo** → pop redo, run `Apply()`, push onto undo.

Standard semantics. The one thing worth noting: **any new edit after an Undo discards the redo history**. This is intentional and matches every editor on the planet.

## Stack depth

Default cap: **256 commands**. When exceeded, the oldest entry is discarded.

If you find this too small (rare — most sessions stay well under 50), you can raise it by editing `SuiCommandStack.MaxDepth`, but the rationale for the cap is RAM: each command holds a snapshot of the data it mutated.

Set to `0` for unlimited (not recommended for very long editing sessions).

## What clears the stack

The stack is per-document. It's cleared when:

- You open a different `.sui` file (the controller swaps documents).
- You reload the same file from disk (e.g. asset hot-reload after an external edit).
- You call **File → New** to start fresh.

Saving a file does NOT clear the stack — you can Save then keep undoing past the save point.

## Save-after-undo behavior

Undoing past your last save and then saving **rewrites the file with the older state**. This is correct (your in-editor state is the source of truth) but worth knowing if you accidentally Ctrl+Z too far.

The output folder's `.sui-backups/` does not back up `.sui` documents — only generated `.razor` / `.razor.scss`. Versioning the `.sui` is your job (Git is the conventional answer).

## Coalescing — what isn't merged

Most editors coalesce continuous edits (e.g. typing in a text field produces one undo entry per word, not per character). **SUI Designer does NOT coalesce.** Each property edit is one command.

For Details-panel typing this means: typing "Health" in the Text field produces **6 commands** (one per character) if the field commits per-keystroke. The Details widget mitigates this by committing on focus-out / Enter, not per keystroke — so most fields produce 1 command per edit.

If a field commits per-keystroke and floods the stack, that's a bug. Open an issue with a repro.

## Drag = one command, not many

Dragging an element across the canvas is a single `SuiMoveElementCommand` — captured at drag end, not per mouse-move. Same for resize and reparent. Group drags produce one command per selected element (so dragging 5 elements = 5 undo entries — one per element).

This is by design: undoing a group drag with a single Ctrl+Z would feel surprising. If you want them grouped, that's a feature request.

## Commands vs the document

Commands operate on `SuiDocument` directly. They don't go through any service or event bus. After `Apply()` or `Undo()` runs, the controller fires a `DocumentChanged` event that:

1. Marks the document dirty (window title gets `*`).
2. Tells the canvas to repaint.
3. Tells the Details panel to refresh.
4. Tells the Hierarchy to rebuild.

There's no incremental dirty tracking — every command does a full repaint. Cheap because the document is small.

## Writing custom commands (developers)

If you're extending SUI Designer and need a new mutation type:

1. Create `Editor/Commands/SuiYourThingCommand.cs`.
2. Implement `ISuiCommand` — `Description`, `Apply`, `Undo`.
3. Capture all data needed for Undo at construction time. **Never** read live document state inside `Undo()` — by then the document has already changed.
4. Push via `controller.CommandStack.Push( cmd, document )`.

Example skeleton:

```csharp
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

public sealed class SuiToggleVisibilityCommand : ISuiCommand
{
    public string Description => $"Toggle '{_elementId}' visibility";

    private readonly string _elementId;
    private readonly SuiVisibility _oldValue;
    private readonly SuiVisibility _newValue;

    public SuiToggleVisibilityCommand( SuiElement el, SuiVisibility next )
    {
        _elementId = el.Id;
        _oldValue = el.Style.Visibility;
        _newValue = next;
    }

    public void Apply( SuiDocument doc )
    {
        var el = doc.FindById( _elementId );
        if ( el != null ) el.Style.Visibility = _newValue;
    }

    public void Undo( SuiDocument doc )
    {
        var el = doc.FindById( _elementId );
        if ( el != null ) el.Style.Visibility = _oldValue;
    }
}
```

Then in your trigger code:

```csharp
controller.CommandStack.Push(
    new SuiToggleVisibilityCommand( selected, SuiVisibility.Hidden ),
    controller.Document
);
```

## See also

- [Controller architecture]({% link architecture/overview.md %}) (planned)
- [Keyboard shortcuts]({% link reference/keyboard-shortcuts.md %})
