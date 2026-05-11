---
layout: default
title: Image
parent: Element reference
nav_order: 4
---

# Image

Renders an image asset. Internally uses `background-image` on a `<div>` so it can clip with `border-radius` and tint via `background-image-tint`.

## Properties (Image section)

| Field | Default | Notes |
|---|---|---|
| **Image Path** | `""` | Path to the image asset, relative to project root (e.g. `ui/InventoryAssets/InventoryBG.png`) |
| **Tint** | `#ffffff` | Multiplied with the image pixels. White = no tint. Use to recolor sprites or flash on damage |
| **Fit Mode** | Contain | Stretch / Contain / Cover / None |
| **Background Position** | Center | Center / Left / Right / Top / Bottom / corners — placement when Fit leaves empty space |

## Fit Mode

| Mode | Behavior |
|---|---|
| **Stretch** | Image fills the element ignoring aspect ratio (`background-size: 100% 100%`) |
| **Contain** | Image scales to fit inside, keeps ratio. May letterbox |
| **Cover** | Image scales to fill, keeps ratio. May crop |
| **None** | Image uses original native size |

## Generated output

```html
<div class="my-icon sui-my-icon"></div>
```

```scss
.sui-my-icon {
  width: 96px;
  height: 96px;
  background-image: url("ui/InventoryAssets/InventoryBG.png");
  background-size: contain;
  background-position: center;
  background-repeat: no-repeat;
  background-image-tint: #ef4444;
}
```

`background-repeat: no-repeat` is always emitted to match canvas paint (CSS default is `repeat`, which would tile small images).

## Canvas vs runtime

Both use the same `background-size` semantics. Canvas paint clips to `border-radius` via `Editor.Paint.Draw(rect, pixmap, alpha, radius)`. Runtime SCSS clips via `border-radius` natively.

## Tips

- **Asset paths are forward-slash strings rooted at the project**, NOT under `Assets/`. The image at `Assets/ui/icon.png` is referenced as `ui/icon.png`.
- For sprite recoloring, set the source image to grayscale/white and use **Tint** to color it at runtime.
- For circular icons: set **Border Radius** to half the element size (e.g. 48 for a 96×96 element).
- Use **InventorySlot** or **ItemIcon** instead of plain Image when you want the stack count overlay rendered.
