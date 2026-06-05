# SUI Designer Showcase Samples

A curated set of **16 polished demos** that cover every V1.5 feature of the SUI Designer — from a single centered label to a runtime-driven drag-and-drop inventory. Each sample folder is self-contained: open the `.sui`, drop the companion `Controller.cs` onto a `GameObject`, press Play. Use this page to find the closest sample to what you want to build, copy the pattern, and ship.

Every sample ships a `.sui` document, a `<Name>Controller.cs`, and a per-folder `README.md` with Variables / Bindings / Events tables. **Per-sample READMEs are the primary docs** — this page is the catalog that helps you find the right one.

## Quick start

```text
samples/showcase/<sample_name>/
  <sample_name>.sui            UI definition — open in Designer + Compile
  <SampleName>Controller.cs    drop on any GameObject
  README.md                    full setup + Variables/Bindings tables
```

1. Open `samples/showcase/<sample>/<sample>.sui` in the **SUI Designer** window (`Window -> Sbox UI Designer`) and hit **Compile**. This writes the generated `.razor` + `.scss` + wrapper `.cs` under `Code/Samples/<SampleName>/`.
2. Add the matching `<SampleName>Controller.cs` to your project (anywhere under `Code/`).
3. Create a `GameObject` in any scene, attach the `<SampleName>Controller` Component, press Play.

## Browse by category

### Starter (3)

The smallest possible end-to-end documents. Work through these first if you are new to SUI Designer.

| Sample | What it teaches | Concepts | Difficulty |
|---|---|---|---|
| [empty_canvas](./empty_canvas/) | Minimum viable SUI document — wrapper mount + `Hide()` on destroy. | Anchoring | Beginner |
| [label_clock](./label_clock/) | OneWay binding from gameplay -> UI Variable, driven from `OnUpdate`. | Variables, OneWayBinding, ManualBinding, Anchoring | Beginner |
| [counter_button](./counter_button/) | First Code-mode `OnClick` + Variable update from event. | Variables, OneWayBinding, Events_Code, Button | Beginner |

### Input widgets (2)

Samples that exercise the V1.5 input widget set and the canonical commit patterns.

| Sample | What it teaches | Concepts | Difficulty |
|---|---|---|---|
| [toggle_pause](./toggle_pause/) | Smallest possible TwoWay binding — `Toggle.Checked` round-trip. | Variables, TwoWayBinding, OnChangeBinding, Toggle | Intermediate |
| [settings_full](./settings_full/) | Every input widget (`TextEntry` / `Slider` / `Toggle` / `DropDown`) plus the `Apply.All()` save pattern with dirty detection. | TextEntry_Manual, Slider, DropDown, Toggle, AppApply | Intermediate |

### Interactive states (3)

Reactive HUDs that drive multiple bindings from gameplay events.

| Sample | What it teaches | Concepts | Difficulty |
|---|---|---|---|
| [health_bar](./health_bar/) | Two OneWay-bound Variables (float fraction + string label) driving a `ProgressBar` + text. | Variables, OneWayBinding, ManualBinding, ProgressBar | Intermediate |
| [boss_hp_bar](./boss_hp_bar/) | Phase markers + ZIndex-layered flash overlay + `ExposeAsVariable` Style writes. | OneWayBinding, ExposeAsVariable, ProgressBar, Anchoring | Intermediate |
| [death_respawn_modal](./death_respawn_modal/) | Six OneWay text bindings + countdown-gated Code-mode click + single-variable button-label swap. | Variables, OneWayBinding, Events_Code, Button | Intermediate |

### Runtime-rendered (7)

Samples that AddChild / mutate the visual tree at runtime via `ExposeAsVariable`.

