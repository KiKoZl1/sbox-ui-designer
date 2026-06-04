---
layout: default
title: Button
parent: Element reference
nav_order: 5
---

# Button
{: .no_toc }

A clickable region with a centered text label and full interactive-state support — hover, pressed, disabled, focused, transitions, sounds, cursor presets, shape presets.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## What it is

A `Button` is an interactive `<div>` with `Pointer Events: All` by default so it catches mouse input. Codegen emits an inner `<label>` with the button text and applies the **Interactive State** SCSS pseudo-classes (`:hover`, `:active`, `:focus`, `.disabled`) when overrides are authored.

In V1.5 (M3.5 — PRD 25) the Button gained five named states, transitions, per-state sounds, a cursor enum, a shape preset, and per-state background-image sizing. The bare-bones V1.0 button (a flat `<div>` with manual `.User.scss` hover rules) is still there if you ignore the new fields — every M3.5 addition is opt-in.

## Properties

### Button section

| Field | Default | Notes |
|---|---|---|
| **Button Text** | `""` | Caption shown inside the button |
| **Button Shape** | `Rectangle` | Preset that overrides `BorderRadius`. See [Button shape](#button-shape) |

### Text section

Font Size / Weight / Color / Text Align / Vertical Align apply to the inner `<label>`. See [Text]({% link elements/text.md %}).

### Appearance — Interactive States (V1.5 M3.5)

Below the standard Background / Border / Border Radius / Opacity controls, the Appearance section grows **four collapsible per-state dropdowns**:

| State | CSS selector | When |
|---|---|---|
| **Hover** | `:hover:not(:active)` | Pointer is over the element + not currently pressed |
| **Pressed** | `:active` | Mouse button down on this element |
| **Disabled** | `.disabled` | `IsDisabled = true` from the binding / wrapper |
| **Focused** | `:focus` | Keyboard / controller focused this element |

Each dropdown has a **Clear State** button + a `(set)` tag on its header when any field is authored. The fields available inside each state mirror the Normal Appearance set: background color, border color, border width, text color, scale, opacity, plus a per-state **Background Image** (with [Background Size](#background-size)) — useful for image-based buttons that swap a different texture on hover.

The pseudo-class emit order is **`:hover:not(:active)` → `:focus` → `:active` → `.disabled`** so Pressed always wins over Hover (without `:not(:active)` the engine CSS sticks on `:hover` and click visuals never show — validated against the M3.5 smoke test).

`.disabled` always carries `pointer-events: none` so authors don't have to write it themselves.

### Transition

| Field | Default | Notes |
|---|---|---|
| **Transition Enabled** | `true` | Emits `transition: all Ns ease` on the root selector |
| **Transition Duration** | `0.15s` | Material-Design "fast" baseline |

With Transition Enabled and **no** state overrides authored, the rule is visually inert (no change → nothing animates). The moment you author a Hover override, your colour / scale / opacity transitions smoothly.

### Sound + cursor

| Field | Notes |
|---|---|
| **Hover Sound** | `SoundEvent` asset path played on `:hover` ingress. Empty = silent |
| **Press Sound** | `SoundEvent` asset path played on `:active` ingress. Empty = silent |
| **Cursor** | Enum: Default / Pointer / NotAllowed / Wait / Text / Move / Crosshair / Help / None. `Default` emits no rule (engine default) |

### Button shape

`SuiButtonShape` overrides `BorderRadius` based on geometry:

| Value | Effect |
|---|---|
| `Rectangle` | No override — use `Style.BorderRadius` |
| `Square` | Forces `border-radius: 0` (warns if W ≠ H) |
| `Round` | Forces `border-radius: min(W,H)/2` — perfect circle when square |
| `Pill` | Forces `border-radius: 9999px` |
| `Custom` | Same as Rectangle but the inspector exposes the `BorderRadius` field prominently |

### Background size

When the Button has a background image (either Normal or per-state):

| Value | CSS emit |
|---|---|
| `Cover` | `background-size: cover` |
| `Contain` | `background-size: contain` |
| `Stretch` | `background-size: 100% 100%` |
| `Custom` | Author-set `BackgroundWidth`px × `BackgroundHeight`px |

The Details panel also offers a **Snap to image aspect** button that resizes the element to match the image's aspect ratio in one click.

## Wiring an OnClick

V1.5 ships first-class event wiring through the [Events tab]({% link workflows/events-and-refs.md %}). Two modes:

### Code mode

```csharp
// In the Designer: Events → + Add Event → Button → OnClick → Code → "OnFireClick"
// Then in your gameplay code:

public sealed class HudController : Component
{
    [Property] public Game.UI.MyHud Hud { get; set; } = new();

    protected override void OnStart()
    {
        Hud.OnFireClick = HandleFire;   // assign the Action slot
        Hud.Show();
    }

    void HandleFire() => Log.Info( "fired!" );
}
```

The generator emits `[Property, Group("Events")] public Action OnFireClick { get; set; }` on the wrapper.

### Doo mode

Pick **Doo** in the Add Event dialog → click **Open Full Editor** to author the Doo Graph inside the engine's DooEditor. The graph is stored **inside the `.sui`** so every instance shares the same default (engine inspector still lets you override per-instance). See [Events & Actions]({% link concepts/events-and-actions.md %}).

## Generated output

For a Button named `FireButton` with a Hover state authored:

```razor
<div class="primary-btn sui-fire-button @(IsDisabled ? "disabled" : "")"
     tabindex="0"
     @onclick=@OnFireClick>
    <label class="label">@ButtonText</label>
</div>
```

```scss
.sui-fire-button {
  width: 260px;
  height: 48px;
  background-color: #dc2626;
  border-color: #fca5a5;
  border-width: 2px;
  border-radius: 6px;
  display: flex;
  justify-content: center;
  align-items: center;
  transition: all 0.15s ease;
  cursor: pointer;
  sound-in: "ui/click.sound";

  .label {
    font-size: 18px;
    font-weight: 700;
    color: #ffffff;
    text-align: center;
  }

  &:hover:not(:active) {
    background-color: #ef4444;
  }

  &:active {
    background-color: #b91c1c;
    transform: scale( 0.98 );
  }

  &.disabled {
    pointer-events: none;
    opacity: 0.4;
  }
}
```

Razor emits `tabindex="0"` so `:focus` fires for controller / keyboard nav. The `(IsDisabled ? "disabled" : "")` class wires the runtime-bindable `IsDisabled` bool through.

## Tips

- Keep `Transition Enabled` on — it costs nothing visually until you author an override.
- Use `Button Shape: Pill` for "long horizontal" CTAs that should always have rounded ends regardless of width.
- Bind `IsDisabled` to a Variable to toggle the button from gameplay code (form validation, cooldowns, etc.).
- For hover/active feedback you can't express via the per-state dropdowns (custom keyframes, complex effects), fall back to the `.User.scss` sidecar:

```scss
.fire-button:hover .label { letter-spacing: 0.05em; }
```

The user-side stylesheet imports after the generated one so it always wins.

## Known gaps

- **Per-state preview on the canvas** — cancelled (M3.5 P5). The canvas always renders Normal. See overrides via **Test in Play**.
- **`IsDisabled` Variable binding** — the renderer field exists, but the wrapper does not auto-expose `[Property] IsDisabled` per element yet. Workaround: bind it through the Universal binding matrix (`Enabled` property). Tracked for V1.6.
- **Action Graph picker** on Code-mode `Action` slots — saves to scene fine but doesn't fire in Play (see [Events & Actions]({% link concepts/events-and-actions.md %}) § Known gap).

## See also

- [Interactive states concept]({% link concepts/interactive-states.md %}) — the mental model
- [Events & Actions]({% link concepts/events-and-actions.md %}) — Code vs Doo modes
- [Bindings]({% link concepts/bindings.md %}) — drive `ButtonText` / `IsDisabled` from a Variable
- [InventorySlot]({% link elements/inventory-slot.md %}) — also uses M3.5 interactive states
- [ItemIcon]({% link elements/item-icon.md %}) — also uses M3.5 interactive states
