---
layout: default
title: User SCSS customization
parent: Workflows
nav_order: 3
---

# User SCSS customization
{: .no_toc }

How to add custom styles that survive recompile — using the `.User.scss` sidecar.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Why the sidecar exists

The generated `.razor.scss` is **owned by the compiler** — every press of `Ctrl+B` may overwrite it. If you edit it directly your work is lost on the next compile.

The sidecar pattern fixes this: alongside `MyHud.razor.scss` the compiler writes `MyHud.User.scss` **once** and never touches it again. The generated file ends with:

```scss
@import "MyHud.User.scss";
```

So your rules cascade in last and win at equal specificity.

## File location

After first compile, your output folder contains:

```
<outputFolder>/
├── MyHud.razor          # generated — do not edit
├── MyHud.razor.scss     # generated — do not edit
└── MyHud.User.scss      # YOURS — edit freely
```

The sidecar lives next to the generated files. It's a normal SCSS file the s&box Razor compiler picks up.

## Boilerplate

Created by the writer the first time a document compiles:

```scss
// MyHud.User.scss — your custom styles for MyHud.
// This file is created once and never overwritten by the SUI compiler.
// Edit freely — your rules win the cascade because the generated
// .razor.scss imports this file last.

MyHud {
  // Example:
  // .my-class { color: red; }
}
```

The outer `MyHud` selector scopes everything to your component's root panel. Anything nested inside is automatically descended from your top-level element — no need to repeat the `MyHud` prefix on each rule.

## What you can put in it

Any SCSS the s&box runtime accepts. Since the User sidecar is **not parsed by the SUI generator**, the [allowed-property whitelist]({% link reference/allowed-css.md %}) doesn't gate it — but the engine still does. If you write a property s&box doesn't parse, the engine just ignores it silently.

Common things people add to `.User.scss`:

### Hover / pressed states

```scss
MyHud {
  .pickup-button {
    transition: background-color 0.15s ease, transform 0.1s ease;

    &:hover {
      background-color: rgba(255, 255, 255, 0.12);
      transform: scale(1.04);
    }

    &:active {
      transform: scale(0.98);
    }
  }
}
```

### Glow / shadow effects

```scss
MyHud {
  .health-bar {
    box-shadow: 0 0 8px rgba(239, 68, 68, 0.5);
  }

  .rare-item-icon {
    box-shadow: 0 0 12px rgba(250, 204, 21, 0.8);
  }
}
```

### Animations

```scss
MyHud {
  @keyframes pulse {
    0%   { transform: scale(1); }
    50%  { transform: scale(1.05); }
    100% { transform: scale(1); }
  }

  .low-health-flash {
    animation: pulse 0.6s ease-in-out infinite;
  }
}
```

### Pseudo-classes the editor doesn't expose

```scss
MyHud {
  .toggle-button {
    &.active {              // toggled by your component code
      background-color: #22c55e;
    }
    &.disabled {
      opacity: 0.4;
      pointer-events: none;
    }
  }
}
```

Toggle these via the `Sandbox.UI.Panel` API in your code:

```csharp
ToggleButton.SetClass( "active", isOn );
```

### Tweaking generated rules

If a generated property is "close, but not exactly right" — write the override in `.User.scss`. It cascades last, so:

```scss
MyHud {
  // Generator emitted: background-color: rgba(0,0,0,0.5);
  // Want it darker:
  .modal-backdrop {
    background-color: rgba(0, 0, 0, 0.85);
  }
}
```

## Targeting elements

Every element generates a stable per-element class `sui-<id>` (used internally for collision-free styling) **plus** any user-provided `ClassName`. Prefer targeting your `ClassName`s — they're stable across edits.

For an element with `ClassName = "pickup-button"` you get:

```html
<div class="sui-elem-12 pickup-button">
```

So in your sidecar:

```scss
MyHud {
  .pickup-button { ... }      // preferred — stable, semantic
  .sui-elem-12 { ... }        // brittle — ID changes if you recreate the element
}
```

If you forgot to set a `ClassName` in the Details panel, set it now and recompile. Your sidecar rule starts working immediately.

## SCSS features available

The s&box runtime uses LibSass-compatible SCSS. Available:

- **Nesting** (`&:hover`, `& .child`) — used everywhere above.
- **Variables** (`$accent: #22c55e; color: $accent;`)
- **`@keyframes`** + `animation` — see Animations above.
- **Mixins** (`@mixin card { ... } .x { @include card; }`)
- **Functions** — `darken()`, `lighten()`, `rgba($color, $alpha)`, `mix()`.

Things to avoid:
- **`@import` of external `.scss` files** — the s&box build only sees files in your project's SCSS scope. Stick to one User sidecar per generated panel.
- **`display: grid`, `position: fixed`** — see [Allowed CSS]({% link reference/allowed-css.md %}). The engine silently ignores them.

## How the cascade actually works

The generated file looks roughly like:

```scss
MyHud {
  flex-direction: column;
  background-color: #0f1117;

  .health-bar {
    background-color: #ef4444;
    width: 200px;
  }

  /* …more generated rules… */

  // User-protected styles for MyHud — safe to edit.
  @import "MyHud.User.scss";
}
```

The `@import` is **inside** the outer `MyHud` selector, so your sidecar rules are scoped to it automatically. That's why the boilerplate's outer `MyHud { ... }` block is informational — you can omit it and write `.health-bar { ... }` at the top level if you prefer:

```scss
// Both of these reach the same element:

MyHud {
  .health-bar { box-shadow: 0 0 8px red; }
}

// or just:

.health-bar { box-shadow: 0 0 8px red; }
```

(The first form is clearer and matches the boilerplate convention.)

## Test in Play and the sidecar

**Test in Play does NOT include your `.User.scss`.** The preview pipeline runs the generator in `Preview` mode which skips the `@import` (the sidecar doesn't exist at the preview path).

So:

- Canvas preview → no `.User.scss` styles.
- Test in Play → no `.User.scss` styles.
- Real Compile → Play → **yes**, your User styles are live.

If a hover effect or animation only appears after a real compile, this is why. See [Test in Play workflow]({% link workflows/test-in-play.md %}) for details.

## When the sidecar isn't enough

If a single property you want to tweak is exposed in the Details panel, prefer editing it there — round-trips correctly through the document. Use `.User.scss` for the long tail: hovers, animations, shadows, pseudo-classes.

If you find yourself rewriting half the generated rules in the sidecar, that's a sign the element type doesn't fit your design — consider switching to a different element or filing a [feature request]({% link support/faq.md %}).

## See also

- [Compile + output management]({% link workflows/compile-and-output.md %})
- [Styling concept]({% link concepts/styling.md %})
- [Allowed CSS reference]({% link reference/allowed-css.md %})
