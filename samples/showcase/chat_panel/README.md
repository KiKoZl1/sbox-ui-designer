# Chat Panel

A compact in-game **chat panel** showcase for the s&box UI Designer (`.sui`). One dark card in the bottom-left corner with a scrollable message history, a typed input, and a green Send button — the canonical "twitch-style overlay chat" surface, in pure `.sui` plus a small controller.

If the `settings_full` sample is "every input widget the Designer ships," and `inventory_grid_full` is "a runtime-populated grid using `ExposeAsVariable`," then `chat_panel` is the **smallest** runtime-driven sample: two `ExposeAsVariable` elements (one Panel, one TextEntry), one Manual TextEntry binding, one Code-mode click event, and a runtime list of `Label` children rebuilt every send. It demonstrates:

- **TextEntry** in `Manual` update mode — the draft sits on the widget until the controller calls `Apply.All()` on Send, which is the canonical "type then submit" pattern
- **A Button with `OnClick → Code`** wired to a delegate the controller assigns *before* `Hud.Show()` (the load-bearing gotcha; see Events table below)
- **`ExposeAsVariable=true` on two elements** so the controller can reach the runtime panel via `Hud.View?.MessageList` and the input via `Hud.View?.ChatInput` without find-by-class
- **`OneWay` Text binding** on the header counter, driven entirely by the controller
- **Hover / Pressed interactive styles** on the Send button (1.03× hover, 0.97× pressed, green→darker-green tint)
- **A built-in input action (`Reload`, default key `R`) as an Enter fallback** so the sample works out of the box on any project without input-config edits

## Behavior

End-to-end walkthrough of every interaction the sample wires up.

1. **Mount the panel.** A 480×320 dark card with a 1-pixel border anchors to the bottom-left, 24px in and 20px up from the screen edge. A single seeded welcome message — `[00:00] system: Welcome - type a message and hit Send (or press R).` — is rendered in green. The header reads `Chat` on the left and `1 message` on the right.
2. **Type into the input.** The TextEntry is `Manual` mode, so `Hud.ChatInputText` does **not** update as you type — it stays empty until the controller calls `Hud.Apply.All()`. The on-screen widget shows the draft normally; only the bound Variable lags.
3. **Click Send (or press R).** The controller's `OnSendClick` runs `Hud.Apply.All()` (flushing the draft into `Hud.ChatInputText`), trims the string, exits if empty, then builds a `ChatMessage` and appends it to `_messages`. `RenderMessages()` wipes the `MessageList` panel and re-creates one `Label` child per message. The header counter updates (`2 messages`), the panel scrolls to the bottom (`TryScrollToBottom()`), the input is cleared (`Hud.ChatInputText = ""`), and the keyboard focus is restored to the TextEntry (`Hud.View?.ChatInput?.Focus()`) so you can keep typing.
4. **`/me <action>`** is parsed as an emote — the message is tinted amber (`#fbbf24`) and the leading `/me ` is stripped. Try `/me waves`.
5. **`/system <text>`** is parsed as a system message — author is overridden to `system`, the message is tinted green (`#4ade80`). Try `/system server restarting in 5 minutes`.
6. **Send 65 messages.** `MaxMessages` (default 64) caps the in-memory list; older entries are dropped from the front. The runtime panel never grows unbounded.

The Manual TextEntry trigger is intentional: a chat input has obvious "submit" semantics, and `OnChange` would commit the draft on every keystroke which is wasteful and would clash with the Send button's flush. `Apply.All()` runs once per send, never per frame.

## What you'll see

A 480×320 dark card with rounded corners and a thin grey border anchors to the bottom-left of the screen. Top-left of the card reads `Chat` in bold grey; top-right reads `0 messages` (then `1 message`, `2 messages`, …) as you interact. Below the header is a 240px-tall message list with `Overflow=Hidden`, which clips the oldest messages out of view once the list is taller than the card. Beneath that is a 28px-tall darker bar (`#1f2937`) with the text input layered on top — placeholder `Type a message...` until you click and type. A green Send button (with hover scale + pressed darken) sits to the right.

The seed message — green, system author — fills the panel immediately on mount so it never looks blank. Every subsequent send appends one row below with a `[MM:SS]` timestamp, the local author (`you`), and the typed text.

## How to use

