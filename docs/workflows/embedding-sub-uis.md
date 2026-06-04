---
layout: default
title: Embedding sub-UIs
parent: Workflows
nav_order: 9
---

# Embedding sub-UIs
{: .no_toc }

Drop a `.sui` inside another `.sui` via `SuiReference`. Pass per-instance Props. Use ForEach for dynamic lists. The visual composition workflow.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Workflow — single embed

### 1. Build the child

Create a reusable widget — say `progress_bar.sui` — with the design + the Variables it needs:

- `ActualValue: float` (Default 50, IsPublic = **true**)
- `MaxValue: float`    (Default 100, IsPublic = **true**)
- `FillColor: Color`   (Default #4ade80, IsPublic = **true**)

Bind `ProgressBar.Value` to `ActualValue` (with a `Divide(MaxValue)` chain if you want 0..1 normalization). Bind `ProgressBar.FillColor` to `FillColor`.

Save + Compile. The generator emits `Game.UI.ProgressBar` (wrapper) + `Game.UI.ProgressBarPanel` (renderer).

### 2. Drop it into the parent

Open the parent — say `hud.sui`. The **USER WIDGETS** dynamic palette category now lists `progress_bar` as its own item (driven by `SuiAssetRegistry`).

Drag `progress_bar` from USER WIDGETS onto the canvas. A `SuiReference` element appears at your drop point.

### 3. Configure the embed

In the Details panel for the new SuiReference:

- **Name** — change to `StaminaBar` (must be unique within the document).
- **Layout / Style** — position + size as usual. The child's tree paints inside this rect.
- **Props** — the Variables flagged `IsPublic` on the child appear here as typed editors. Override per-instance:
  - `MaxValue: 100` (uses Default)
  - `FillColor: #fbbf24` (override — staminbar is amber, not green)

The canvas re-paints the child with the new values immediately.

### 4. Compile + use

Save the parent. Compile. The wrapper now has:

```csharp
[Property, Group("Children")]
public global::Game.UI.ProgressBar StaminaBar { get; set; } = new();
```

From gameplay code:

```csharp
[Property] public Game.UI.Hud Hud { get; set; } = new();

Hud.Show();
Hud.StaminaBar.ActualValue = 75;        // direct C# property access by name
Hud.StaminaBar.FillColor   = Color.Red; // override the design-time pick
Hud.StaminaBar.Hide();                  // embedded — toggles visibility only
```

## Workflow — ForEach

### 1. Design the per-item child

Build a `chat_line.sui` that renders one line of chat. Declare its IsPublic Variables:

- `MessageText: string`
- `SenderColor: Color`

Bind a Text element's `Text` to `MessageText` and its `Color` to `SenderColor`. Save + Compile.

### 2. Declare the list Variable on the parent

In your parent `hud.sui`:

- **Variables** tab → **+ Add Variable** → Name: `Messages`, Type: `List<ChatMessage>`.

`ChatMessage` is your gameplay-side POCO:

```csharp
public sealed class ChatMessage
{
    public string Text { get; set; }
    public Color  Color { get; set; }
}
```

The Variables dialog accepts the type name once the type compiles in your project.

### 3. Drop a ForEach SuiReference

Drag `chat_line` from USER WIDGETS onto the parent canvas. The SuiReference appears.

In Details:

- **Name** — `MessagesContainer` (the C# field name on the wrapper).
- **ForEach** section → click **Enable** → pick `Messages` as the source.
- For each child Variable, type the per-item expression:
  - `MessageText` ← `@item.Text`
  - `SenderColor` ← `@item.Color`

ForEach iterates the source Variable **as-is** — the list stores your `ChatMessage` POCOs. The child wrapper (`ChatLine`) is only what gets *rendered* per item; it is not what is *stored*.

The canvas renders one preview child (the first item, if any).

### 4. Compile + use

The wrapper field type matches the Variable's TypeRef — `List<ChatMessage>`, not `List<ChatLine>`:

```csharp
[Property] public List<global::Game.ChatMessage> MessagesContainer { get; set; } = new();
```

From code, construct the POCO (`ChatMessage`), not the child wrapper:

```csharp
Hud.MessagesContainer.Add( new ChatMessage { Text = "Hello!", Color = Color.Green } );
Hud.MessagesContainer.Add( new ChatMessage { Text = "Hi back", Color = Color.Cyan } );
Hud.MessagesContainer[0] = new ChatMessage { Text = "Updated!", Color = Color.Yellow };
```

The parent re-renders automatically (recursive `ContentHash` picks up the list mutation).

### Primitive lists

ForEach also works with `List<string>`, `List<int>`, etc:

```
ForEach config:
  source: PartyMemberNames (List<string>)
  mapping: child.Caption ← @item
```

## Hide / Show on an embedded wrapper

Calling `Hide()` on an embedded wrapper flips its `IsShown` flag — the parent's next render emits the child's tag wrapped in an `@if` guard so the embed disappears from layout:

```csharp
Hud.StaminaBar.Hide();   // recursive ContentHash propagates; parent re-renders without the tag
Hud.StaminaBar.Show();   // tag re-appears
```

**Do NOT call `Remove()` on an embedded wrapper** — embedded wrappers don't own a mount. (Designer error path emits a warning.)

## Cycle protection

`SuiReferenceCycleDetector` runs on save. If you accidentally embed `hud.sui` inside something it transitively contains, you get a Compile Results error with the cycle chain named. The USER WIDGETS palette filters the host document from its own list to prevent the obvious one-step cycle without a modal.

## Per-instance vs per-document defaults

| Where you set | Effect |
|---|---|
| Child's Variable Default | Default for every embed instance that doesn't override |
| Parent's SuiReference Props | Per-embed override of that one IsPublic Variable |
| Gameplay code (`Hud.StaminaBar.ActualValue = 75`) | Runtime override — wins over both above for the current frame |

## See also

- [Composition concept]({% link concepts/composition.md %}) — the full mental model
- [SuiReference element]({% link elements/sui-reference.md %})
- [Wrapper generation]({% link concepts/wrapper-generation.md %})
- [Variables]({% link concepts/variables.md %}) — `IsPublic` for Props editor