| Sample | What it teaches | Concepts | Difficulty |
|---|---|---|---|
| [chat_panel](./chat_panel/) | Manual TextEntry + `Apply.All()` commit + rebuild a message list every send. | ManualBinding, TextEntry_Manual, ExposeAsVariable, RuntimeAddChild | Advanced |
| [dialog_system](./dialog_system/) | Branching NPC tree with typewriter text + deferred mutation outside the event dispatch loop. | OneWayBinding, ExposeAsVariable, RuntimeAddChild, FlexLayout | Advanced |
| [drag_drop_inventory](./drag_drop_inventory/) | Two 4x4 grids with cursor-following ghost + hit-test on mouse-up. | ExposeAsVariable, Events_Code, RuntimeAddChild, FlexLayout | Advanced |
| [inventory_grid_full](./inventory_grid_full/) | 6x4 grid exposed as a Variable so the controller wires hover/click/right-click/double-click after mount. | ExposeAsVariable, ChildContainer, GridLayout, Events_Code | Flagship |
| [loadout_selector](./loadout_selector/) | Class-card grid + detail panel — five Code-mode buttons drive six Variables to redraw stats. | OneWayBinding, Events_Code, ProgressBar, CssTransitions | Advanced |
| [notification_toast_queue](./notification_toast_queue/) | Stacking auto-dismissing toasts via FlexDirection + CSS transitions + frame-staggered class flips. | ExposeAsVariable, RuntimeAddChild, CssTransitions, FlexLayout | Advanced |
| [quest_journal](./quest_journal/) | Multi-tab nav driven entirely by `IsHighlighted` + `HighlightedStyle` — controller just flips bools. | ExposeAsVariable, IsHighlighted, ProgressBar, Button | Advanced |

### Full-feature showcase (1)

| Sample | What it teaches | Concepts | Difficulty |
|---|---|---|---|
| [survival_hud_aaa](./survival_hud_aaa/) | Every Variable type (float / string / bool / Color) and every common binding target in one document. | Variables, OneWayBinding, ProgressBar | Flagship |

## Browse by concept

Lookup table — find every sample that demonstrates a concept. Samples appear in the same category order as above (Starter -> Input widgets -> Interactive states -> Runtime-rendered -> Full-feature) so navigation stays predictable.

### Bindings

- **OneWayBinding** — gameplay state pushes into a UI Variable; UI follows. Default for HUDs. → [label_clock](./label_clock/), [counter_button](./counter_button/), [toggle_pause](./toggle_pause/), [settings_full](./settings_full/), [health_bar](./health_bar/), [boss_hp_bar](./boss_hp_bar/), [death_respawn_modal](./death_respawn_modal/), [chat_panel](./chat_panel/), [dialog_system](./dialog_system/), [inventory_grid_full](./inventory_grid_full/), [loadout_selector](./loadout_selector/), [quest_journal](./quest_journal/), [survival_hud_aaa](./survival_hud_aaa/)
- **TwoWayBinding** — user input on a widget round-trips back into a Variable. Required for `Toggle`, `Slider`, `TextEntry` commits. → [toggle_pause](./toggle_pause/), [settings_full](./settings_full/), [chat_panel](./chat_panel/)
- **ManualBinding** — widget value sits stale until controller calls `Apply.All()`. Canonical for "type then submit" inputs. → [chat_panel](./chat_panel/), [settings_full](./settings_full/), [health_bar](./health_bar/), [label_clock](./label_clock/), [dialog_system](./dialog_system/), [inventory_grid_full](./inventory_grid_full/), [loadout_selector](./loadout_selector/)
- **OnChangeBinding** — UpdateTrigger that fires only when the value changes (cheaper than per-frame). → [counter_button](./counter_button/), [toggle_pause](./toggle_pause/), [settings_full](./settings_full/), [boss_hp_bar](./boss_hp_bar/), [death_respawn_modal](./death_respawn_modal/), [chat_panel](./chat_panel/), [inventory_grid_full](./inventory_grid_full/), [label_clock](./label_clock/), [loadout_selector](./loadout_selector/)

### Events

