---
layout: default
title: Changelog
parent: Support
nav_order: 3
---

# Changelog

Release history. The authoritative version is in [CHANGELOG.md](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/CHANGELOG.md) in the source repo.

---

## V1.5 — 2026-06-03

Major feature release. **Fully backward-compatible** — V1.0 documents load unchanged and get migrated via the upgrade prompt (see the [V1.0 → V1.5 upgrade guide]({% link UPGRADE_V1_0_TO_V1_5.md %})). The internal deviations log (kept in the source repo, not published to the docs site) is the authoritative catalogue of changes between the locked PRDs and the shipped code.

### Milestones

| Milestone | Headline feature |
|---|---|
| **M1** — Variables & Bindings | Typed UI-local state. Each `.sui` declares `SuiVariable` slots; element properties bind to them through optional converter chains. PRD 18. |
| **M2** — Composition | One `.sui` can embed another via `SuiReference`. ForEach replication. USER WIDGETS dynamic palette section. PRD 19. Wrapper class always generated (DEVIATIONS D-013). |
| **M3** — Events & Element Refs | `OnClick` / `OnHover` / `OnValueChanged` slots on every interactive element, with two modes: **Code** (Action handler) or **Doo** (visual script stored inside the `.sui`). Element `@ref` exposure flag. PRD 20. |
| **M3.5** — Button Polish | Hover / Pressed / Disabled / Focused per-state SCSS overrides. Cursor presets. Hover / press sound slots. Smooth transitions. ButtonShape + BackgroundSize. PRD 25. |
| **M4** — Input Widgets | TextEntry, Slider, Toggle, DropDown. TwoWay bindings + `UpdateTrigger` (`OnChange / OnLostFocus / OnSubmit / OnRelease / Manual`). `Apply` API for Manual commits. 13 new builtin converters + UI overhaul (literal args, type-tinting, broken-binding visuals, step reorder, Compose converter). PRD 21. |

### Concepts that landed

- **Variables** — string / int / long / float / bool / Color / Vector / engine types / `Enum:` / `Component:` / `List<T>` + `IsPublic` flag.
- **Bindings** — OneTime / OneWay / TwoWay (+ reserved OneWayToSource), per-`(elementType, property)` matrix.
- **Converters** — 66 builtins (Math 10 / Range 9 / Conversion 6 / Logic 10 / String 15 / Color 10 / Collection 6) + custom `[SuiConverter]` scaffolding into `Code/GameConverters.cs`. Variadic `params object[]` support. Literal args + chain reposition + broken-binding warnings.
- **Composition** — `SuiReference` embeds another `.sui`. Recursive canvas paint. Recursive `ContentHash` (DEVIATIONS D-015).
- **Wrapper class** — every `.sui` compiles to `<Name>Panel.razor` + `<Name>.razor.scss` + `<Name>.cs` (extends `SuiPanel<<Name>Panel>`). Add / Show / Hide / Remove API. `Apply.<ElementName>Value()` namespace for `UpdateTrigger.Manual` (TextEntry + Slider only — Toggle + DropDown Manual flush is a V1.5 gap). (DEVIATIONS D-013, D-014, D-029.)
- **Events** — Doo replaces ActionGraph as the visual scripting backend (DEVIATIONS D-017). Doo body stored inside the `.sui` (WBP-like, DEVIATIONS D-018).
- **Asset Registry** — stable GUID resolution for SuiReference + cascade compile + USER WIDGETS palette section.

### Schema

Bumped to **V3**:

- V1 → V2 — adds Variables + Bindings + Events + SuiReference element type.
- V2 → V3 — adds per-state interactive style overrides + Transition + Sound + Cursor + ButtonShape + BackgroundSize on `SuiElementProps`.

V1 + V2 documents migrate automatically on first open (lossless — every existing field still loads). See [SUI JSON schema]({% link reference/sui-json-schema.md %}).

### Elements (5 new)

- **SuiReference** — composition.
- **TextEntry** / **Slider** / **Toggle** / **DropDown** — input widgets.

See [Element type matrix]({% link reference/element-types.md %}).

### Known gaps (V1.5 final)

- Action Graph picker on Code-mode `[Property] Action` slots persists but doesn't fire in Play (DEVIATIONS D-020).
- `TextEntry.IsPassword` / `AutoFocus` / `IsNumeric` deferred to V1.6 (DEVIATIONS D-023).
- `Toggle` ships only the default Checkbox visual; Pill / Switch variants deferred to V1.6 (DEVIATIONS D-025).
- Dynamic `DropDown.Options` binding deferred to V1.6 (DEVIATIONS D-024).
- Drag `.sui` from Asset Browser onto canvas deferred to V1.6 (DEVIATIONS D-011).
- Find Usages cache deferred (DEVIATIONS D-008).
- Standalone PreviewData panel deferred (DEVIATIONS D-009).

