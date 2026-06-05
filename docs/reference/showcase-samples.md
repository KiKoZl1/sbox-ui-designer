Rewrote `showcase-samples.md` as a gallery landing page.

**Preserved verbatim:**
- Frontmatter (layout/title/parent/nav_order: 11)
- Intro paragraph (line 11 of the original — "The 16 showcase samples shipped with V1.5 — 5 beginner ones that isolate a single SUI Designer concept each, 3 intermediate samples that wire multiple features onto realistic surfaces, and 8 advanced samples that combine the full runtime into game-flow surfaces (modals, multi-tab navigation, dramatic single-element drives, chat history, class pickers, dialog trees, drag-and-drop, and stacking toast queues). Open the `.sui`, drop the companion `Component` on a `GameObject`, and you have a working UI in seconds.")
- `{: .fs-6 .fw-300 }` styling on the intro

**Replaced (removed per-sample H2 walkthroughs):**
- Old per-sample H2 sections (empty_canvas, label_clock, health_bar, counter_button, toggle_pause, settings_full, inventory_grid_full, survival_hud_aaa, death_respawn_modal, quest_journal, boss_hp_bar, chat_panel, loadout_selector) — all replaced by links out to `{% link samples/<name>.md %}` per-sample pages being built in parallel.
- Old "Table of contents" TOC block (no longer needed for a card-style landing).
- Old "See also" tail (replaced with the new shorter one).

**New structure (in order):**
1. Intro paragraph (preserved) + a short "this page is the gallery landing" sentence pointing to Sample index and Sample tour.
2. `## Browse by category` with 5 sub-sections — Starter (3), Input widgets (2), Interactive states (3), Runtime-rendered (7), Full-feature (1) — each a markdown table with `Sample | What it teaches | Difficulty` columns. Sample names link via `{% link samples/<name>.md %}`. All 16 samples from the prompt metadata are covered, including the three that weren't yet in the old file (dialog_system, drag_drop_inventory, notification_toast_queue).
3. `## Browse by concept` — short pointer to `{% link reference/concept-map.md %}`.
4. `## Pattern recipes` — 16 "I want to..." → "Look at..." rows (one per sample) mirroring the GitHub showcase README convention. Sample names link via `{% link samples/<name>.md %}`.
5. `## Source repository` — links the GitHub source folder `https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase` and lists the three artefacts each folder ships (`.sui`, controller, README).
6. `## See also` — three bullet links: Sample index, Sample tour, Concept map.

**Categorisation source:** the metadata block in the prompt (Starter / Input widgets / Interactive states / Runtime-rendered / Full-feature, with per-sample difficulty + one-line teaches).

**Word count:** ~870 words (within the 700–1000 target).

Output file: C:/DEV/Surprise/sbox-ui-designer/docs/reference/showcase-samples.md