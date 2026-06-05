---
layout: default
title: dialog_system
parent: Samples
nav_order: 5
permalink: /samples/dialog_system/
---

# dialog_system
{: .no_toc }

A branching NPC dialog showcase: portrait, speaker name, line of text revealed letter-by-letter (typewriter), and 1–3 choice buttons that branch through a small conversation tree. Click anywhere on the card while text is typing to skip the animation; click a choice when the line is complete to advance.
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

- TOC
{:toc}

---

## Overview

The conversation graph is six nodes, hardcoded in `BuildDialogTree()`. Eldrin the Gatekeeper offers a quest, warns about a cursed land, and either farewell or quest-accept ends the dialog. Real games would swap this for a JSON / asset-driven tree.

Stress-tests several pipeline areas at once:

1. **Per-frame mutation of an `ExposeAsVariable` Label** — the typewriter rewrites `DialogText.Text` every ~25 ms while a line is animating.
2. **Runtime `AddChild<Panel>` with `onclick` listeners** — choice buttons are created fresh on every node transition; old ones are deleted via `DeleteChildren(true)`.
3. **State-machine UI swap** — Portrait colour, SpeakerName, DialogText, and the choices set all change in lockstep on every node change.
4. **Card-level `onclick` for skip-typewriter** — listener attached on `Hud.View.Card` via the `@ref` capture one-shot, since the View is null at `OnStart`.

## Behavior

1. **Mount.** A 700×320 dark card appears in the lower-middle of the screen. Portrait (purple/indigo) at the top-left. `Eldrin the Gatekeeper` in amber bold over the dialog area. Text starts revealing one character at a time. A faded `[ click anywhere to skip ]` hint sits below the dialog while typing.
2. **Skip.** Click anywhere on the card while the text is still revealing → the full line snaps in, the hint fades, and the choice buttons appear.
3. **Choose.** Click any choice button → the controller looks up the choice's `NextNodeId`, calls `ApplyNode(nextId)`, which swaps Portrait colour, SpeakerName, DialogText, restarts the typewriter, and clears the previous choices.
4. **End.** A `NextNodeId == -1` choice (e.g. "Goodbye." or "I'm on my way.") closes the dialog via `Hud.Remove()`.

## Conversation graph

```
[0] Greeting
  ├── "Who are you?"          → [1]
  ├── "I'm looking for work." → [2]
  └── "Just passing through." → [4] (end)
[1] Lore reveal
  ├── "What lies beyond?"     → [3]
  ├── "Tell me about work."   → [2]
  └── "Farewell."             → [4] (end)
[2] Quest offer
  ├── "I'll take the job."    → [5] (accept)
  └── "Too dangerous."        → [4] (end)
[3] Warning
  ├── "I'm not afraid."       → [5] (accept)
  └── "I'll heed."            → [4] (end)
[4] Farewell                  → close
[5] Quest accepted (portrait turns green) → close
```

Node [5] swaps the portrait colour from indigo to green to signal the change in tone — same pattern would let a multi-NPC system use any colour palette per speaker.

## How to use

