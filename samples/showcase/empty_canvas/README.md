# Empty Canvas

The bare minimum SUI document. One Canvas, one centered Panel, one Text. Zero Variables, zero Bindings, zero Events.

If this sample doesn't render on your screen, nothing else in the showcase will. That's its job: prove the wrapper-mounting plumbing works end-to-end before you start adding complexity.

## What you'll see

A single line of bold white text reading **"Hello SUI!"** rendered at 48pt in the dead-center of your screen. No background, no border, no decoration — just text floating on whatever the game is drawing behind it.

## How to use

1. Open the `empty_canvas.sui` document in the SUI Designer and click **Generate** to produce `EmptyCanvas.razor` / `EmptyCanvas.razor.scss` under `Code/Samples/EmptyCanvas/`.
2. In your scene, create an empty GameObject and add the `EmptyCanvasController` component to it.
3. Hit **Play**. The text appears immediately.

That's it. No wiring, no inspector fields to fill in.

## Variables exposed

| Name | Type | Role |
| ---- | ---- | ---- |
| _(none)_ | — | This sample intentionally has no Variables. |

## Bindings

| Element | Property | Variable | Mode |
| ------- | -------- | -------- | ---- |
| _(none)_ | — | — | — |

The `Text` value `"Hello SUI!"` is baked into the document as a literal Prop — no binding involved.

## Events

| Element | Event | Handler |
| ------- | ----- | ------- |
| _(none)_ | — | — |

`SuiInputMode.Passive` means the panel does not receive mouse or keyboard input at all, so there's nothing for events to fire on.

## Extending it

A few small edits turn this into a real starting template:

- **Change the text color.** Open `empty_canvas.sui`, select `HelloText`, and set `Props.Color` to anything you like.
- **Make the text dynamic.** Add a `Variable` named `Greeting` of type `string`, then bind `HelloText.Text` to it. Drive it from `EmptyCanvasController.OnUpdate` with `Hud.Greeting = $"FPS: {Time.Now:F0}";`.
- **Add a background card.** Give `CenterPanel` a `BackgroundColor` (e.g. `#000000aa`), `BorderRadius` (e.g. `8`), and some `Padding` to wrap the text in a chrome-y card.
- **React to clicks.** Switch `Hud.Show( SuiInputMode.MouseOnly )` and add an `OnClick` event on `CenterPanel` in the designer's Events tab — it'll generate a partial method stub for you to fill in on `EmptyCanvasController`.
- **Animate the entrance.** Add a `FadeIn` animation in the designer's Animations tab and trigger it from `OnStart` after `Show(...)`.
