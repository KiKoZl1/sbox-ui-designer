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

## Schema version (V1.5)

`SchemaVersion` is bumped to **V3** in V1.5. The history:

- **V1** — V1.0 baseline.
- **V2** — V1.5 M1/M2 — adds Variables (with `IsPublic` flag) + Bindings + Events + SuiReference element type.
- **V3** — V1.5 M3.5 (PRD 25) — adds per-state interactive style overrides (`HoverStyle / PressedStyle / DisabledStyle / FocusedStyle`) plus `IsDisabled` / `Transition*` / `*Sound` / `Cursor` / `ButtonShape` / `BackgroundSize` / `BackgroundImage` fields on `SuiElementProps`.

Loaders accept any version ≥ 1; the migration pipeline (`SuiDocumentMigration.Apply`) bumps every load to Current. Saves always write Current.

Every V1 field still loads — the schema is fully additive (see [Upgrade guide]({% link UPGRADE_V1_0_TO_V1_5.md %})).

## Top-level document

```jsonc
{
  "SchemaVersion": 3,
  "DocumentId": "sui_my_hud_a3b2c1d4",
  "Name": "my_hud",
  "CreatedWith": "Sbox UI Designer",
  "DesignerVersion": "0.1.0",

  "Canvas": { /* SuiCanvasSettings */ },
  "Settings": { /* SuiDocumentSettings */ },
  "Output": { /* SuiOutputSettings */ },
  "Manifest": { /* SuiGeneratedFileManifest */ },

  "Variables": [ /* SuiVariable[] — V2+ */ ],
  "PreviewData": null,        /* deferred — see DEVIATIONS D-009 */

  "Elements": [ /* SuiElement[] — each element carries its own Bindings + Events */ ],
  "Animations": []
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `SchemaVersion` | int | yes | `1` (V1.0) / `2` (V1.5 M1/M2) / `3` (V1.5 M3.5+) |
| `DocumentId` | string | yes | Stable for life of doc; generated on creation |
| `Name` | string | yes | Matches `.sui` filename without extension |
| `CreatedWith` | string | yes | "Sbox UI Designer" |
| `DesignerVersion` | string | yes | e.g. "0.1.0" |
| `Canvas` | object | yes | Design canvas dimensions + safe area |
| `Settings` | object | yes | Snap-to-grid, grid size, etc. |
| `Output` | object | yes | Generated class name + namespace + output folder |
| `Manifest` | object | yes | Tracked generated files; auto-managed |
| `Variables` | array | V2+ | Typed UI-local state. See [Variables]({% link concepts/variables.md %}) |
| `Elements` | array | yes | All elements in the document (flat) |
| `Animations` | array | reserved | V2 future |

**No top-level `Events` or `Bindings` arrays.** V1.5 schema moved both onto `SuiElement` itself (`SuiElement.Bindings: List<SuiBinding>` + `SuiElement.Events: Dictionary<string, SuiEventBinding>`). The document-level event list existed in V1.0 schemas; the V1→V2 migration moved them per-element, and the pre-M3 cleanup removed the empty top-level field. See `Code/Runtime/SuiDocument.cs` lines 55-59 for the historical note.

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

One of these string values (V1.5):

```
Canvas, Panel, Overlay, Text, Image, Button,
HorizontalBox, VerticalBox, Grid, ScrollPanel,
ProgressBar, InventoryGrid, InventorySlot, ItemIcon,
Tooltip, Hotbar,

