---
layout: default
title: Changelog
parent: Support
nav_order: 3
---

# Changelog

Release history. The authoritative version is in [STATUS.md](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/STATUS.md) in the source repo.

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

- [STATUS.md](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/STATUS.md) — current development status
- [ISSUES.md](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md) — open and historical issues
