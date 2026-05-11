---
layout: default
title: Generator pipeline
parent: Architecture
nav_order: 5
---

# Generator pipeline
{: .no_toc }

How a `.sui` document becomes a pair of in-memory `.razor` + `.razor.scss` strings — the pure-function half of compilation.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Inputs and outputs

```csharp
public sealed class SuiGenerationContext
{
    public SuiDocument Document { get; set; }
    public SuiGenerationMode Mode { get; set; }     // Preview | Final
    public string OutputFolder { get; set; }        // project-relative
    public string ClassName { get; set; }           // defaults to Document.Output.ClassName
    public string Namespace { get; set; }           // defaults to Document.Output.Namespace
}

public enum SuiGenerationMode { Preview, Final }

var result = SuiGenerationPipeline.Run( ctx );
// result.Files = [ { Kind: Razor, Path: "...", Content: "...", Hash: "..." }, ... ]
// result.Errors / Warnings / Infos
```

Pure function: no disk writes, no side effects. The compile writer (separate component) takes the result and persists it. See [Compile writer]({% link architecture/compile-writer.md %}).

Source: [Code/Generation/SuiGenerationPipeline.cs](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Code/Generation/SuiGenerationPipeline.cs).

## Pipeline stages

```
SuiDocument
    │
    ▼
1. SuiDocumentValidator        (cycles, dup IDs, missing root → bail)
    │
    ▼
2. Resolve ClassName + Namespace
    │
    ▼
3. SuiRazorGenerator           → in-memory Razor string
    │
    ▼
4. SuiScssGenerator            → in-memory SCSS string
    │
    ▼
5. Pack into SuiGenerationResult (files + hashes + diagnostics)
```

If validation fails the pipeline returns early with errors — no Razor or SCSS is produced.

## Mode: Preview vs Final

The same generator runs in both modes. Two differences in output:

### 1. Namespace suffix

```csharp
if ( ctx.Mode == SuiGenerationMode.Preview && !ctx.Namespace.EndsWith( ".SuiPreview" ) )
    ctx.Namespace = ctx.Namespace + ".SuiPreview";
```

So `Game.UI.MyHud` (Final) becomes `Game.UI.SuiPreview.MyHud` (Preview). This prevents `CS0111: duplicate render-tree members` when a document is **both** compiled to disk AND has a live preview cache — they would otherwise be two `partial class MyHud` declarations in the same namespace.

### 2. User.scss `@import`

The Final SCSS ends with:

```scss
// User-protected styles for MyHud — safe to edit.
@import "MyHud.User.scss";
```

The Preview SCSS does NOT emit this `@import`. The preview cache path doesn't have a sidecar — and importing a non-existent file silently breaks SCSS compilation at runtime (leaving the UI unstyled). The fix is to emit the import only in Final mode.

That's it. Every other output detail is identical.

## The Razor generator

`SuiRazorGenerator.Generate(ctx, result)` walks the document tree and emits:

```razor
@using Sandbox;
@using Sandbox.UI;

@namespace Game.UI

@attribute [StyleSheet]

@inherits PanelComponent

<root>
  <div class="sui-elem-1 root">
    <div class="sui-elem-2 health-bar">
      ...
    </div>
  </div>
</root>

@code
{
    protected override int BuildHash() => System.HashCode.Combine( /* fields here */ );
}
```

Notable shapes:

- **Every element gets a `sui-<id>` class** alongside its user-defined `ClassName`. This per-element class is what scopes SCSS rules to a single element — without it, siblings sharing a `ClassName` would all inherit the same rules (bug-prone for InventorySlot lists).
- **The outer tag matches `Output.ClassName`** — `<MyHud>` becomes the root selector both in HTML and SCSS.
- **Text elements emit `<label>`**, others emit `<div>` (or `<button>`, `<img>` for those types).
- **`BuildHash()`** is emitted to force re-render when the document changes during hot-reload. V1 emits a static value; V1.5 will hash element state.

