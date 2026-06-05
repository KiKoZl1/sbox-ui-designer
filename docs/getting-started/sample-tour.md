Sample tour guided learning-path doc authored at C:/DEV/Surprise/sbox-ui-designer/docs/getting-started/sample-tour.md.

Structure:
- Jekyll frontmatter as specified (layout: default, title: Sample tour, parent: Getting started, nav_order: 5).
- Title "# Sample tour" with the standard `{: .no_toc }` modifier used by sibling pages, plus a one-paragraph intro framing this as a guided learning path from "mount a panel" to "wire a full settings flow with Apply.All()".
- Table of contents block matching the existing your-first-ui.md / showcase-samples.md pattern.
- Prerequisites section linking back to install + your-first-ui (so the tour is not a dead-end if a reader lands here cold).
- Six ordered "### N. <sample_name>" sections, each with the required **What you'll learn**, **Time** (~10 min), 2-3 sentence walkthrough, and **Next** link:
  1. empty_canvas — minimum mount lifecycle, Show/Hide plumbing.
  2. label_clock — first OneWay binding, OnChange trigger.
  3. counter_button — first Code-mode OnClick, SyncFieldsTo / assign-before-Show gotcha.
  4. toggle_pause — first TwoWay binding (Toggle.Checked round-trip).
  5. health_bar — ProgressBar bound to normalized 0..1 float, fraction+label canonical pattern.
  6. settings_full — full input widget suite + Apply.All() Manual-commit pattern with dirty-state.
- "## After the tour" section with 3 bullets pointing at drag_drop_inventory (ExposeAsVariable + runtime AddChild), dialog_system (deferred mutation + User.scss overrides), notification_toast_queue (CSS-owned transitions + class flips). Each bullet calls out the new concepts.
- "## See also" linking to showcase-samples (samples landing), sample-index, bindings concept page, plus a back-link to your-first-ui.

Notes:
- The samples landing page on the docs site is reference/showcase-samples.md (verified — no docs/samples/ directory exists), so per-sample {% link %} fallbacks all use the GitHub URLs as the spec allows.
- Used `parent: Getting started` exactly as specified, even though existing sibling pages use the capitalized "Getting Started" form. If the build's just-the-docs config is case-sensitive on the parent title, this page may need its parent value tweaked to "Getting Started" to nest correctly under the existing section card.
- Total length ~1100 words including frontmatter, within the 800-1200 range.
- Tone matches existing getting-started/your-first-ui.md and reference/showcase-samples.md (concise, technical, friendly, .fs-6 .fw-300 hero paragraph, TOC block, link-heavy footer).