1. Open `chat_panel.sui` once in the **SUI Designer** window (`Window → Sbox UI Designer`) and hit **Compile**. This writes `ChatPanel.razor` + `ChatPanel.scss` + `ChatPanel.cs` (the wrapper) into `Code/Samples/ChatPanel/` of your project, under namespace `Sandbox.Samples`.
2. Drop `ChatPanelController.cs` into the same folder (or anywhere under `Code/`).
3. In any scene, add a new GameObject and attach the **ChatPanelController** Component to it.
4. Press **Play**. The card appears in the bottom-left. Click the input, type something, click **Send** (or press `R`), and watch the message render. Try `/me waves` and `/system maintenance window 22:00 UTC` for the two command flavours.

The Component's `Hud` Property surfaces both Variables (`ChatInputText`, `MessageCountText`) under a foldout in the Inspector — handy when debugging whether `Apply.All()` actually flushed. Three additional `[Property]` knobs (`SystemAuthor`, `LocalAuthor`, `MaxMessages`) let you tweak flavour without touching code.

### Hotkeys

| Key | Action input | Effect |
|---|---|---|
| `R` (default binding for `Reload`) | `Input.Pressed("Reload")` | Sends the current input draft — Enter-fallback so you can hammer messages without reaching for the mouse. Gated by `Hud.IsMounted` so it's inert before mount / after destroy. |

`Reload` is a built-in action that ships with every fresh s&box project, which is why the sample picks it — no `actions.cfg` edit required. If you'd rather have real `Enter` behaviour, see "Extending it" below.

## Variables

| Name | Type | Default | Role |
|---|---|---|---|
| `ChatInputText` | `string` | `""` | The current draft in the input field. **TwoWay** + **Manual** — the wrapper field only catches up to the live widget when the controller calls `Hud.Apply.All()` (which `OnSendClick` does on every send). |
| `MessageCountText` | `string` | `"0 messages"` | Header counter ("12 messages"). **OneWay**-bound to the `MsgCountText` element. The controller writes this from `_messages.Count` at the end of `RenderMessages()`. |

Both Variables are `IsPublic = true` so they surface in the wrapper's public API.

## Bindings

| Element | Property | Variable | Mode | Update Trigger |
|---|---|---|---|---|
| `ChatInput` (TextEntry) | `Value` | `ChatInputText` | TwoWay | **Manual** |
| `MsgCountText` (Text) | `Text` | `MessageCountText` | OneWay | OnChange |

