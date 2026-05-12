# Changelog

All notable changes to this project are documented here. This project follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The fuller, prose version with rationale lives in the docs site:
<https://kikozl1.github.io/sbox-ui-designer/support/changelog/>

## [Unreleased]

Nothing yet.

## [1.0.0] — 2026-05-11

First public release.

### Added

- Visual designer window: paint-based canvas, multi-select, alignment tools, anchor picker, custom color picker, snap-to-grid, group drag, marquee select.
- 16 element types: Canvas, Panel, Overlay, Text, Image, Button, HorizontalBox, VerticalBox, Grid, ScrollPanel, ProgressBar, InventoryGrid, InventorySlot, ItemIcon, Tooltip, Hotbar.
- 12 anchor presets (9 corner/edge/center + 3 stretch variants) with pivot offset.
- Flex container support: direction, justify, align-items, wrap, gap, padding, margin.
- Text size modes: `Auto`, `Fixed`, `AutoHeightWrap`.
- Two-renderer architecture: editor canvas via Qt `Editor.Paint`; runtime via s&box CSS engine. Shared `SuiLayoutSolver` keeps them in agreement.
- Razor + SCSS code generation with `SUI:GENERATED` header-based ownership.
- `.User.scss` sidecar — written once, never overwritten, imported by generated SCSS so user rules win the cascade.
- One-click **Test in Play**: compile → poll TypeLibrary → open stage scene → `EditorScene.Play`.
- Compile writer with hash-deduped writes, manifest tracking, and backups outside `Code/`.
- Allowed-property whitelist: generator validates every CSS property against an explicit list and refuses unsupported properties at compile time.
- Undo/redo for every document mutation (256-deep command stack).
- 5 sample `.sui` files covering all 15 visible element types.
- Full docs site (~50 pages) covering getting-started, user guide, elements, concepts, workflows, architecture, reference, tutorials, support.

### Known issues at release

- `<label>` background-color rgba alpha is ignored by the runtime CSS engine (workaround: wrap Text in a Panel). See [ISSUE-004](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md#issue-004).
- `PreviewCount` badges show in the canvas but are not emitted in the runtime Razor yet. See [ISSUE-005](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/ISSUES.md#issue-005).

### Resolved during V1.0 development

- **ISSUE-001** — ColorPicker SV box stale on hue change → replaced editor's color picker with custom `SuiColorPickerPopup`.
- **ISSUE-002** — Text vertical alignment divergence between canvas and runtime → introduced `SuiTextSizeMode { Auto, Fixed, AutoHeightWrap }`.
- **ISSUE-003** — Editor color picker instability (5 related symptoms) → resolved alongside ISSUE-001.

[Unreleased]: https://github.com/KiKoZl1/sbox-ui-designer/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/KiKoZl1/sbox-ui-designer/releases/tag/v1.0.0
