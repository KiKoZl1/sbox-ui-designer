# Settings (Full)

The end-to-end **settings screen** showcase for the s&box UI Designer (`.sui`). One card on top of a dimming scrim, every input widget the Designer ships, and the classic Apply / Cancel / Reset triad wired through the `Apply.All()` save pattern.

If the `counter_button` sample is "the smallest thing that proves the binding loop works," this is "the smallest thing that proves a real settings screen is possible without dropping out to Razor." It exercises:

- **TextEntry** in `Manual` update mode (the name field commits only on Apply)
- **Slider** in `OnChange` (the value Variable and live "73%" label both update as you drag)
- **Toggle** in `OnChange` (flips immediately)
- **DropDown** in `OnChange` (quality preset, options driven from the `.sui`)
- **Three Buttons** with `OnClick → Code` handlers (`OnApplyClick`, `OnCancelClick`, `OnResetClick`)
- **Computed status text** ("● Unsaved changes") driven OneWay from a string Variable the controller maintains
- **Hover / Pressed interactive styles** on every button (the Apply button scales 1.03× on hover, 0.97× when pressed)
- **A full dark UI card layout** in pure Absolute mode — nothing depends on Flex, so every element stays draggable in the Designer canvas

## What you'll see

A centred 720×680 dark card called "Settings" appears over a dimming scrim that covers the whole screen. Three tabs run across the top — **Audio** is active, **Graphics** and **Controls** are visually present but disabled (the V1 sample renders only the Audio panel; extending it with real tab switching is one of the suggestions below).

The Audio panel contains, top-to-bottom:
- **Player Name** — a text field, prefilled with `Player`, max 24 chars.
- **Master Volume** — a slider from 0 to 100 with a `50%` label to the right that tracks live as you drag.
- **Music Enabled** — a checkbox-style toggle, on by default.
- **Quality** — a dropdown with `Low / Medium / High / Ultra`, defaulting to `High`.
- **Status line** — empty until you start editing; turns into yellow `● Unsaved changes` the moment any value diverges from the last-saved snapshot.
- **Reset / Cancel / Apply** — three buttons. Reset wipes everything back to defaults, Cancel reverts to the last saved snapshot, Apply commits the in-flight edits (including the Manual-trigger Name field).

## How to use

1. Open `settings_full.sui` once in the **SUI Designer** window (`Window → Sbox UI Designer`) and hit **Compile**. This writes `SettingsFull.razor` + `SettingsFull.scss` + `SettingsFull.cs` (the wrapper) into `Code/Samples/SettingsFull/` of your project, under namespace `Sandbox.Samples`.
2. Drop `SettingsFullController.cs` into the same folder (or anywhere under `Code/`).
3. In any scene, add a new GameObject and attach the **SettingsFullController** Component to it.
4. Press **Play**. The card appears centred over the scene. Type into the name field, drag the slider, click the toggle and dropdown, watch the status line flip to "● Unsaved changes," and try the three action buttons.

The Component's `Hud` Property surfaces every Variable (`PlayerName`, `Volume`, `MusicEnabled`, `GraphicsPreset`, `VolumeLabel`, `StatusText`) under a foldout in the Inspector — handy when debugging which way values are flowing.

## Variables

| Name | Type | Default | Role |
|---|---|---|---|
| `PlayerName` | `string` | `"Player"` | Display name. **TwoWay** + `Manual` — only commits to the wrapper when the controller calls `Apply.All()` (on Apply click and once per frame for live dirty detection). |
| `Volume` | `float` | `50` | Master volume 0–100. **TwoWay** + `OnChange` so the live `VolumeLabel` updates as you drag. |
| `VolumeLabel` | `string` | `"50%"` | Formatted preview ("73%") bound **OneWay** to the value text. The controller writes this every frame from `(int)Hud.Volume`. |
| `MusicEnabled` | `bool` | `true` | Music on/off. **TwoWay** + `OnChange` — flips immediately when the toggle is clicked. |
| `GraphicsPreset` | `int` | `2` | Quality preset 0–3 (`Low/Medium/High/Ultra`). **TwoWay** + `OnChange` on `DropDown.Value`. |
| `StatusText` | `string` | `""` | "● Unsaved changes" when dirty, empty when clean. **OneWay** — driven entirely by the controller. |

All Variables are `IsPublic = true` so they show up in the wrapper's public API.

## Bindings

| Element | Property | Variable | Mode | Update Trigger |
|---|---|---|---|---|
| `NameEntry` (TextEntry) | `Value` | `PlayerName` | TwoWay | **Manual** |
| `VolumeSlider` (Slider) | `Value` | `Volume` | TwoWay | OnChange |
| `VolumeValue` (Text) | `Text` | `VolumeLabel` | OneWay | OnChange |
| `MusicToggle` (Toggle) | `Checked` | `MusicEnabled` | TwoWay | OnChange |
| `QualityDropdown` (DropDown) | `Value` | `GraphicsPreset` | TwoWay | OnChange |
| `StatusText` (Text) | `Text` | `StatusText` | OneWay | OnChange |