1. Open `dialog_system.sui` in the **SUI Designer** window and hit **Compile**. This writes `DialogSystemPanel.razor` + `.razor.scss` + `DialogSystem.cs` (the wrapper) into `Code/Samples/DialogSystem/`.
2. Add `DialogSystemPanel.User.scss` (see [Required `User.scss` rules](#required-userscss-rules) below) for the runtime choice-button styling.
3. Drop `DialogSystemController.cs` into the same folder.
4. Attach `DialogSystemController` to a GameObject in any scene and hit **Play**.

`TypewriterDelay` (default `0.025`) is exposed as a `[Property]` — lower for faster typing, higher for slower / more dramatic pacing.

## Required `User.scss` rules

The SUI compiler creates `DialogSystemPanel.User.scss` on first compile and never touches it again — drop this in:

```scss
DialogSystemPanel {
    .dialog-choice {
        flex-grow: 0;
        flex-shrink: 0;
        background-color: #1f2937;
        border: 1px solid #374151;
        border-radius: 4px;
        padding: 8px 16px;
        cursor: pointer;
        justify-content: center;
        align-items: center;
        transition: background-color 0.12s, transform 0.12s;
    }
    .dialog-choice:hover {
        background-color: #374151;
        transform: scale(1.03);
    }
    .dialog-choice:active {
        background-color: #111827;
        transform: scale(0.97);
    }
    .dialog-choice-label {
        color: #e5e7eb;
        font-size: 14px;
        font-weight: bold;
        text-align: center;
    }
}
```

## Controller architecture

- **`OnStart`** builds the dialog tree, mounts the panel, sets `_currentNodeId = 0`. Visual application is deferred to `OnUpdate` because `Hud.View?.X` is null until first paint.
- **`OnUpdate`** runs the bootstrap one-shot (attach card `onclick` + `ApplyNode(0)` once `@ref`s capture) and advances the typewriter (`if (!_typewriterDone && Time.Now >= _nextCharAt) ...`).
- **`ApplyNode(int)`** swaps Portrait colour, SpeakerName, DialogText, clears old choices, restarts the typewriter timer.
- **`OnCardClick`** is the skip handler — fast-forwards the typewriter index to the end and calls `FinishTypewriter()`.
- **`FinishTypewriter`** writes the full line, hides the skip hint, calls `SpawnChoices()`.
- **`SpawnChoices`** wipes the container and adds one `Panel` button per choice with an `onclick` capturing the `NextNodeId`.
- **`OnChoiceClick(int)`** — sentinel `-1` closes the dialog via `Hud.Remove()`; any other id calls `ApplyNode(id)`.

## Troubleshooting

Surprises specific to this sample, captured the hard way during development — check here before blaming the engine.

| Symptom | Cause | Fix |
|---|---|---|
| `Hud.View.DialogText` is always null, NRE the first time the typewriter ticks. | Codegen bug: `ExposeAsVariable=true` on a Text element declared the renderer field but never emitted the `@ref` capture onto the `<label>` tag, so the field stayed null forever. | Update to SUI Designer v1.5+ (commit `7199e37`). `EmitTextElement` now calls `EmitRazorRef` the same way container/input emitters do — recompile the `.sui` and the field captures. |
| Typewriter writes a few chars then the label snaps back to the original template text mid-line. | Direct mutation `Hud.View.DialogText.Text = "..."` is stomped by any panel re-render, which re-emits the static `Props.Text` from the template. | Mark the field as a Variable with a OneWay binding instead of `ExposeAsVariable`, and assign via the wrapper (`Hud.DialogText = "..."`). The binding survives re-renders. See `reference_sbox_variable_vs_exposeasvariable`. |
| Clicking a choice throws `IndexOutOfRangeException` deep inside `Sandbox.UI.Panel`. | `SpawnChoices` calls `DeleteChildren(true)` on `ChoicesContainer` while the engine is still iterating the click handler's parent chain. | Don't transition nodes inline from `OnChoiceClick`. Set `_pendingNodeId = id` and let `OnUpdate` call `ApplyNode(_pendingNodeId)` on the next frame, after the click stack unwinds. |
| Controller fails to compile: `The type or namespace name 'DialogSystem' could not be found`. | The wrapper class is generated by the SUI Designer on first compile of the `.sui` file — it does not exist until then. | Open `dialog_system.sui` in the SUI Designer window and hit **Compile** once. The wrapper (`DialogSystem.cs`) appears next to the `.razor` and the controller resolves. |

## Extending it

- **JSON / asset-driven tree.** Replace `BuildDialogTree()` with a `[ResourceType] DialogTreeAsset` GameResource that ships per NPC. Inspector picks the asset, controller loads `_nodes` from it.
- **Variable substitution.** Add `{playerName}` / `{questGold}` tokens to node Text strings and a `string ReplaceTokens(string)` pass before assigning to `_currentText`.
- **Voiced lines.** Each node gets an optional `SoundEvent VoiceLine`. `ApplyNode` plays it via `Sound.Play(...)` and the typewriter syncs to the audio length.
- **Conditional choices.** A `Func<bool>` predicate per `DialogChoice` (e.g. `() => Inventory.Has("Amulet")`). `SpawnChoices` skips entries whose predicate returns false.
- **Quest binding.** `NextNodeId = -1` already closes the dialog — add a sibling sentinel like `-2` meaning "accept quest", and call `QuestSystem.Start(questId)` before closing.
- **Speaker portraits via Image.** Replace the coloured Portrait panel with `slotPanel.AddChild<Image>()`, and put `Texture portrait` references on each node.
- **Re-open dialog with a hotkey.** Track `_dialogActive`; toggle `Hud.Show()`/`Hud.Remove()` on a hotkey, gated by proximity to the NPC GameObject.

## See also

- [Read the full `dialog_system` README on GitHub](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase/dialog_system).
- [Showcase samples]({% link reference/showcase-samples.md %}) — the full catalog with Variables / Bindings tables inlined.
- [Sample index]({% link reference/sample-index.md %}) — short index of every shipped sample.
- [Bindings]({% link concepts/bindings.md %}) — the OneWay binding pattern used by `DialogText` to survive panel re-renders.
- [Events & Actions]({% link concepts/events-and-actions.md %}) — background on the `@ref` capture one-shot used to attach the card `onclick`.
- [Wrapper generation]({% link concepts/wrapper-generation.md %}) — how `DialogSystem.cs` is produced from the `.sui`.