- **Events_Code** — `OnClick -> Code` delegates the controller assigns **before** `Hud.Show()` so `SyncFieldsTo` carries them to the renderer. → [counter_button](./counter_button/), [chat_panel](./chat_panel/), [death_respawn_modal](./death_respawn_modal/), [dialog_system](./dialog_system/), [drag_drop_inventory](./drag_drop_inventory/), [inventory_grid_full](./inventory_grid_full/), [loadout_selector](./loadout_selector/), [notification_toast_queue](./notification_toast_queue/), [quest_journal](./quest_journal/), [settings_full](./settings_full/)
- **AppApply** — `Hud.Apply.All()` flushes every Manual-mode widget at once on Save/Send. → [chat_panel](./chat_panel/), [settings_full](./settings_full/)

### ExposeAsVariable patterns

- **ExposeAsVariable** — surface a runtime element on the wrapper so the controller can mutate it without find-by-class. → [chat_panel](./chat_panel/), [dialog_system](./dialog_system/), [drag_drop_inventory](./drag_drop_inventory/), [inventory_grid_full](./inventory_grid_full/), [notification_toast_queue](./notification_toast_queue/), [quest_journal](./quest_journal/), [boss_hp_bar](./boss_hp_bar/)
- **ChildContainer** — flag a Panel as the target for runtime `AddChild` calls instead of writing manual `@ref` walks. → [inventory_grid_full](./inventory_grid_full/), [notification_toast_queue](./notification_toast_queue/)

### Interactive states

- **HoverStyle** — visual state authored in the `.sui`; no `Style.Dirty()` from C#. → [counter_button](./counter_button/), [chat_panel](./chat_panel/), [loadout_selector](./loadout_selector/), [notification_toast_queue](./notification_toast_queue/), [quest_journal](./quest_journal/), [settings_full](./settings_full/)
- **PressedStyle** — pressed-state visual delta, same model as Hover. → [counter_button](./counter_button/), [chat_panel](./chat_panel/), [loadout_selector](./loadout_selector/), [notification_toast_queue](./notification_toast_queue/), [quest_journal](./quest_journal/), [settings_full](./settings_full/)
- **IsHighlighted** — bool-driven "selected" state with `HighlightedStyle` overrides. → [quest_journal](./quest_journal/)

### Layout

- **Anchoring** — anchor + pivot patterns for every screen position. → all 16 samples ship anchored layouts; canonical example is [empty_canvas](./empty_canvas/).
- **FlexLayout** — `FlexDirection` reflows children automatically; required for stacking toasts and dialog choice buttons. → [dialog_system](./dialog_system/), [drag_drop_inventory](./drag_drop_inventory/), [notification_toast_queue](./notification_toast_queue/)
- **GridLayout** — `Display: Grid` with column count + cell size, used for slot grids. → [inventory_grid_full](./inventory_grid_full/)

### Visual effects

- **CssTransitions** — author transitions in the `.sui`; controller flips a class and CSS does the rest. → [loadout_selector](./loadout_selector/), [notification_toast_queue](./notification_toast_queue/)
- **UserScss** — escape hatch for hand-rolled `.user.scss` that survives Force Regen. → [dialog_system](./dialog_system/), [drag_drop_inventory](./drag_drop_inventory/)
- **ProgressBar** — bar fill bound to a normalized 0..1 float. → [health_bar](./health_bar/), [boss_hp_bar](./boss_hp_bar/), [loadout_selector](./loadout_selector/), [quest_journal](./quest_journal/), [settings_full](./settings_full/), [survival_hud_aaa](./survival_hud_aaa/)

### Widgets

- **Button** — Code-mode `OnClick` is the canonical interaction. → [counter_button](./counter_button/), [chat_panel](./chat_panel/), [death_respawn_modal](./death_respawn_modal/), [loadout_selector](./loadout_selector/), [notification_toast_queue](./notification_toast_queue/), [quest_journal](./quest_journal/), [settings_full](./settings_full/)
- **TextEntry_Manual** — value sits stale until `Apply.All()`; the right default for chat / form fields. → [chat_panel](./chat_panel/), [settings_full](./settings_full/)
- **Slider** — drag-driven float Variable. → [settings_full](./settings_full/)
- **DropDown** — select-one widget with OnChange auto-commit. → [settings_full](./settings_full/)
- **Toggle** — `Checked` round-trips through a `bool` Variable. → [toggle_pause](./toggle_pause/), [settings_full](./settings_full/)

