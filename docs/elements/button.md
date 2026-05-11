---
layout: default
title: Button
parent: Element reference
nav_order: 5
---

# Button

A clickable region with a centered text label. Has `Pointer Events: All` by default so it catches input.

## Properties (Button section)

| Field | Default | Notes |
|---|---|---|
| **Button Text** | `""` | Caption shown inside the button |

Text styles (Font Size, Weight, Color, Text Align, Vertical Align) come from the **Text** section — they apply to the inner `<label>`.

## Generated output

```html
<div class="primary-btn sui-respawn-btn">
  <label class="label">Respawn</label>
</div>
```

```scss
.sui-respawn-btn {
  width: 260px;
  height: 48px;
  background-color: #dc2626;
  border-color: #fca5a5;
  border-width: 2px;
  border-radius: 6px;
  display: flex;
  flex-direction: row;
  justify-content: center;
  align-items: center;
  .label {
    font-size: 18px;
    font-weight: 700;
    color: #ffffff;
    text-align: center;
  }
}
```

The button uses `display: flex` + `justify-content: center` + `align-items: center` to center the label.

## Wiring an OnClick

V1 generator only emits the markup. To handle clicks:

1. Generate the Razor.
2. Create a `<ClassName>.User.cs` file next to the generated `.razor` (V1 doesn't auto-generate this — you write it by hand).
3. Make it a partial class and add the handler:

```csharp
public partial class MyHud
{
    void OnRespawnClick()
    {
        // … your logic
    }
}
```

4. In the Razor (which you can edit OUTSIDE the `SUI:GENERATED` markers — V1.5 will support inline events), attach the handler:

```razor
<div class="primary-btn sui-respawn-btn" @onclick="OnRespawnClick">
  ...
</div>
```

Event wiring will be first-class in V1.5 — see [Roadmap]({% link support/changelog.md %}#v1-5).

## Tips

- Set **Pointer Events: All** if it's somehow gotten set to None (default for buttons).
- For hover/active styles, use the `.User.scss` sidecar:

```scss
.respawn-btn:hover { background-color: #ef4444; }
.respawn-btn:active { background-color: #b91c1c; }
```

(`:hover` / `:active` pseudo-classes work in s&box CSS for elements with `Pointer Events: All`.)