> **Why `Manual` on the input field?** A chat send is the canonical "draft → submit" pattern. The wrapper exposes a per-element `Apply.ChatInputValue()` method plus a catch-all `Apply.All()` — see the [Manual commit with Apply](https://kikozl1.github.io/sbox-ui-designer/workflows/manual-commit-with-apply.html) workflow doc. Using `OnChange` here would commit on every keystroke and conflict with the Send-button flush.

## Events

| Element | Event | Mode | Handler |
|---|---|---|---|
| `SendButton` (Button) | `OnClick` | Code | `OnSendClick` |

> **Note on Code-mode wiring.** For each Code-mode event the generator emits
> `[Property, Group("Events")] public Action OnXxxxClick { get; set; }`
> on the `ChatPanel` wrapper class — **not** as a method named-resolved on
> the controller. The controller must explicitly assign every delegate
> *before* `Hud.Show()`:
>
> ```csharp
> Hud.OnSendClick = OnSendClick;
> Hud.Show( GameObject, SuiInputMode.All );   // mount AFTER all wiring
> ```
>
> `Show()` triggers `SyncFieldsTo`, which copies the wrapper's delegates into
> the renderer Panel. Assigning *after* `Show()` leaves the renderer with
> `null` and the button hover-animates but the click silently no-ops.
> See the full pattern in [Events & Actions → Code mode](https://kikozl1.github.io/sbox-ui-designer/concepts/events-and-actions.html#code-mode).

## ExposeAsVariable — runtime element access

Two elements are flagged `ExposeAsVariable = true` in the `.sui`:

| Element | Type | Why exposed |
|---|---|---|
| `MessageList` | `Panel` | Controller calls `Hud.View?.MessageList.DeleteChildren(true)` + `AddChild<Panel>()` per render, plus `TryScrollToBottom()` to keep the newest message in view. |
| `ChatInput` | `TextEntry` | Controller calls `Hud.View?.ChatInput?.Focus()` after each send so the keyboard caret returns to the input without the player clicking again. |

The flag tells the wrapper generator to emit a `[Sui] public T Name { get; set; }` field on the **View** class — that's the rendered Panel sibling of the Variables. The controller reaches them through `Hud.View?` (null-safe because the View is created on first `Show()` and torn down on `Remove()`). Without the flag the only escape hatch is `find-by-class` on the rendered tree, which is brittle the moment you rename a CSS class.

## Controller architecture

`ChatPanelController` keeps the runtime message store in a `List<ChatMessage>` (a tiny private struct with `Author / Text / SentAt / Kind`). The flow:

- `OnStart` wires `Hud.OnSendClick`, seeds both Variables, calls `Hud.Show(GameObject, SuiInputMode.All)` — `All` mode is required so the TextEntry can receive keyboard input — then seeds a friendly welcome line so the panel isn't blank on frame zero.
- `OnUpdate` checks `Input.Pressed("Reload")` once per frame and triggers an Enter-fallback send. Guarded by `Hud.IsMounted` so it's inert before mount / after destroy.
- `OnSendClick` runs `Hud.Apply.All()` to flush the Manual TextEntry, trims the draft, exits if empty, parses `/me` and `/system` command prefixes, appends a `ChatMessage`, trims the list to `MaxMessages`, calls `RenderMessages()`, clears the input, and refocuses the TextEntry.
- `RenderMessages` wipes `MessageList`, rebuilds one `Sandbox.UI.Label` per message with a `[MM:SS] author: text` prefix, applies per-kind `Style.FontColor` (green for system, amber for emotes, grey for normal), updates `MessageCountText` (with singular/plural handling), and calls `TryScrollToBottom()` so the newest row is visible.
- `OnDestroy` calls `Hud?.Remove()` for clean teardown — without this the panel survives the GameObject and leaks into the next scene.

The use of `SuiInputMode.All` is intentional — the TextEntry needs the keyboard, the Send button needs the cursor. If you want gameplay input to coexist (so the player can still move while typing), drop to `SuiInputMode.MouseOnly` and accept that the TextEntry's text input will be capture-mode only.

## Extending it

- **Send on real Enter instead of `R`.** Add an action named `chat_send` mapped to `KEY_RETURN` in `actions.cfg`, then swap `Input.Pressed("Reload")` for `Input.Pressed("chat_send")` in `OnUpdate`. The whole sample stays 1-Component.
- **Toggle the panel with a hotkey.** Add a `bool _chatOpen` field and gate `Hud.Show()` / `Hud.Remove()` on `Input.Pressed("Score")` (default `Tab`). When closed, the panel is fully unmounted — no rendering, no input capture, no per-frame `OnUpdate` work beyond the toggle check.
- **Network the messages.** Promote `_messages` to a `[Sync] List<ChatMessage>` (and add a `[Sync]` attribute to the struct), then call a `[Rpc.Broadcast]` method from `OnSendClick` instead of mutating the list locally. Every client renders from the same authoritative list. The `.sui` doesn't change.
- **Add more commands.** The `/me` and `/system` parsers in `OnSendClick` are deliberately tiny — add `/whisper <name> <text>`, `/clear`, `/help`, or a slash-command registry the controller iterates over. Each new command lands in `_messages` like any other.
- **Persist history across sessions.** Serialise `_messages` to `FileSystem.Data` in `OnDestroy` (`FileSystem.Data.WriteJson("chat.json", _messages)`) and reload it in `OnStart` before the first `RenderMessages()` call. Use `MaxMessages` as the cap so the file stays small.
- **Auto-fade old messages.** Cache the `Label` per row in a `Dictionary<int, Label>` keyed by send index, then in `OnUpdate` walk the dictionary and lower `label.Style.Opacity` based on `(Time.Now - msg.SentAt)`. Twitch-style chat scrollback fade in ~30 lines.
- **Replace the runtime Labels with a generated row template.** Instead of `AddChild<Label>()` in `RenderMessages`, design a second `chat_row.sui` with author + body + timestamp slots, then `Hud.View?.MessageList.AddChild(new ChatRow { Author = msg.Author, Body = msg.Text })`. Gets you styled rows that the Designer can preview live.
- **Add a typing indicator.** A second `Text` element bound `OneWay` to a `bool IsRemoteTyping` Variable — controller flips it true on RPC-receive of a "typing" event, false 2s after the last keystroke. Same flow as `MessageCountText`.
