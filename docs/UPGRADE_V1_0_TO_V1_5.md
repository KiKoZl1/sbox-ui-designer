---
layout: default
title: V1.0 to V1.5 upgrade
nav_order: 10
---

# Upgrading from Sbox UI Designer V1.0 to V1.5

This guide walks you through updating an existing project from the V1.0 release
to V1.5. The short version: drop in V1.5, open the editor, click **Migrate all**
when the prompt appears, and you're done.

> **Audit:** every backward-compat surface of V1.5 was reviewed before release.
> TL;DR: green across the board, one documented amber. The full per-area
> verdict lives in the internal `_V15_UPGRADE_AUDIT` doc kept in the source
> repo (not published to the docs site).

---

## TL;DR

1. Back up your project (or just commit your current state).
2. Replace `Libraries/kikozl.sbox_ui_designer/` with the V1.5 build.
3. Open S&Box editor.
4. Open any `.sui` file → upgrade prompt appears.
5. Click **Migrate all**. Wait. Done.

---

## What's new in V1.5

| Milestone | Headline feature |
|---|---|
| **M1** — Variables & Bindings | Typed UI-local state. Each `.sui` declares `SuiVariable` slots; element properties bind to them through optional converter chains. PRD 18. |
| **M2** — Composition | One `.sui` can embed another via `SuiReference`. ForEach replication. User-widget palette section. PRD 19. |
| **M3** — Events & Element Refs | `OnClick` / `OnHover` / `OnValueChanged` slots on every interactive element, with two modes: **Code** (`.partial.cs` stub) or **Doo** (typed wrapper property). Element `@ref` exposure flag. PRD 20. |
| **M3.5** — Button Polish | Hover / Pressed / Disabled / Focused state SCSS overrides. Cursor presets. Hover / press sound slots. Smooth transitions. PRD 25. |
| **M4** — Input Widgets | TextEntry, Slider, Toggle, DropDown. TwoWay bindings + `UpdateTrigger` (OnChange / OnLostFocus / OnSubmit / OnRelease / Manual). PRD 21. |
| **Pipeline polish** | Asset Registry for stable GUID resolution. Cascade compile (parent recompiles dependent children automatically). `<Name>Panel.razor` + `<Name>.cs` wrapper split (renderer is the inner Panel; user-facing wrapper extends `SuiPanel<>`). |

There are 60+ built-in converters (Math, Range, Conversion, Logic, String, Color,
Collection), variadic `params` support, custom user-converter scaffolding into
`Code/GameConverters.cs`, broken-binding warnings, and a step-reorderable chain
editor.

---

## Breaking changes

**None at the public API level.** Every V1.5 surface is additive:

- No public type from V1.0 was removed or renamed.
- No `SuiBuiltinConverters` method had its signature changed.
- All new POCO fields ship with safe defaults — JSON deserialise of a V1 file
  produces a fully-shaped V3 document with no manual JSON editing required.

The one **shape change** worth mentioning is in generated code, not in the API:

> **The generated panel renderer was renamed from `<Name>.razor` to `<Name>Panel.razor`.**
> Your `[Property] public MyHud Widget` declaration still works — the public
> type `MyHud` now points to the wrapper class (`MyHud.cs`), which extends
> `SuiPanel<MyHudPanel>` and provides `Add()` / `Show()` / `Hide()` / `Remove()`.
> The renderer (the actual `PanelComponent`) is now named `MyHudPanel`.

**Practical impact:**
- If your gameplay code did `[Property] public MyHud Widget` and called `Widget.Show()`,
  **no changes needed** — `Show()` is provided by the new wrapper base class.
- If you instantiated the panel class directly via `new MyHud()` and treated it
  as a `PanelComponent`, swap to `widget.Add(scene); widget.Show();` — the
  wrapper pattern owns the lifecycle now.
- If you had a hand-edited `<Name>.partial.cs` referencing the renderer class by
  its old name, rename references from `<Name>` → `<Name>Panel`. (If you didn't
  write a `.partial.cs`, nothing to do.)
- **Old `<Name>.razor` files** left over from a very-early-alpha build (where the
  panel class still owned the bare name) are **deleted automatically** by Force
  Regen — but only when the file carries our `SUI:GENERATED:BEGIN` header
  comment. Hand-authored files that happen to share the name stay untouched.

---

## Upgrade steps

### 1. Back up

Either commit your project to source control, or copy the project folder.
Two paths can change:

- `Code/<output-folder>/*.razor` and `*.razor.scss` — fully regenerable.
- `Assets/**/*.sui` — these get **resaved** on upgrade (the schema version
  number bumps and the legacy Text-element size-mode fix lands). The resave
  is lossless, but commit before just in case.

### 2. Replace the library folder

Replace the contents of `Libraries/kikozl.sbox_ui_designer/` with the V1.5
release. If you cloned the source repo, `git pull` on the `v1.5` branch.

### 3. Open the editor

Boot S&Box editor with your project loaded.

### 4. Trigger the upgrade prompt

Open any `.sui` document — double-click in the Asset Browser, or pick one from
your recent list. The first time the SUI Designer window opens in a session, it
scans every `.sui` under the project root and counts how many are on an older
schema.

If at least one is found, you'll see:

> **Sbox UI Designer — schema upgrade detected**
>
> Found N of M .sui document(s) saved against an older schema (current is V3).
>
> Migrate all now? This will:
>   - reload each .sui through the migration pipeline
>   - resave the updated JSON
>   - regenerate all outputs (.razor / .razor.scss / wrapper .cs)
>   - clear the preview cache (Code/_sui_preview/)
>
> Your .sui sources are NOT renamed or restructured — only re-saved with the
> new schema version. User-owned files (`*.User.scss`, `*.partial.cs`,
> `GameConverters.cs`) are never touched.
>
> [ Don't ask again ]                       [ Skip for now ] [ **Migrate all** ]

### 5. Click **Migrate all**

The Force Regen pass logs progress per document:

```
[Sui] Force Regen starting — 18 .sui document(s) under project root.
[Sui] Force Regen 1/18: 'Assets/SuiSamples/hud_survival.sui' (schema V3)...
[Sui] Force Regen 2/18: 'Assets/SuiSamples/inventory_basic.sui' (schema V2)...
...
[Sui] Force Regen done — total: 18, migrated: 12, resaved: 18, compiled OK: 16,
    no-output: 2, failed: 0, files written: 78.
```

Documents without an `Output.RootFolder` are resaved (schema bumped) but not
recompiled — they're pure design-time sources you never compiled.

### 6. Restart S&Box (recommended if you saw the ⚠ advisory)

When Force Regen finishes, the editor shows a summary modal. If any documents
were migrated from an older schema OR any orphan classes were deleted, the
modal recommends a full editor restart. Reason: Sandbox's hot-reload handles
new outputs perfectly, but renamed/deleted classes can leave stale references
in TypeLibrary, scene files, or the Razor template cache. A restart guarantees
a clean state.

For minor regens (everything already V3, no orphans) the modal just confirms
success and you can keep working — no restart needed.

### 7. Done

Reopen any `.sui` and click Compile to confirm everything still builds. The
prompt won't reappear next session — the designer-state file remembers it
already migrated this schema version.

---

## Manual fallback

If something goes wrong, you can do the whole migration manually:

### Re-trigger the prompt

If you previously clicked "Don't ask again" but want the prompt back, edit
`<projectRoot>/.sui-designer-state.json` and set `DismissedUpgradePromptForVersion`
to `0`. Or just delete the file — the next editor open recreates it.

### Run Force Regen from the Tools menu

In any open SUI Designer window: **Tools → Force Regenerate All (migrate + recompile)**.
Same pipeline as the prompt's primary button.

### Wipe the preview cache by hand

If `Code/_sui_preview/` is stale or causing engine compile errors, delete the
whole folder:

```
rm -rf "<projectRoot>/Code/_sui_preview"
```

(Or in PowerShell: `Remove-Item -Recurse -Force <projectRoot>\Code\_sui_preview`)

The cache rebuilds on next Preview tab activation.

### A stale `<Name>.razor` orphan from V1.0 caused a CS0111 — what now?

Force Regen handles this automatically — but if you've already run it once and
still hit the error (e.g. the file's `SUI:GENERATED:BEGIN` header was edited
out by hand at some point), delete the old file manually:

```
rm "<projectRoot>/Code/<output-folder>/<Name>.razor"
```

Compile again — the generator only emits the `<Name>Panel.razor` shape now.

### Resave one .sui without recompiling

Open the `.sui` in the Designer. The schema version bump applies on `AssetOpen`
(via `SuiDocumentMigration.Apply`); the next `Save` writes the bumped version
back to disk. No regen runs unless you click Compile.

---

## FAQ / Known gaps

- **Does my `Code/GameConverters.cs` get touched?** No. The scaffolder
  only writes inside its `SUI:GENERATED:BEGIN` / `END` markers, and only
  for new converters you've declared in the Designer. Your hand-written
  converter methods are left alone.

- **Will `*.User.scss` get overwritten?** No. The compiler writes-if-missing
  on first compile; subsequent compiles always leave it alone.

- **Will `*.partial.cs` get overwritten?** No. The SUI compiler never writes
  to that file at all.

- **What if my `.sui` has fields the new schema doesn't know about?**
  System.Text.Json silently ignores unknown JSON properties on load. The
  resave drops them — but no V1.0 fields were removed in V1.5, so you should
  not hit this case unless you hand-edited the JSON.

- **What about `UpdateTrigger`?** New in late M4. Existing V2/V3 documents
  load with `UpdateTrigger = OnChange` (per-keystroke / per-frame), which
  matches pre-trigger behaviour. If you want a binding to commit on Enter
  or on focus-loss instead, re-author it in the bind popup.

- **What about `TransitionEnabled`?** V3 default is `true`, which adds a
  `transition: all 0.15s ease` rule on the root selector of every Button /
  InventorySlot / ItemIcon. With no Hover override authored, this is visually
  invisible (a transition with no state change does nothing). Once you add
  a Hover override, your colour / scale / opacity change will animate.

- **My V1.0 user-widget integration code uses `new MyHud()`.** Switch to:
  ```csharp
  [Property] public MyHud Widget = new();
  // ...
  Widget.Add( Scene );  // wire it up
  Widget.Show();        // visible
  Widget.Hide();        // not visible (kept in scene)
  Widget.Remove();      // tear down
  ```
  These are provided by `SuiPanel<TView>` in `SboxUiDesigner.Runtime`.

- **Where are deviations from the PRDs?** The internal `_V15_DEVIATIONS.md`
  log in the source repo (not published to the docs site). Ping the
  maintainer for the file if you need it for a support case.

---

If something on this list is wrong for your project, file an issue on the
repo with the line that broke and the .sui file that triggered it.