> **Why `Manual` on the name field?** The canonical "type, then click Save" pattern. The wrapper exposes a per-element `Apply.NameEntryValue()` method plus a catch-all `Apply.All()` — see the [Manual commit with Apply](https://kikozl1.github.io/sbox-ui-designer/workflows/manual-commit-with-apply.html) workflow doc. The other widgets use `OnChange` because there's no "draft" semantic — flipping a toggle or picking a dropdown row is the commit.

## Events

| Element | Event | Mode | Handler |
|---|---|---|---|
| `ResetButton` (Button) | `OnClick` | Code | `OnResetClick` |
| `CancelButton` (Button) | `OnClick` | Code | `OnCancelClick` |
| `ApplyButton` (Button) | `OnClick` | Code | `OnApplyClick` |

> **Note on Code-mode wiring.** For each Code-mode event the generator emits
> `[Property, Group("Events")] public Action OnXxxxClick { get; set; }`
> on the `SettingsFull` wrapper class — **not** as a method named-resolved on
> the controller. The controller must explicitly assign every delegate
> *before* `Hud.Show()`:
>
> ```csharp
> Hud.OnResetClick  = OnResetClick;
> Hud.OnCancelClick = OnCancelClick;
> Hud.OnApplyClick  = OnApplyClick;
> Hud.Show(GameObject, SuiInputMode.All);   // mount AFTER all wiring
> ```
>
> `Show()` triggers `SyncFieldsTo`, which copies the wrapper's delegates into
> the renderer Panel. Assigning *after* `Show()` leaves the renderer with
> `null` and the buttons hover-animate but the clicks silently no-op.
> See the full pattern in [Events & Actions → Code mode](https://kikozl1.github.io/sbox-ui-designer/concepts/events-and-actions.html#code-mode).

## Controller architecture

The `SettingsFullController` keeps a **saved snapshot** of every value (the last
state at which the user pressed **Apply**) alongside the wrapper's "live" state:

- `OnStart` wires the three click delegates, seeds the Variables, then calls
  `Hud.Show( GameObject, SuiInputMode.All )` — `All` mode is required so the
  TextEntry can receive keyboard input.
- `OnUpdate` does three things every frame:
  1. Writes `VolumeLabel = $"{(int)Hud.Volume}%"` so the percentage text stays in sync.
  2. Calls `Hud.Apply.All()` to flush the Manual TextEntry binding into `Hud.PlayerName` (cheap — it's a string read).
  3. Compares each wrapper field against the saved snapshot and writes `Hud.StatusText` to either `"● Unsaved changes"` or `""`.
- `OnApplyClick` calls `Apply.All()` once more (defensive) and then copies the live values into the saved snapshot. The dirty check immediately collapses to clean.
- `OnCancelClick` writes the saved snapshot back into the wrapper fields. Because every binding is TwoWay, the widgets visually revert.
- `OnResetClick` overwrites both saved AND live values with the hard-coded defaults (`Player / 50 / true / 2`).

The use of `SuiInputMode.All` (cursor + keyboard focus) is intentional — the
TextEntry needs the keyboard, and the rest of the widgets need the mouse. If
you embed this card inside a larger HUD that also runs gameplay, swap to
`MouseOnly` and treat the card as a modal you toggle on/off from a hotkey
(see "Extending it" below).

## Extending it

- **Wire the Graphics / Controls tabs** by giving each tab an `OnClick` Code handler that flips a `int CurrentTab` Variable, then add `[CSSClass]` or visibility bindings on three separate sub-panels (`AudioPanel`, `GraphicsPanel`, `ControlsPanel`) that toggle based on `CurrentTab`. The tabs are already styled — only the panels and the dispatch are missing.
- **Persist the values across sessions** by serialising the saved snapshot to `FileSystem.Data` in `OnDestroy()` (`FileSystem.Data.WriteJson( "settings.json", new { _savedName, _savedVolume, ... } )`) and reloading it in `OnStart()` before the first `Hud.Show()`.
- **Apply the volume to a real bus** by hooking `OnApplyClick` to set `Game.Preferences.MasterVolume = _savedVolume / 100f` (or whatever your audio bus exposes) — the SUI side already gives you the committed value.
- **Open / close the menu from a key** by gating `Hud.Show()` / `Hud.Remove()` on `Input.Pressed( "Score" )` (or any action) — currently the card mounts at scene start and never unmounts.
- **Validate the name field** by adding a converter to the `bind_name_value` binding (e.g. `builtin.Compose` with a trimmer) or post-process in `OnApplyClick` (`_savedName = Hud.PlayerName.Trim()`). The TextEntry's `MaxLength` is already capped at 24 in the `.sui` props.
- **Add a "Defaults differ" badge** by adding a second Variable `bool HasNonDefaults` that the controller flips when any saved value diverges from the defaults — bind a small `Text` element's `Visibility` (universal binding) to it.
- **Replace the disabled placeholder tabs with a real Graphics panel** containing a `Resolution` DropDown, a `Fullscreen` Toggle, and a `Brightness` Slider — every widget you need is already demoed here; copy / paste / re-bind.