The Razor doesn't include the SUI:GENERATED header — that's added by the compile writer when the file is actually written to disk (so editing the in-memory output doesn't see the header).

## The SCSS generator

`SuiScssGenerator.Generate(ctx, result)` is the bulk of the generator complexity. It walks the document tree and emits CSS rules per element.

### Nesting structure

```scss
MyHud {                           // outer = class name
  flex-direction: column;
  background-color: #0a0a0a;

  .sui-elem-2 {                   // per-element unique class
    position: absolute;
    left: 40px;
    top: 40px;
    width: 200px;
    height: 18px;
    background-color: rgba(0,0,0,0.5);
  }

  .sui-elem-3 {
    ...
  }

  // ProgressBar nested fill rule
  .sui-elem-2 .progress-fill {
    background-color: #ef4444;
  }

  // Final mode only:
  @import "MyHud.User.scss";
}
```

The whole document compiles into a single nested SCSS block. Two-space indent. Stable output across edits (sorted by element ID where order doesn't matter visually).

### Anchor → CSS translation

The 9-anchor + pivot model from the document compiles into CSS like:

| Anchor | CSS emitted |
|---|---|
| TopLeft | `position: absolute; left: X; top: Y;` |
| TopCenter | `position: absolute; left: 50%; top: Y; transform: translateX(-50%);` |
| MiddleCenter | `position: absolute; left: 50%; top: 50%; transform: translate(-50%, -50%);` |
| BottomRight | `position: absolute; right: X; bottom: Y;` |
| Stretch | `position: absolute; left: X; top: Y; right: W; bottom: H;` |

The math mirrors `SuiLayoutSolver.ResolveAbsoluteRect` 1:1.

### Property emission rules

For every CSS property the generator emits, it passes through `SuiAllowedPropertyList.Validate(property, value)`. Rejected properties surface as `Errors` in the result — the compile aborts. This blocks the most common class of bug: a renderable-on-web property that s&box silently ignores.

What's allowed: see [Allowed CSS reference]({% link reference/allowed-css.md %}).

### Skipped emissions

The generator deliberately doesn't emit:

- `width: 8px; height: 8px;` when anchor is `Stretch` — those fields are margins, not size. Emitting them collapses the element.
- `position: relative` (default) — only emit on flex items that need it for absolute children.
- Per-type defaults — `background-color: transparent` is the default, no need to write it.
- `display: flex` outside flex containers — wastes a rule.

Defaults stay implicit because every emitted rule is a chance for typo/drift.

### Type-specific blocks

Each element type may emit additional nested rules:

- **Button** — nested `.label` rule for the inner text (font, color, align).
- **ProgressBar** — nested `.progress-fill` rule with width based on `PreviewValue`.
- **Image** — `background-image: url(/path)` + `background-size: cover` (or `contain` per `FitMode`).
- **ItemIcon** — emits `background-image` from `PreviewIconPath` if present.
- **Grid / InventoryGrid** — wrapped flex translation (CSS Grid is forbidden in s&box).

### Special: Grid mapping

CSS Grid (`display: grid`) is forbidden by the allowed-property list — s&box's Yoga engine doesn't support it. SUI's Grid element maps to **wrapped flex**:

```scss
.sui-grid {
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  gap: 4px;
}
.sui-grid > * {
  width: 64px;
  height: 64px;
}
```

This matches what the canvas's `SolveGrid` does — both produce a regular tile pattern with row wrapping.

## Hashing

Every file in the result carries a SHA-256 of its content (`SuiHashUtility.Sha256`). The compile writer uses this to skip "same hash, no-op" writes, avoiding the expensive engine hot-reload trigger.

## Error and warning surfaces

The result has 3 diagnostic levels:

- `Errors` — block the compile (validator failures, disallowed properties).
- `Warnings` — proceed but surface in Compile Results (`ClassName` collision, missing image asset).
- `Infos` — generator narration (skipped no-op files, emit counts).

All surfaced in the [Compile Results panel]({% link user-guide/compile-results.md %}).

## What the generator does NOT do

- **Doesn't touch the filesystem** — that's the compile writer's job.
- **Doesn't add the SUI:GENERATED header** — also the writer.
- **Doesn't read or write the manifest** — also the writer.
- **Doesn't run SCSS compilation** — outputs source SCSS, the s&box engine compiles it at load.
- **Doesn't generate `.cs` code** — V1 emits only `.razor` and `.razor.scss`. V1.5 will optionally emit a `.cs` partial for [Property] fields.

The strict pure-function boundary makes testing and offline reasoning easy: you can run the generator from a unit test with a synthetic `SuiDocument` and assert on the emitted strings.

## See also

- [Compile writer]({% link architecture/compile-writer.md %}) — what consumes the result
- [Document model]({% link architecture/document-model.md %}) — what feeds the generator
- [Allowed CSS]({% link reference/allowed-css.md %}) — the property whitelist
