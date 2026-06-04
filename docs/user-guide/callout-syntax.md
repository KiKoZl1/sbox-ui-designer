---
layout: default
title: Callouts
parent: User Guide
nav_order: 99
---

# Callouts

The SUI Designer documentation site uses [just-the-docs callouts](https://just-the-docs.com/docs/ui-components/callouts/) to highlight tips, warnings, and other inline asides. Six kinds are configured — each one is styled in the SUI palette with a Unicode glyph prefix so they read at a glance.

This page is a quick reference: how to write each kind in Markdown, and what they render as.

## Available kinds

| Class         | Color        | Glyph | Use it for                                  |
|---------------|--------------|:-----:|---------------------------------------------|
| `.tip`        | green        | ✦     | A helpful shortcut or "good to know"        |
| `.note`       | green        | ◆     | An aside the reader can safely skip         |
| `.info`       | blue         | ▶     | Neutral context the reader benefits from    |
| `.new`        | purple       | ★     | A feature added in the current release      |
| `.warning`    | amber        | ⚠     | A gotcha that will bite if ignored          |
| `.important`  | red          | ⚡     | Data loss, breaking change, or hard rule    |

All six are pre-declared in `_config.yml` under the `callouts:` key, so you only have to apply the class — no front-matter setup per page.

## Syntax

Callouts use the kramdown [block IAL](https://kramdown.gettalong.org/quickref.html#block-attributes) syntax: a `{: .class }` line **immediately before** the paragraph or blockquote it applies to.

### Single paragraph

```markdown
{: .tip }
Use `Ctrl+Shift+Click` on the canvas to add an element without picking from the palette.
```

{: .tip }
Use `Ctrl+Shift+Click` on the canvas to add an element without picking from the palette.

### Multi-paragraph (blockquote form)

```markdown
{: .warning }
> The Test in Play button re-uses the active scene.
>
> Unsaved scene edits will be lost when the play session shuts down.
```

{: .warning }
> The Test in Play button re-uses the active scene.
>
> Unsaved scene edits will be lost when the play session shuts down.

### Custom title

Append `-title` to the class to use the **first line** of the blockquote as the title instead of the default ("Tip", "Warning", ...).

```markdown
{: .important-title }
> Bindings break on rename
>
> Renaming a `@ref` element after binding it requires re-binding by hand. There is no rename-refactor in V1.5.
```

{: .important-title }
> Bindings break on rename
>
> Renaming a `@ref` element after binding it requires re-binding by hand. There is no rename-refactor in V1.5.

## Rendered examples

One of each kind so you can copy the closest fit when writing a page.

{: .tip }
Hold `Alt` while dragging an element in the Hierarchy panel to duplicate-and-reparent in a single gesture.

{: .note }
The bottom tabs (Variables / Bindings / Events / Compile / Logs) collapse when their content fits — there is no manual collapse toggle.

{: .info }
SUI files are stored as JSON under `Assets/ui/`. They are safe to diff and merge by hand, though the Designer round-trip is preferred.

{: .new }
**V1.5 M4** adds Slider, NumberBox, TextEntry, and Checkbox input widgets to the palette, with two-way Bindings out of the box.

{: .warning }
Editor scenes do not run the normal lifecycle. `PanelComponent` instances need `OnEnabled` / `OnPreRender` invoked manually via reflection — see the M10 spike notes.

{: .important }
Do **not** push `v1.5` to the remote until manual smoke-test passes. The branch is local-only by policy until UAT signs off.

## When NOT to use a callout

- For step-by-step instructions — use an ordered list, not a stack of callouts.
- For code-only asides — a fenced code block is enough.
- For headings — callouts are paragraph-level. If a section needs its own heading, give it a heading.

A page densely filled with callouts becomes a wall of colored boxes that reads worse than plain prose. One or two per page is usually plenty.

## Adding a new kind

Edit `docs/_config.yml` and add an entry under `callouts:`. Then add a matching `@include sui-callout(...)` and `@include sui-callout-icon(...)` line to `docs/_sass/custom/_callouts.scss` so the brand colors and glyph apply.

If you skip the SCSS step, the new kind will fall back to the stock just-the-docs styling (gray border, no glyph), which clashes with the SUI dark theme.
