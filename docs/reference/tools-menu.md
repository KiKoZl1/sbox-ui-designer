---
layout: default
title: Tools menu
parent: Reference
nav_order: 11
---

# Tools menu
{: .no_toc }

Every item under the Designer's **Tools** menu, what it does, when you'd reach for it. The boring command-reference page. Bookmark it for the moments your save / compile loop trips and you need a hammer.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Where the menu lives

Top of the Designer window, between **View** and **Help**. Active whenever a `.sui` document is open. Source: `Editor/SuiDesignerWindow.cs` `AddMenu("Tools")` (~line 581).

---

## The menu items

In on-screen order:

### Validate Document

Runs `SuiDocumentValidator` against the current document and surfaces every error / warning in the Compile Results panel.

**When you'd use it:**

- Before a `Ctrl+S` to know whether your edits are clean.
- When something feels off — broken binding warnings, name collisions, unreachable elements show here.
- As a quick sanity check after a big restructure.

Equivalent to triggering the validation pass without the file write. Saves still validate; this is the explicit form.

### Regenerate Preview

Re-runs the canvas paint by re-emitting the preview cache for the current document. Forces the canvas to drop its render-cache.

**When you'd use it:**

- Canvas looks stale after a `.User.scss` edit.
- An embedded SuiReference looks frozen at an old state (the registry rarely misses, but the cache might).
- After installing or removing a custom converter, when the bind popup should now offer it.

This is a per-document operation. Cheap.

### Clean Preview Cache

Wipes the preview cache directory for the **current project**. Next render rebuilds from scratch.

**When you'd use it:**

- The Regenerate Preview hammer didn't work — escalate to this.
- You're shipping a release build and want zero cache cruft in the repo.

Heavier than Regenerate Preview but bounded. Doesn't touch user code or `.sui` files.

### Clean All SUI Caches (preview + backups)

Same as Clean Preview Cache PLUS deletes the auto-backup `.sui.bak` files the Designer writes on every save.

**When you'd use it:**

- You're tidying the repo and the `.bak` files are noise in `git status`.
- A backup got corrupted and the restore-on-crash flow is mis-firing.

The Designer recreates the `.bak` files on the next save. Safe to run anytime.

### Rebuild SUI Asset Registry

Tells `SuiAssetRegistry` to scan every `.sui` in the project + rebuild the GUID → asset lookup table. Forces the registry to forget anything cached.

**When you'd use it:**

- After moving a `.sui` file with git (the registry caches by absolute path).
- After deleting a `.sui` and its embeds elsewhere now claim "Source unknown".
- After a clean clone where the registry hasn't bootstrapped yet.

The registry rebuilds on editor startup, but this gives you the explicit "no I really mean it" path.

### Install Sample Documents

Copies the bundled SUI samples (TestParent, InteractiveHud, InputWidgetsShowcase, etc.) into `Assets/SuiSamples/` of the current project.

**When you'd use it:**

- First-time setup on a new project — gives you the reference docs to remix.
- After a clean to repopulate.

Idempotent. Existing samples aren't overwritten. See [Sample index]({% link reference/sample-index.md %}) for what each sample showcases.

### Force Regenerate All (migrate + recompile)

The nuclear option. Walks every `.sui` in the project, runs the V1.0→V1.5 migration pipeline if needed, and recompiles all of them. Also cleans up orphan `<Name>.razor` files from the pre-M2-K6 era (per the V1.0 → V1.5 upgrade migration).

After it runs, the Designer reloads the currently-open document so you see the post-regen state.

**When you'd use it:**

- After upgrading from V1.0 to V1.5 — `UPGRADE_V1_0_TO_V1_5.md` recommends this first.
- After a schema bump (V2 → V3 in V1.5 M3.5) — auto-prompt if the loader detects an older schema, but the menu lets you trigger it manually.
- After a refactor in your own code that breaks every generated `<Name>.cs` (e.g. you renamed a namespace).
- When the auto-prompt is suppressed (see `[Sui] upgrade prompt suppressed for schema V<N>`).

Logs a summary in Console: `Force Regen complete — migrated X, compiled Y, failures Z.` Failures land in Compile Results.

---

## What's NOT in the Tools menu (but lives nearby)

- **Save** / **Save As** → File menu.
- **Undo** / **Redo** → Edit menu (or `Ctrl+Z` / `Ctrl+Y`).
- **Zoom controls** → View menu.
- **Compile a single .sui** → automatic on save. No explicit menu item — saves trigger the generator.
- **Per-element actions** (Lock / Hide / Expose as Variable / Bind…) → right-click the element in Hierarchy or the Details panel.

---

## See also

- [Editor tour]({% link user-guide/editor-tour.md %}) — every panel + menu in context
- [Top toolbar]({% link user-guide/top-toolbar.md %}) — the icon strip below the menu
- [Sample index]({% link reference/sample-index.md %}) — what Install Sample Documents installs
- [Upgrade guide]({% link UPGRADE_V1_0_TO_V1_5.md %}) — Force Regen in the V1.0 → V1.5 context