// V1.5 additions:
SuiReference,
TextEntry, Slider, Toggle, DropDown
```

See [Element types reference]({% link reference/element-types.md %}) for what each does.

### V1.5 — Element-level fields

Every `SuiElement` carries the V1.5 extensions:

```jsonc
{
  "Id": "el_a3f9b21c",
  "Type": "Button",
  // ... (everything from V1)

  "Bindings": [ /* SuiBinding[]                         — per-property bindings */ ],
  "Events":   { /* Dictionary<string, SuiEventBinding>  — Code/Doo handlers, keyed by slot name */ },
  "SuiReference": null   /* SuiReferenceData when Type == SuiReference */
}
```

`Events` is a dictionary (slot name → binding), NOT an array. Empty / nulls are legal — V1 documents load with all three empty.

### V1.5 — `SuiReference`-specific fields

```jsonc
{
  "Type": "SuiReference",
  "Name": "StaminaBar",
  "SuiReference": {
    "SourceGuid": "sui_child_doc_guid",
    "Props": {
      "var_a3f9b21c": 75,                  // per-instance override of an IsPublic Variable, keyed by VariableId
      "var_b4c5d6e7": "Health bar"
    },
    "ForEach": null
  }
}
```

When `ForEach` is set:

```jsonc
"ForEach": {
  "SourceVariableId": "var_messages",   // a Variable on this document, typed List<T>
  "ItemPropId":       null,              // reserved — not consumed by V1.5 codegen
  "IndexPropId":      null               // reserved
}
```

Items in the bound `List<T>` need member names matching the child's `IsPublic` Variable names — the Razor generator forwards every public Variable as `<ChildPanel VarName=@(__item?.VarName ?? default) />`. See [Composition]({% link concepts/composition.md %}).

## `Flags` block

```jsonc
{
  "Locked": false,
  "HiddenInDesigner": false
}
```

| Field | Type | Default | Notes |
|---|---|---|---|
| `Locked` | bool | false | Can't be moved/resized in canvas |
| `HiddenInDesigner` | bool | false | Hidden in canvas; still in doc + generated |

> **Removed 2026-05-29 (pre-M3 cleanup):** `IsVariable` was a V1.5 stub that never wired into codegen. Existing `.sui` files with `"IsVariable": false` still load — the deserializer ignores the field. The M3 `ExposeAsVariable` flag (PRD 20 § 5.2) takes over the "expose to C#" role.

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

`PreviewCount` shows as a badge in the canvas. **Currently not emitted by the runtime Razor** — see [Known issues]({% link reference/known-issues.md %}#issue-005--previewcount-badges-not-emitted-in-runtime).

## `Variables` block (V1.5)

Top-level array. Each entry:

```jsonc
{
  "Id": "var_a3f9b21c",
  "Name": "Health",
  "Type": "int",                 // see § Variable types below
  "Default": 100,                // JSON node; shape matches Type
  "Description": "Current HP",
  "IsAdvanced": false,
  "IsPublic": false,             // V1.5-M2-K — when true, parent-settable + reachable
  "Group": "Stats",
  "ResourceType": null,          // only meaningful when Type = "Resource"
  "Source": { "Kind": "Manual" } // SuiVariableSource — only Manual ships in V1.5 (D-017)
}
```

### Variable types (`Type` field)

Closed set (PRD 18 § 3.3):

- Primitives: `string`, `int`, `long`, `float`, `bool`
- Engine: `Color`, `Vector2`, `Vector3`, `Vector4`, `Angles`, `Rotation`, `Transform`
- Assets: `Texture`, `Resource` (set `ResourceType` to a concrete type name)
- Generic enums: `Enum:<full type name>` (e.g. `Enum:MyMod.Difficulty`)
- Components: `Component:<full type name>`
- Lists: `List<T>` where `T` is any of the above (e.g. `List<string>`, `List<ChatMessage>`)

## `Bindings` block (per-element)

Lives on `SuiElement.Bindings`. Each entry:

```jsonc
{
  "Id": "bind_c4d6e7a8",
  "Property": "Value",                  // target property name
  "Mode": "OneWay",                     // OneTime / OneWay / TwoWay / OneWayToSource
  "UpdateTrigger": "OnChange",          // V1.5 D-028 — OnChange / OnLostFocus / OnSubmit / OnRelease / Manual
  "Source": {
    "VariableId": "var_a3f9b21c"           // GUID of a Variable on this document
  },
  "Converters": [                        // chain (left → right)
    {
      "ConverterRef": "builtin.Clamp",
      "Args": [
        { "Kind": "ChainRef" },          // implicit chain feed
        { "Kind": "Literal", "Type": "float", "Value": 0 },
        { "Kind": "Literal", "Type": "float", "Value": 100 }
      ]
    },
    { "ConverterRef": "builtin.FloatToInt" }
  ],
  "FallbackValue": null                  // JSON node; null = property's type default
}
```

Allowed `Mode` values per `(Type, Property)` come from `SuiBindingModeMatrix` — see [Binding-mode matrix]({% link reference/binding-mode-matrix.md %}). Allowed `UpdateTrigger` values come from `SuiBindingModeMatrix.AllowedUpdateTriggers` — see [Update-trigger matrix]({% link reference/update-triggers.md %}).

## `Events` block (per-element)

Lives on `SuiElement.Events`, a **`Dictionary<string, SuiEventBinding>`** keyed by event slot name (from `SuiEventMatrix`):

```jsonc
"Events": {
  "OnClick": {
    "Mode": "Code",                     // Code / Doo
    "Handler": "OnFireClick",           // C# identifier — when Mode == Code, emits [Property] Action <Handler>
    "DooPropertyName": null,            // C# identifier — when Mode == Doo, emits [Property] Doo <DooPropertyName>
    "DooBody": null                     // Sandbox.Doo (IJsonConvert) — embedded default body when Mode == Doo
  },
  "OnHover": {
    "Mode": "Doo",
    "Handler": null,
    "DooPropertyName": "OnHoverFx",
    "DooBody": { "body": [/* serialised Doo BlockTree */] }
  }
}
```

The two slots above coexist on a single Button. Codegen picks the live mode per slot. See [Events & Actions]({% link concepts/events-and-actions.md %}) and PRD 20 § 3.

## V1.5 M3.5 — Interactive state + button-polish fields on `SuiElementProps`

Applies to `Button` / `InventorySlot` / `ItemIcon`. All additive, default values match V2 behaviour so V2 → V3 migration is a no-op.

```jsonc
{
  // ...existing element Props...

  "ButtonShape": "Rectangle",       // SuiButtonShape — Rectangle / Square / Round / Pill / Custom

  // Interactive state overrides — each is a SuiInteractiveStateStyle or null
  "HoverStyle":    { /* ... */ },
  "PressedStyle":  null,
  "DisabledStyle": null,
  "FocusedStyle":  null,

  "IsDisabled": false,              // runtime-bindable; adds .disabled class when true
  "TransitionEnabled": true,
  "TransitionDuration": 0.15,       // seconds
  "HoverSound": "ui/hover.sound",
  "PressSound": "",
  "Cursor": "Pointer"               // SuiCursor — Default / Pointer / NotAllowed / Wait / Text / Move / Crosshair / Help / None
}
```

`SuiInteractiveStateStyle` shape:

```jsonc
{
  "BackgroundColor": "#ef4444",
  "BorderColor":     null,
  "BorderWidth":     null,
  "BorderRadius":    null,
  "TextColor":       null,
  "Scale":           1.0,
  "Opacity":         null,
  "BackgroundImage": "ui/buttons/red_hover.png"
}
```

Null fields inherit Normal-state values. Note: `BackgroundSize` is an element-level prop on `SuiElementProps` (see § V1.5 M3.5 — Interactive state + button-polish fields) — not a per-state override. See [Interactive states]({% link concepts/interactive-states.md %}).

## V1.5 M4 — Input-widget fields on `SuiElementProps`

```jsonc
// TextEntry
"PlaceholderText": "",
"MaxLength": 0,                 // 0 = unbounded
"ReadOnly": false,
"PreviewValue": "",

// Slider
"SliderMin":  0.0,
"SliderMax":  100.0,
"SliderStep": 1.0,
"SliderOrientation": "Horizontal", // future-proof; V1.5 ships horizontal only (PRD 21 § 11 #2)
"SliderTrackColor":  "#22222288",
"SliderFillColor":   "#4ade80",
"SliderHandleColor": "#ffffff",
"SliderShowValue":   false,
"SliderValue":       50.0,
"SliderTooltipBgColor":   "#000000",
"SliderTooltipTextColor": "#ffffff",

// Toggle
"ToggleChecked":   false,
"ToggleLabelText": "",

// DropDown
"DropDownOptions":       ["Low", "Medium", "High"],
"DropDownSelectedIndex": 0
```

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
