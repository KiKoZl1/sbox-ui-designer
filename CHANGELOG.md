# Changelog

All notable changes to this project are documented here. This project follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The fuller, prose version with rationale lives in the docs site:
<https://kikozl1.github.io/sbox-ui-designer/support/changelog/>

## [Unreleased]

Promoted to [1.5.0].

## [1.5.0] — 2026-06-05

Major release. V1.5 turns the visual designer into a full reactive UI authoring pipeline: bindings + converters, composition (user widgets as first-class elements), event slots, element refs, Doo (WBP-style in-file graph), interactive state styles, four new input widgets (TextEntry, Slider, Toggle, DropDown), 16 showcase samples, and a rewritten docs site.

### Added

- **M0 — schema V2 foundation.** New `.sui` schema, Asset Registry, migration scaffold.
- **M1 — bindings + converters.** Variables panel with add/edit + undo/redo; bind popup with Variable picker, converter chain builder, and inline 🔗 icon on bound rows; 66 built-in converters across Number/String/Logic/Color/Math; custom `[SuiConverter]` discovery via TypeLibrary, auto-migration of legacy `Assets/GameConverters.cs` → `Code/`; codegen emits Variables, bind metadata, BuildHash, and universal binding-to-CSS so visual properties react with zero glue code.
- **M2 — composition.** `SuiReference` palette entry, picker, Details Props editor; AcceptedProps (later unified into `Variable.IsPublic`); ForEach + PerLocalPlayer modes (PerLocalPlayer later replaced by `Instance` mode + `SuiPanel<TView>` runtime API: Add/Show/Hide/Remove); palette **USER WIDGETS** section listing `.sui` from the registry; canvas paints SuiReference subtrees recursively with bounding-box scale; codegen wraps every document in a typed wrapper class with named-instance fields (`Hud.Child.Var`); cycle detector; **cascade compile** (UE5 WidgetBlueprint pattern) so editing a child re-emits its parents.
- **M3 — events + refs + Doo.** Event slots and element refs on every interactive element, Bindings-style Designer UX; Doo authoring (in-file Graph storage, WBP-like) for codeless event handlers; cross-paradigm name-collision detector; events-and-refs workflow guide.
- **M3.5 — interactive state styles.** Hover / Pressed / Focus / Disabled / Highlighted state authoring with sticky-on toggle for buttons; Cursor as enum dropdown; Razor `class` emitted via helper method.
- **M4 — input widgets.** TextEntry, Slider, Toggle, DropDown shipped with canvas paint matching engine widget templates, SCSS 1:1 with engine, sensible defaults, and full enum support. Slider rebuilt 100% in userland (custom tooltip pill, author-controlled colors, drag/step/snap math mirroring `SliderControl`).
- **UpdateTrigger.** TwoWay bindings can now commit on `PropertyChanged` (default) or `Manual` via `wrapper.Apply.<Field>()`.
- **Converters polish.** Literal args, chain reordering + step-swap buttons, type-tinted picker, variadic params, broken-binding visual indicator, custom converter dialog with default parameter values, `Compose` ("easy mode" string templating without `{N}` placeholders).
- **Upgrade flow.** Old-schema detector + Force Regen dialog (auto-cleans pre-M2-K6 `<Name>.razor` orphans), post-regen summary modal + restart advisory, `.sui-designer-state.json` gitignored, V1.0 → V1.5 upgrade guide.
- **Showcase samples.** 16 validated samples replacing dev fixtures, including 3 new wow-factor entries: `drag_drop_inventory`, `dialog_system` (via `chat_panel`/`notification_toast_queue` lineage), and `notification_toast_queue`. Round 1 beginner (5), Round 2 wow-factor (3), Round 3 advanced (5), plus polish pass.
- **Docs site rewrite.** 16-sample showcase index at `samples/showcase/README.md`, per-sample pages under `docs/samples/<name>/`, guided sample tour at `docs/getting-started/sample-tour.md`, concept-to-sample map at `docs/reference/concept-map.md`, gallery rewrite at `docs/reference/showcase-samples.md`, new concepts cluster (7 docs), new element docs (TextEntry/Slider/Toggle/DropDown/SuiReference), workflows cluster (5 docs), reference catalogs (4), tutorials (settings-screen, health-hud), GitBook visual overhaul (dark theme, card grids, callouts, mermaid).

### Changed

- **Wrapper class always generated** (Output Mode removed, D-013). Generated renderer extends `Panel`, not `PanelComponent`; runtime mounts via `Host.Panel.AddChild<TView>()`.
- **AcceptedProp unified into `Variable.IsPublic`** — net -633 lines, no obsolete cruft.
- **ActionGraph + FromComponent + Manual on Toggle/DropDown** ripped during cleanup; behavior consolidated under Doo / Code / TwoWay.
- **Regen pipeline** skips `Libraries/`, heals `Mode=Doo` documents with `DooBody=null`.
- **Razor class attribute** emitted via helper method (escaping fix) instead of mixed content.
- **Duplicate command** regenerates bind ids per clone (no shared-state collisions).
- **Converter validation** rejects cross-step type mismatches and unknown converter refs at edit time.

### Fixed

