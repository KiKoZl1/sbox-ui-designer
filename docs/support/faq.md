---
layout: default
title: FAQ
parent: Support
nav_order: 2
---

# FAQ
{: .no_toc }

Common questions about SUI Designer.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What is SUI Designer?

A visual UI editor for s&box. It opens `.sui` documents — a JSON format describing a UI tree — and lets you arrange them on a canvas, then compiles them into Razor + SCSS that the s&box runtime renders as a `ScreenPanel`.

Think of it as Figma → exported as functional code, with the s&box engine as the renderer.

## Who is it for?

- s&box developers who want a Figma-style workflow instead of writing Razor by hand.
- People comfortable with HTML/CSS-style layout (flexbox, anchors) who want a visual front-end.
- Teams where a designer iterates on UI separately from the programmer who wires it up.

## Is it free / open source?

Yes. MIT license. Source: [github.com/KiKoZl1/sbox-ui-designer](https://github.com/KiKoZl1/sbox-ui-designer).

## Does it replace Razor?

No — it **generates** Razor. You can still hand-edit Razor for things the designer doesn't expose. The split is:

- **`.razor`** — generated, do not edit (overwritten on Compile).
- **`.razor.scss`** — generated, do not edit.
- **`.User.scss`** — yours, edit freely. Never overwritten.
- **`.cs` partial** (V1.5+) — yours, edit freely.

## Can I use it for in-world UI (WorldPanel)?

V1 targets ScreenPanel (HUD overlays). WorldPanel support (3D-positioned UI in the scene) is V2.

## Does it support hot-reload?

Yes — Test in Play uses the engine's hot-reload to push compiled Razor into a live play session. Edit `.sui`, Test in Play, see it on a real player.

## Why is my UI different in Test in Play vs the canvas?

Two renderers. The canvas paints via Qt `Editor.Paint`; runtime renders via the s&box CSS engine. They usually agree, but:

- `.User.scss` isn't loaded in Test in Play (only in real Compile + Play).
- `<label>` rgba alpha is a known engine quirk — see [ISSUE-004]({% link reference/known-issues.md %}#issue-004-label-background-color-rgba-alpha-ignored-in-runtime).
- PreviewCount badges show in canvas but not runtime — see [ISSUE-005]({% link reference/known-issues.md %}#issue-005-previewcount-badges-not-emitted-in-runtime).

If you find a divergence not listed in [Known issues]({% link reference/known-issues.md %}), please report it.

## How do I add a hover effect?

The Details panel doesn't expose `:hover`. Add it to `.User.scss`:

```scss
MyHud {
  .my-button {
    transition: background-color 0.15s ease;

    &:hover {
      background-color: #3b82f6;
    }
  }
}
```

See [User SCSS customization]({% link workflows/user-scss-customization.md %}) for the full pattern.

## How do I bind data to my UI?

V1 only supports static content. V1.5 introduces:

- `[Property]` on generated C# class (set values from gameplay code).
- A Bindings tab in the designer (declare `Element.Text ← Source.Path`).

Until V1.5, you wire things up by hand in a `.partial.cs` file — see the [Death modal tutorial]({% link tutorials/death-modal.md %}) for the pattern.

## Can I edit the generated `.razor` directly?

You CAN, but the next Compile will overwrite it (after backing up your version). Better:

- For styles → put them in `.User.scss`.
- For markup that the designer doesn't support → consider a feature request.
- For logic → write a sibling `.partial.cs` that extends the generated class.

## How do I rename a class?

In the Output section of the right panel, change Class Name. Recompile.

The old generated files are detected as **Obsolete** (their header has the old DocumentId, but no new file claims them). The Compile Results panel surfaces them — manually delete them.

The DocumentId itself does NOT change on class rename — it stays stable so the manifest still works.

## How do I move my `.sui` to a different project?

Copy:

- `Assets/UI/your_file.sui`
- Optionally `Code/UI/YourFile.User.scss` if you've customized it

Open in SUI Designer in the new project. Set Output → Class Name + Namespace + RootFolder for the new project. Compile.

The `DocumentId` is preserved; manifest will start fresh in the new output folder.

## What's the V1 → V2 plan?

V1 (current): visual design + Razor/SCSS codegen + Test in Play.

V1.5: bindings, `[Property]` exposure, event hookup, animations.

V2: WorldPanel support, full flex algorithm, themes, palette pre-builds, drag-and-drop logic shared in addon.

## What if I need a property that's not in the Details panel?

Two options:

1. **Add it to `.User.scss`** — works for any allowed CSS property. See [Allowed CSS]({% link reference/allowed-css.md %}).
2. **Open a feature request** on GitHub. Most missing properties are intentional V1 scope cuts and reasonable additions for V2.

## Is the canvas WYSIWYG?

Mostly. The canvas matches runtime for: anchors/pivot, flex, sizing, colors, borders, text, images. It does NOT match for: hover/active states (canvas is static), `box-shadow`, `transform`, `:intro`/`:outro` (transitions), and the known issues above.

For features the canvas can't preview, the status bar shows a hint.

## How big can a `.sui` file get?

There's no hard limit, but in practice:

- Up to ~100 elements: zero perf concerns.
- 100–500 elements: still fine; canvas paint stays smooth.
- 500+ elements: getting unusual; consider splitting into multiple `.sui` files (one per UI screen).

A typical HUD has 10–30 elements. A complex inventory might be 50–100.

## How do I undo a destructive operation?

`Ctrl+Z` works in the designer for all document mutations (delete, move, etc.). For things outside the document (renaming the `.sui` file, deleting compiled `.razor`s manually), you're on your own — use git.

## How do I clean up old generated / backup files?

**Tools → Clean SUI Caches**. Wipes:

- `<projectRoot>/.sui-backups/` (all timestamps).
- `Code/_sui_preview/` (preview cache).

The Manifest is left alone — clean it manually if you want to force a fresh recompile of everything.

## Can I share a `.sui` across multiple projects?

Yes — `.sui` files are pure JSON, project-independent. The output settings (class name, namespace, root folder) are per-doc but easy to change per project.

You can keep a library of reusable `.sui` files in a shared repo and copy them into projects as needed.

## What if my game uses a different UI framework (not ScreenPanel)?

SUI Designer targets s&box's standard Sandbox.UI / PanelComponent / ScreenPanel stack. If you have a custom framework, you can:

- Use `.sui` files as a design source-of-truth (JSON).
- Write your own generator that emits whatever your framework needs.

The `.sui` schema is documented in [SUI JSON schema]({% link reference/sui-json-schema.md %}).

## Will this be in the s&box marketplace?

Currently distributed as a Library you add to your project's `Libraries/`. Marketplace listing is on the V1.0 release checklist.

## Where can I see real examples?

5 sample `.sui` files ship with the addon under `Assets/SuiSamples/`:

- `hud_stats_v2.sui` — Survival HUD (matches the [Survival HUD tutorial]({% link tutorials/survival-hud.md %}))
- `death_screen.sui` — Death modal
- `loot_pickup.sui` — Bottom-corner pickup notification
- `inventory_screen.sui` — Full-screen inventory
- `quest_log.sui` — Side-panel quest list

Open any of them in SUI Designer to inspect, modify, or copy patterns.

## I have another question.

Open an [issue](https://github.com/KiKoZl1/sbox-ui-designer/issues) or join the s&box Discord and tag the question with SUI Designer.

## See also

- [Troubleshooting]({% link support/troubleshooting.md %})
- [Changelog]({% link support/changelog.md %})
- [Known issues]({% link reference/known-issues.md %})
