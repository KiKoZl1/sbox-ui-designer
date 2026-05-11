---
layout: default
title: Troubleshooting
parent: Support
nav_order: 1
---

# Troubleshooting
{: .no_toc }

Common failure modes and how to diagnose them.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## "I clicked Test in Play and nothing happened"

The launcher prints diagnostics to the engine console (`View → Console`). Open it and look for `[SuiPreviewLauncher]` / `[SuiPreviewMount]` lines.

### "Already in Play. Stop the current play first."

You have a play session active. Click Stop in the Scene Viewport.

### "Compile failed: ..."

The generator produced errors. Check the Compile Results panel for details — usually a SCSS validation issue.

### "Timed out waiting for type ... to compile (≈6s)"

The `.razor` was written but the engine didn't compile it. Common causes:

- SCSS error in the generated output → check console for `[Sui] scss emit blocked:` messages.
- Razor syntax error → won't happen with V1 generator output, but if you've edited `_sui_preview/...` files manually, undo.
- Another `partial class` declaration with the same name elsewhere in your project → rename your real-final compile target.

### "Preview stage not found at 'sui_preview/preview_stage.scene'"

The addon's bundled assets are missing. Reinstall the addon or copy `Libraries/kikozl.sbox_ui_designer/Assets/sui_preview/` from the source repo.

### `Mounted '<fqn>' on ScreenPanel.` appears but UI isn't visible

The UI is mounted but rendered off-screen, transparent, or behind another panel. Common causes:

- The root element has `opacity: 0` set in Style.
- An ancestor has `Visibility: Hidden`.
- The Output namespace clashed with another `partial class`.

Switch to canvas mode in SUI Designer and verify the design renders there. If canvas shows it but runtime doesn't, file an issue.

---

## "Compile failed — file Conflict"

The writer found a file at the target path that **doesn't have the SUI:GENERATED header**, OR has a header pointing at a different `DocumentId`.

### What it means

Someone (you, a previous compile of a different document, or hand-written code) put a file with the same name in your output folder. The writer refuses to overwrite to protect your work.

### How to fix

1. Open the conflicting file — its path is in Compile Results.
2. If it's hand-written code you care about → move it elsewhere first.
3. If it's stale from a previous compile → delete it.
4. Recompile. The fresh file will write cleanly.

### How to prevent

Don't manually edit files with the `SUI:GENERATED` header. Use `.User.scss` for custom styles instead — see [User SCSS customization]({% link workflows/user-scss-customization.md %}).

---

## "I edited the generated file and now my changes are gone"

Compile creates **backups** automatically when it overwrites a generated file with new content:

```
<projectRoot>/.sui-backups/<DocumentName>/<UTC-timestamp>/<file>
```

1. Open that folder.
2. Find the backup at the timestamp just before your overwrite.
3. Manually re-merge your edits — preferably into `.User.scss` so they survive next time.

If `.sui-backups/` is empty, the file likely never had a SUI header — it was treated as Conflict, never touched. Check version control (`git stash` recovery).

---

## "The canvas shows X but Test in Play shows Y"

Some divergence is expected:

- **Hovers, animations, `box-shadow`** — these live in `.User.scss` which isn't loaded in Test in Play (only Compile + Play).
- **PreviewCount badges** — canvas draws them, runtime doesn't yet ([ISSUE-005]({% link reference/known-issues.md %}#issue-005-previewcount-badges-not-emitted-in-runtime)).
- **`<label>` rgba background** — canvas honors alpha, `<label>` runtime ignores it ([ISSUE-004]({% link reference/known-issues.md %}#issue-004-label-background-color-rgba-alpha-ignored-in-runtime)). Workaround: wrap Text in a Panel.

For any other divergence, please file an issue with both a screenshot and the `.sui` file.

---

## "I can't see my changes after pressing Compile"

The writer hashes content. If the new generation matches the existing file's hash, it's a no-op:

```
[Compile] MyHud.razor: Skipped (no changes)
```

This is correct! It means the engine doesn't need to hot-reload, so nothing visible changes.

If you're sure something should have changed:

1. Open the `.razor` directly and look at the timestamps.
2. Check that the change you made in the editor was actually saved (`Ctrl+S`).
3. Check the Compile Results panel for actual file write rows.

---

## "Drag-and-drop in canvas feels off"

If an element jumps when you start dragging, or doesn't follow your cursor cleanly, the most common cause is **drag during canvas zoom != 100%**. The drag code uses the screen-to-canvas transform to map mouse position; some marginal cases on extreme zoom (`Ctrl++` many times) introduce float drift.

Fix: `Ctrl+0` (Fit to screen), then drag.

If it still feels wrong, file an issue with the zoom level + a repro.

---

## "SUI Designer window won't open"

Try in order:

1. **Reload the editor** — sometimes the addon manifest cache is stale.
2. **Check the addon is installed** — verify `Libraries/kikozl.sbox_ui_designer/` exists in your project.
3. **Check `library_loaded` in `manifest.json`** — should be `true`.
4. **Re-import the asset** — right-click your `.sui` file → Re-Open.

If the editor crashes when opening a `.sui`, the most likely cause is a malformed `.sui` file. Open it in a text editor; the JSON should parse cleanly. If it doesn't, restore from git or fix the JSON manually.

---

## "My `.sui` file got reset to empty"

There's a known recovery scenario where a `.sui` with invalid enum values (e.g. from manual editing) failed to load silently → designer created a default empty doc → auto-saved on close → original data lost.

This is **fixed in V1.0** via the `_loadLikelyFailed` protection: if a `.sui` file is >1KB on disk but the loaded document has 0 elements, the designer assumes load failed and **blocks any save**.

If you still hit this:

1. Open your `.sui` in a text editor.
2. Look for `"Elements": []` → load failure occurred.
3. Restore the file from git (`git checkout HEAD -- Assets/UI/<your>.sui`).

The protection should prevent this from happening in fresh sessions. If you can reproduce data loss in V1.0, please file an urgent issue.

---

## "Test in Play opens but the world is empty / dark"

The bundled `preview_stage.scene` should have a player + ground + lights. If it's empty:

- The addon's Assets folder is missing or corrupt.
- Reinstall from source: copy `Libraries/kikozl.sbox_ui_designer/Assets/sui_preview/` over your current install.

---

## "Compile takes a long time"

Normal compile time: <1 second for documents under 100 elements. If it's slow:

- The hot-reload happens AFTER write — the engine takes 200-800 ms to recompile the Razor.
- For very large documents (500+ elements), the generator itself adds time.
- The Compile Results panel updates after the engine finishes — there's a small lag.

If a single compile takes >5 seconds, please file an issue with the `.sui` file size and element count.

---

## Getting help

If none of the above resolves your issue:

1. Search [existing issues](https://github.com/KiKoZl1/sbox-ui-designer/issues) — your problem may already be reported.
2. If not, open a new issue with:
   - A description of what you tried.
   - The `.sui` file (attach or paste).
   - Engine console output.
   - Screenshots if visual.
3. For urgent bugs, mention them in the s&box Discord with a link to your issue.

## See also

- [Known issues]({% link reference/known-issues.md %}) — confirmed open bugs
- [FAQ]({% link support/faq.md %}) — common questions