---

## V1.0 — 2026-05-11

First public release.

### Features

- **Visual designer** — drag-and-drop canvas with 16 element types, multi-select, alignment tools, anchor picker, color picker, snap-to-grid, group drag, marquee select.
- **Two-renderer architecture** — editor canvas via Qt `Editor.Paint` for design-time interaction; runtime via s&box CSS engine for production. Shared `SuiLayoutSolver` keeps them in agreement.
- **Razor + SCSS codegen** — Generates `.razor` (PanelComponent) + `.razor.scss` (Sass) from `.sui` documents. Header-protected, hash-deduped, manifest-tracked.
- **Test in Play** — One-click "compile + open stage scene + EditorScene.Play" workflow. Real-engine preview on a real player.
- **`.User.scss` sidecar** — User-owned customization file created once, never overwritten. Imported by the generated SCSS so user rules cascade-win.
- **Backups + recovery** — Every overwrite backs up to `.sui-backups/` outside `Code/` to keep the engine compiler happy.
- **Allowed-property whitelist** — Generator validates every CSS property against an explicit allowed list. Catches engine-unsupported properties at compile time instead of silently failing at runtime.
- **5 sample `.sui` files** — Survival HUD, death modal, loot pickup, inventory screen, quest log. Cover all 15 visible element types.

### Elements

All 16 types: Canvas, Panel, Overlay, Text, Image, Button, HorizontalBox, VerticalBox, Grid, ScrollPanel, ProgressBar, InventoryGrid, InventorySlot, ItemIcon, Tooltip, Hotbar.

### Layout

- 12 anchor presets (9 corner/edge/center + 3 stretch variants).
- Pivot offset per element.
- Flex containers with direction / justify / align-items / wrap / gap / padding / margin.
- Grid via wrapped-flex strategy (CSS Grid is forbidden in s&box).
- Auto-Text size mode + Fixed mode + AutoHeightWrap mode for text elements.

### Known issues at release

- [ISSUE-004]({% link reference/known-issues.md %}#issue-004-label-background-color-rgba-alpha-ignored-in-runtime) — `<label>` rgba alpha ignored by runtime CSS engine. Workaround: wrap Text in Panel.
- [ISSUE-005]({% link reference/known-issues.md %}#issue-005-previewcount-badges-not-emitted-in-runtime) — PreviewCount badges shown in canvas but not emitted in Razor.

### Resolved during V1.0 development

- ISSUE-001 — ColorPicker SV box stale on hue change → resolved by replacing editor's color picker with custom `SuiColorPickerPopup`.
- ISSUE-002 — Text vertical alignment divergence between canvas and runtime → resolved by `SuiTextSizeMode { Auto, Fixed, AutoHeightWrap }`.
- ISSUE-003 — Editor color picker instability (5 symptoms) → resolved alongside ISSUE-001.

See [ISSUES.md](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md) for the full historical track.

---

## Roadmap (post-V1.0)

These are planned, not promises. Order may change based on user feedback.

### V1.1 (incremental)

- Fix ISSUE-004 (Text rgba background) by wrapping label in div or emitting div directly when bg is set.
- Fix ISSUE-005 (PreviewCount badges in runtime).
- More samples covering edge cases.

### V1.5

- **`[Property]` exposure** — flag an element as a Variable, generates a public `[Property]` field on the C# class. Gameplay code sets values; UI updates automatically.
- **Bindings tab** — declare `Element.Text ← Source.Path` style data bindings in the designer. Generator emits `BuildHash` and update logic.
- **Event hookup** — declare `Button.OnClick → MyHandler` in the designer. Generates C# event subscriptions.
- **Asset-aware image picker** — browse project assets instead of typing paths.
- **Multi-document workspace** — open multiple `.sui` files in tabs.

### V2

- **WorldPanel support** — 3D-positioned UI in the scene.
- **Full flex algorithm** — `flex-grow` / `shrink` / `basis` honored by canvas.
- **`align-self` per child** — exposed in inspector.
- **Themes** — reusable style tokens shared across multiple `.sui` documents.
- **Animation timeline** — visual keyframe editor for transitions.
- **Drag-and-drop logic in addon** — shared C# helpers for inventory drag/drop so games don't reinvent them.

## Notes

This is a community project. Cadence depends on time available, user feedback, and severity of issues found in the wild. If something on the roadmap matters to you, drop a thumbs-up on the matching issue or open one if it doesn't exist.

## See also

- [CHANGELOG.md](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/CHANGELOG.md) — root release log
- [ISSUES.md](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md) — open and historical issues