### Runtime patterns

- **RuntimeAddChild** — populate a panel from a controller list every render pass. → [chat_panel](./chat_panel/), [dialog_system](./dialog_system/), [drag_drop_inventory](./drag_drop_inventory/), [notification_toast_queue](./notification_toast_queue/)
- **Variables** — wrapper-level fields that all bindings ultimately read or write. → every sample except [empty_canvas](./empty_canvas/) and [drag_drop_inventory](./drag_drop_inventory/).

## Pattern recipes

Practical "I want to..." -> "Look at..." mapping. Each row points to the smallest sample that demonstrates the pattern.

| I want to... | Look at... |
|---|---|
| Drive a piece of text from gameplay every frame | [label_clock](./label_clock/) |
| Increment a number when a button is clicked | [counter_button](./counter_button/) |
| Make a checkbox flip a `bool` in C# | [toggle_pause](./toggle_pause/) |
| Bind a `ProgressBar` to a normalized health value | [health_bar](./health_bar/) |
| Build a full settings screen with Apply / Cancel / Reset | [settings_full](./settings_full/) |
| Handle text input with explicit Send-button commit | [chat_panel](./chat_panel/) |
| Spawn UI elements dynamically at runtime | [chat_panel](./chat_panel/), [notification_toast_queue](./notification_toast_queue/) |
| Build a drag-and-drop with cursor-following ghost | [drag_drop_inventory](./drag_drop_inventory/) |
| Show a stacking notification / toast queue | [notification_toast_queue](./notification_toast_queue/) |
| Build a multi-tab UI with selected-state highlight | [quest_journal](./quest_journal/) |
| Show a full-screen modal with a countdown timer | [death_respawn_modal](./death_respawn_modal/) |
| Drive a dramatic single bar with phase markers | [boss_hp_bar](./boss_hp_bar/) |
| Build a class / loadout picker with detail pane | [loadout_selector](./loadout_selector/) |
| Wire a 6x4 inventory grid with per-slot click events | [inventory_grid_full](./inventory_grid_full/) |
| Implement a branching NPC dialog with typewriter | [dialog_system](./dialog_system/) |
| Ship a survival HUD touching every Variable type | [survival_hud_aaa](./survival_hud_aaa/) |
| Prove the wrapper-mount plumbing works end-to-end | [empty_canvas](./empty_canvas/) |

## How these are authored

Every showcase folder follows the same template — title + hero paragraph + Behavior walkthrough + What you'll see + How to use + Variables table + Bindings table + Events table (where applicable). The strongest references for the format are:

- [chat_panel/README.md](./chat_panel/README.md) — flagship for runtime-driven samples (Manual TextEntry + AddChild)
- [inventory_grid_full/README.md](./inventory_grid_full/README.md) — flagship for `ExposeAsVariable` + runtime wiring after mount
- [quest_journal/README.md](./quest_journal/README.md) — flagship for `IsHighlighted` + multi-tab navigation
- [settings_full/README.md](./settings_full/README.md) — flagship for every input widget + `Apply.All()` pattern

When in doubt, copy the structure of the closest existing README and replace the content — that is exactly how the 16 samples were brought to parity.

## Contributing a new sample

1. **Create the folder.** `samples/showcase/<your_sample>/` — name is lowercase + underscores, matching the wrapper class name (`YourSample`).
2. **Author the `.sui`.** Open SUI Designer, build the layout, Save. Default to Absolute layout with explicit X/Y unless Flex / Grid is the lesson being taught.
3. **Write the controller.** `<YourSample>Controller.cs` — assign all Code-mode delegates **before** `Hud.Show()`, expose any `[Property]` knobs that gameplay would tweak.
4. **Write the README** following the template above. Variables / Bindings / Events tables are non-negotiable.
5. **Open a PR** adding your sample to the matching category table in this index file and to the concept lookup lists below it. Keep alphabetical order inside each category section.
