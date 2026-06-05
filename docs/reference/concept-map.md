Authored Concept-to-sample lookup at C:/DEV/Surprise/sbox-ui-designer/docs/reference/concept-map.md.

Key decisions verified against the existing repo:

- Jekyll frontmatter uses `parent: Reference` and `nav_order: 12` exactly as requested. Nav slots 10 (sample-index), 11 (showcase-samples) are already taken, 12 is free.
- Sample link format uses `[name]({% link samples/name.md %})` — verified this is the convention `showcase-samples.md` already uses for the per-sample pages.
- Doo mode is included as a V1.5 feature (not deferred to V2): confirmed by `docs/concepts/events-and-actions.md` lines 99-125, which document Doo as the replacement for ActionGraph in V1.5 (DEVIATIONS D-017 / D-018 / D-019).
- "See also" links point to existing pages: `reference/showcase-samples.md`, `reference/sample-index.md`, `getting-started/sample-tour.md` (the latter is referenced from showcase-samples.md as the narrative tour landing).
- Sample-to-concept mappings were drawn from `reference/showcase-samples.md` and `reference/sample-index.md` Variables/Bindings/Events tables — e.g. `chat_panel` documents Manual+TwoWay TextEntry with Apply.All flush; `inventory_grid_full` documents the ExposeAsVariable + one-shot bootstrap pattern; `notification_toast_queue` is the CSS-transitions / FlexColumn / frame-staggered class-flip sample.
- Word count is in the 600-900 target range.

Cross-referenced concept docs linked inline so readers can drill from the matrix row to the conceptual explainer: `concepts/bindings.md`, `concepts/events-and-actions.md`, `concepts/interactive-states.md`, `concepts/layout-modes.md`, `concepts/wrapper-generation.md`, `workflows/user-scss-customization.md`, `workflows/manual-commit-with-apply.md`, `reference/binding-mode-matrix.md`.