- **Codegen — flex-direction emission independent of `Layout.Mode`** (commit `0a514ee`).
- **Codegen — TextEntry caret-color + focus-hide-placeholder** (commit `d3d4459`).
- **Codegen — Text element `ExposeAsVariable` emits `@ref`** (commit `7199e37`).
- **Codegen — Highlighted wins over Hover but Pressed still fires** (commit `cf493b0`); class-helper uses bound Variable when `IsHighlighted` has binding.
- **Codegen — Visibility binding now wins over static `Style.Visibility`**; introduced `EmitIfNotBound` helper to close the static-vs-binding bug class permanently.
- **Codegen — ProgressBar fill position via SCSS class, not inline style**; removed harmful inline `position: relative` override on parent.
- **Converters — `Parse()` uses invariant culture** (locale bug); `MakeColor` clamps RGBA to `[0,1]`; `NormalizeTypeName` lowercases `Object/Object[]` so source→params slot validation passes; TwoWay auto-switches to OneWay when a converter is added.
- **Preview mount** runs in `OnStart` (not `OnAwake`), uses `Scene.CreateObject + AddChild`, invokes `OnEnabledInternal` on host before the Panel null-check, awaits `Task.Frame` instead of reflection sleeping.
- **Canvas — SuiReference children** painted via `SkipRootFrame` flag (no duplicate Canvas frame); `Paint.*` qualified with `Editor.` inside `SuiCanvasRenderer`; child `.sui` loaded via `AssetSystem` (strip `Assets/` prefix) instead of raw `System.Text.Json`.
- **Sample fixes from user testing:** `health_bar` binding property name, `counter_button` delegate assignment, positioning corrections, `quest_journal` tab swap + selection visuals + button states, `survival_hud_aaa` Tab/R test hotkeys for damage-flash testing.

### Docs

- Showcase READMEs promoted to canonical template (5 partials promoted, 4 troubleshooting sections added with bug history).
- 16-sample docs site pages, sample tour, concept map, gallery rewrite.
- Top-level `README.md` points to showcase; `samples/README.md` acts as redirect.
- Release-readiness audit: 108 R3 findings applied across 55 files; R2 coverage gaps closed with 5 new docs (Apply API method naming, slider codegen example, ForEach schema, `Source.VariableId`, element-Events dictionary); 1 RED + 8 YELLOWs from release-readiness audit fixed.
- DEVIATIONS log: D-013 (wrapper always generated), D-022..D-029 (M4 input widgets + converter overhaul), K-series notes, `wrapper-generation.md` replaces `output-modes.md`.

### Notes

- This is the V1.5 release. Close the V1.5 branch, fast-forward `main`, tag `v1.5.0`.
- ISSUE-006 and ISSUE-007 are scheduled for **V1.5.1**.
- ISSUE-004 (`<label>` rgba alpha background ignored) is fixed in V1.5 codegen but only affects `Text` elements with rgba-alpha backgrounds; the 16 showcase samples are unaffected. The only consumer was internal `Assets/SuiSamples/quest_log.sui`.
- V1.0 `.sui` files will prompt the upgrade dialog on open; Force Regen migrates them in place and cleans `<Name>.razor` orphans from pre-M2-K6 emit.

## [1.0.1] — 2026-05-13

Patch release: fixes the F2 / right-click → Rename flow in the Hierarchy widget, and clarifies anchor resize behavior in the docs.

### Fixed

- **Hierarchy inline rename now works.** `BeginRenameSelected` was previously a V2 stub that no-op'd — meaning F2, right-click → Rename, and Edit menu → Rename all silently did nothing despite an incorrect comment claiming they routed through the context menu. The Details panel Name field was the only way to rename an element. Implemented inline LineEdit overlay (UMG/Unity-style): F2 / Rename now opens a focused editor over the row's label; Enter or click-outside commits via the existing command stack; Ctrl+Z reverts.

### Changed

- **Docs — anchor resize behavior clarified.** Added a "What happens when the parent resizes" section to `docs/concepts/anchors-and-pivot.md` explaining that single-point anchors (TopLeft, MiddleCenter, etc.) keep child size fixed when the parent resizes, while Stretch anchors and Flex containers scale children. Includes an ASCII worked example and a 4-row "what you want → which anchor" table. Matches UMG default behavior; UMG's ScaleBox-equivalent is on the V1.5+ roadmap.
- **Docs — FAQ entry for resize confusion.** Added "Why doesn't my element scale when I resize its parent?" to `docs/support/faq.md` pointing at the new concepts section.

### Notes

- All V1.0 samples and the V1.0 generated `.razor` outputs are unaffected by this release. Drop-in upgrade.
- Inline rename's Escape-to-cancel is not yet implemented; use Ctrl+Z to revert a committed rename. Escape support requires raw key capture and lands in a polish pass.

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

[Unreleased]: https://github.com/KiKoZl1/sbox-ui-designer/compare/v1.5.0...HEAD
[1.5.0]: https://github.com/KiKoZl1/sbox-ui-designer/releases/tag/v1.5.0
[1.0.1]: https://github.com/KiKoZl1/sbox-ui-designer/releases/tag/v1.0.1
[1.0.0]: https://github.com/KiKoZl1/sbox-ui-designer/releases/tag/v1.0.0
