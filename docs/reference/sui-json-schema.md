---
layout: default
title: SUI JSON schema
parent: Reference
nav_order: 1
---

# SUI JSON schema
{: .no_toc }

The on-disk format of a `.sui` file — what each field means, what's optional, and what values are valid.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Top-level document

```jsonc
{
  "SchemaVersion": 1,
  "DocumentId": "sui_my_hud_a3b2c1d4",
  "Name": "my_hud",
  "CreatedWith": "Sbox UI Designer",
  "DesignerVersion": "0.1.0",

  "Canvas": { /* SuiCanvasSettings */ },
  "Settings": { /* SuiDocumentSettings */ },
  "Output": { /* SuiOutputSettings */ },
  "Manifest": { /* SuiGeneratedFileManifest */ },

  "Elements": [ /* SuiElement[] */ ],

  "Events": [],
  "Animations": [],
  "Bindings": []
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `SchemaVersion` | int | yes | Always `1` in V1.0 |
| `DocumentId` | string | yes | Stable for life of doc; generated on creation |
| `Name` | string | yes | Matches `.sui` filename without extension |
| `CreatedWith` | string | yes | "Sbox UI Designer" |
| `DesignerVersion` | string | yes | e.g. "0.1.0" |
| `Canvas` | object | yes | Design canvas dimensions + safe area |
| `Settings` | object | yes | Snap-to-grid, grid size, etc. |
| `Output` | object | yes | Generated class name + namespace + output folder |
| `Manifest` | object | yes | Tracked generated files; auto-managed |
| `Elements` | array | yes | All elements in the document (flat) |
| `Events` | array | no | V1.5+; safe to omit or empty |
| `Animations` | array | no | V2+; safe to omit or empty |
| `Bindings` | array | no | V1.5+; safe to omit or empty |

## `Canvas` block

```jsonc
{
  "BaseWidth": 1920,
  "BaseHeight": 1080,
  "ScaleMode": "ScreenHeight1080",
  "BackgroundPreview": {
    "Type": "Color",
    "Color": "#101010",
    "ImagePath": null
  },
  "SafeArea": {
    "Enabled": false,
    "Left": 0, "Top": 0, "Right": 0, "Bottom": 0
  }
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `BaseWidth` | float | 1920 | Design width |
| `BaseHeight` | float | 1080 | Design height |
| `ScaleMode` | enum | `ScreenHeight1080` | One of: ScreenHeight1080, FixedResolution, Stretch, DesktopResolution |
| `BackgroundPreview` | object | — | Editor-only preview bg (not generated) |
| `SafeArea` | object | — | Optional designer overlay |

`BackgroundPreview.Type`: `Color`, `Image`, or `None`.

## `Output` block

```jsonc
{
  "ClassName": "MyHud",
  "Namespace": "Game.UI",
  "RootFolder": "Code/UI"
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `ClassName` | string | yes | C# identifier; sanitized from `Name` if empty |
| `Namespace` | string | no | Defaults to `Game.UI` |
| `RootFolder` | string | yes (after first compile) | Project-relative output folder |

## `Elements` array

Every element has the same shape. Discriminator is `Type` — different types use different subsets of `Props`.

```jsonc
{
  "Id": "el_a3f9b21c",
  "Name": "HealthBar",
  "Type": "ProgressBar",
  "ParentId": "root",
  "Children": [],

  "Flags": { /* SuiElementFlags */ },
  "Layout": { /* SuiLayoutData */ },
  "Style": { /* SuiStyleData */ },
  "Props": { /* SuiElementProps */ },

  "Notes": null,
  "TooltipText": null,
  "IsVisible": true,
  "ClassOverride": null,
  "StyleRef": null
}
```

### Element ID conventions

- The root element ALWAYS has `Id = "root"`.
- Other IDs match `el_[0-9a-f]{8}`.
- IDs are stable across renames and compiles.

### Element types (`Type` field)

One of these string values:

```
Canvas, Panel, Overlay, Text, Image, Button,
HorizontalBox, VerticalBox, Grid, ScrollPanel,
ProgressBar, InventoryGrid, InventorySlot, ItemIcon,
Tooltip, Hotbar
```

See [Element types reference]({% link reference/element-types.md %}) for what each does.

## `Flags` block

```jsonc
{
  "IsVariable": false,
  "Locked": false,
  "HiddenInDesigner": false
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `IsVariable` | bool | false | V1.5+ — expose as `[Property]` in generated C# |
| `Locked` | bool | false | Can't be moved/resized in canvas |
| `HiddenInDesigner` | bool | false | Hidden in canvas; still in doc + generated |

## `Layout` block

```jsonc
{
  "Mode": "Absolute",

  "X": 40, "Y": 40, "Width": 200, "Height": 18,
  "MinWidth": null, "MinHeight": null,
  "MaxWidth": null, "MaxHeight": null,
  "Anchor": "TopLeft",
  "PivotX": 0, "PivotY": 0,
  "ZIndex": 0,

  "FlexDirection": "Row",
  "JustifyContent": "FlexStart",
  "AlignItems": "Stretch",
  "FlexWrap": "NoWrap",
  "Gap": 0,

  "Margin": { "Left": 0, "Top": 0, "Right": 0, "Bottom": 0 },
  "Padding": { "Left": 0, "Top": 0, "Right": 0, "Bottom": 0 }
}
```

### `Mode`

- `Absolute` — child is positioned by X/Y + Anchor + Pivot inside parent.
- `Flex` — child is laid out by parent's flex container; X/Y/Anchor ignored.

When `Anchor` is `Stretch`/`StretchHorizontal`/`StretchVertical`, X/Y/Width/Height are **margins**, not absolute positions. See [Anchors and pivot]({% link concepts/anchors-and-pivot.md %}).

### Enums in this block

| Field | Values |
|---|---|
| `Mode` | Absolute, Flex |
| `Anchor` | TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight, BottomLeft, BottomCenter, BottomRight, Stretch, StretchHorizontal, StretchVertical |
| `FlexDirection` | Row, Column, RowReverse, ColumnReverse |
| `JustifyContent` | FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround, SpaceEvenly |
| `AlignItems` | FlexStart, Center, FlexEnd, Stretch, Baseline |
| `FlexWrap` | NoWrap, Wrap, WrapReverse |

## `Style` block

```jsonc
{
  "ClassName": "health-bar",
  "CustomClasses": [],
  "BackgroundColor": "#22222288",
  "BorderColor": null,
  "BorderWidth": 0,
  "BorderRadius": 0,
  "Opacity": 1,
  "Visibility": "Visible",
  "PointerEvents": "None",
  "Overflow": "Visible"
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `ClassName` | string | — | Used as the CSS class for the element |
| `CustomClasses` | string[] | empty | Extra classes appended after ClassName |
| `BackgroundColor` | string (color) | null | Hex / rgb / rgba; null = no rule |
| `BorderColor` | string (color) | null | Must be paired with `BorderWidth > 0` |
| `BorderWidth` | float | 0 | px |
| `BorderRadius` | float | 0 | px |
| `Opacity` | float | 1 | 0..1; cascades to children |
| `Visibility` | enum | Visible | Visible / Hidden / Collapsed |
| `PointerEvents` | enum | None | None / All |
| `Overflow` | enum | Visible | Visible / Hidden / Scroll |

Color formats accepted: `#RRGGBB`, `#RRGGBBAA`, `#RGB`, `rgb(r,g,b)`, `rgba(r,g,b,a)`.

## `Props` block — flat bag

A single bag with all type-specific fields. The generator and validator only read fields relevant to the element's `Type`.

### Text fields (used by Text, Button)

```jsonc
{
  "Text": "",
  "FontSize": 16,
  "FontFamily": null,
  "FontWeight": "Normal",
  "Color": "#ffffff",
  "TextAlign": "Left",
  "LineHeight": null,
  "LetterSpacing": 0,
  "TextOverflow": "Clip",
  "TextSizeMode": "Auto",
  "VerticalAlign": "Top"
}
```

| Field | Values / range |
|---|---|
| `FontWeight` | Normal, Bold, Light, Medium, SemiBold, ExtraBold |
| `TextAlign` | Left, Center, Right, Justify |
| `TextOverflow` | None, Ellipsis, Clip |
| `TextSizeMode` | Auto, Fixed, AutoHeightWrap |
| `VerticalAlign` | Top, Center, Bottom (used only when TextSizeMode = Fixed) |

### Image fields

```jsonc
{
  "ImagePath": "ui/icons/sword.png",
  "Tint": "#ffffff",
  "FitMode": "Contain",
  "BackgroundPosition": "Center"
}
```

| Field | Values |
|---|---|
| `FitMode` | Contain, Cover, Stretch, None |
| `BackgroundPosition` | Center, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight |

### Grid / InventoryGrid / Hotbar fields

```jsonc
{
  "Columns": 6,
  "Rows": 4,
  "CellWidth": 64,
  "CellHeight": 64,
  "GridGap": 4,
  "AutoFill": false,
  "GridStrategy": "WrappedFlex"
}
```

| Field | Values |
|---|---|
| `GridStrategy` | WrappedFlex (recommended), AbsoluteSlots |

### Button

```jsonc
{
  "ButtonText": "Click me"
}
```

### ProgressBar

```jsonc
{
  "ProgressMin": 0,
  "ProgressMax": 100,
  "ProgressPreviewValue": 75,
  "ProgressFillColor": "#4ade80",
  "ProgressDirection": "LeftToRight"
}
```

| Field | Values |
|---|---|
| `ProgressDirection` | LeftToRight, RightToLeft, BottomToTop, TopToBottom |

### Text wrap

```jsonc
{
  "AutoWrapText": false,
  "WrapTextAt": 0
}
```

`AutoWrapText = true` is equivalent to `TextSizeMode = AutoHeightWrap`. `WrapTextAt` is the max width in px (0 = element's `Width`).

### InventorySlot / ItemIcon

```jsonc
{
  "SlotIndex": 0,
  "PreviewIconPath": "ui/icons/health_potion.png",
  "PreviewCount": 5
}
```

`PreviewCount` shows as a badge in the canvas. **Currently not emitted by the runtime Razor** — see [Known issues]({% link reference/known-issues.md %}#issue-005).

## Tree integrity rules

The validator enforces:

- Exactly one element with `ParentId = null` (the root).
- Root has `Id = "root"`.
- Every `ParentId` matches an existing element's `Id` (no orphans).
- Every entry in `Parent.Children` exists in the document and has `ParentId` pointing back.
- No cycles in the parent chain.
- Element IDs are unique.

If a hand-edited document violates these, the validator surfaces errors and refuses to compile. Some violations (parent/child link drift) are auto-repaired.

## Minimal valid document

The smallest possible `.sui`:

```json
{
  "SchemaVersion": 1,
  "DocumentId": "sui_empty_a3b2c1d4",
  "Name": "empty",
  "Canvas": { "BaseWidth": 1920, "BaseHeight": 1080 },
  "Output": { "ClassName": "Empty" },
  "Elements": [
    {
      "Id": "root",
      "Name": "Root",
      "Type": "Canvas",
      "ParentId": null,
      "Children": [],
      "Layout": { "Mode": "Absolute", "Width": 1920, "Height": 1080 },
      "Style": { "ClassName": "root" }
    }
  ]
}
```

All other fields default to safe values.

## Serialization details

- **Encoding**: UTF-8 without BOM.
- **Library**: `System.Text.Json` with default options.
- **Pretty-printed** when saved from the designer for git-friendliness.
- **Field casing**: PascalCase (matches C# types).
- **Enums**: serialized as strings, not numbers.
- **Nulls**: emitted explicitly (so the schema is self-documenting).

## See also

- [Document model]({% link architecture/document-model.md %}) — internal representation
- [Element types reference]({% link reference/element-types.md %}) — per-type field matrix
