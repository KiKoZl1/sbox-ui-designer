# Notification Toast Queue

A stacking notification system: click any of the three trigger buttons (Info, Success, Warning) and a coloured toast slides into the top-right corner. Each toast sits visible for ~4 seconds, then slides back out. Click any toast to dismiss it immediately. Stack multiple by clicking the buttons in quick succession — newest stacks at the bottom (CSS flex-column with `gap: 10`), each one independently animating in and out.

Stress-tests four pipeline areas at once:

1. **Simultaneous intro and outro CSS transitions.** A new toast can slide in at the same instant another fades out, both animating without conflicting (transitions are per-panel, scoped by class state).
2. **Per-toast timer state.** Each `ActiveToast` tracks its own spawn/dismiss/remove timestamps. Auto-dismiss + outro completion fire per-toast on `Time.Now` polling.
3. **Stacking math via flex column gap.** No manual position offsets — the container is `FlexDirection: Column` with `Gap: 10`, so new children land at the bottom of the stack and existing children reflow upward automatically.
4. **Cascading lifecycle through pending queues.** Clicks enqueue a spawn → next frame the spawn materialises → frame after, the `.show` class flips in (kicks the CSS transition) → 4 s later `.dismiss` flips on → 0.3 s later the panel is `Delete()`d. The whole pipeline runs outside Sandbox.UI's event dispatch loop (mutating children inside `onclick` throws IOOR — same gotcha as the dialog_system and drag_drop samples).

## Behavior

1. **Mount.** Three coloured buttons appear in the bottom-left of the screen (Info blue, Success green, Warning amber). Top-right corner is empty.
2. **Click a trigger.** The button's `OnClick` Code-mode handler runs, picks the next title/body line from its kind-specific array (cycles through 4 sample messages), and enqueues a spawn.
3. **Spawn (next frame).** `OnUpdate` drains the queue. `AddChild<Panel>` inside `ToastsContainer` creates the toast structure: icon circle with a single-letter glyph + title row + body row. Initial CSS state is `opacity: 0; transform: translateX(360px)` (off-screen right).
4. **Slide-in (frame after spawn).** OnUpdate adds the `.show` class to any toast missing it. CSS transition fires: 0.3 s ease over `opacity` and `transform` lands the toast at `translateX(0)` fully opaque.
5. **Sit.** Toast lives for `ToastDuration` (default 4 s) measured from spawn.
6. **Auto-dismiss.** OnUpdate adds the `.dismiss` class when `Time.Now ≥ DismissAt`. CSS flips back to `opacity: 0; transform: translateX(360px)`. Same 0.3 s transition, slides out.
7. **Cleanup.** OnUpdate `Delete()`s the toast Panel when `Time.Now ≥ RemoveAt` (DismissAt + OutroDuration). The list reflows.
8. **Click-to-dismiss.** Clicking a toast sets `DismissAt = Time.Now`. Next frame OnUpdate sees the deadline passed and triggers the outro path — same code as auto-dismiss.

## Toast kinds

| Kind | Icon | Icon BG | Sample lines |
|---|---|---|---|
| Info | `i` | `#3b82f6` blue | "New friend request" / "Quest updated" / "Lobby open" / "Patch available" |
| Success | `✓` | `#10b981` green | "Achievement unlocked" / "Item crafted" / "Trade complete" / "Level up!" |
| Warning | `!` | `#f59e0b` amber | "Low ammunition" / "Connection unstable" / "Inventory full" / "Server restart soon" |

Each kind cycles through its 4 lines independently — spam the same button and you'll see all four messages in sequence.

## How to use

1. Open `notification_toast_queue.sui` in the **SUI Designer** and hit **Compile**.
2. Add `NotificationToastQueuePanel.User.scss` (see **Required `User.scss` rules** below).
3. Drop `NotificationToastQueueController.cs` into `Code/Samples/NotificationToastQueue/`.
4. Attach `NotificationToastQueueController` to any GameObject and hit **Play**.

The `ToastDuration` and `OutroDuration` `[Property]` knobs let you tune timing without touching code. Outro must match the `transition` duration in `User.scss` — both default to 0.3 s.

## Required `User.scss` rules

```scss
NotificationToastQueuePanel {
    .toast {
        opacity: 0;
        transform: translateX(360px);
        flex-direction: row;
        align-items: center;
        gap: 12px;
        padding: 12px 14px;
        background-color: #1f2937;
        border: 1px solid #374151;
        border-radius: 8px;
        cursor: pointer;
        flex-shrink: 0;
        transition: opacity 0.3s, transform 0.3s, background-color 0.12s;
    }
    .toast.show { opacity: 1; transform: translateX(0); }
    .toast.dismiss { opacity: 0; transform: translateX(360px); }
    .toast:hover { background-color: #2a3441; }

    .toast-icon {
        width: 32px; height: 32px; border-radius: 16px;
        flex-shrink: 0; justify-content: center; align-items: center;
    }
    .toast-icon-letter { color: #ffffff; font-size: 18px; font-weight: bold; }
    .toast-text { flex-direction: column; flex-grow: 1; gap: 2px; }
    .toast-title { color: #ffffff; font-size: 14px; font-weight: bold; }
    .toast-body-text { color: #9ca3af; font-size: 12px; }

    .toast.toast-info .toast-icon { background-color: #3b82f6; }
    .toast.toast-success .toast-icon { background-color: #10b981; }
    .toast.toast-warning .toast-icon { background-color: #f59e0b; }
}
```

## Controller architecture

- **`OnStart`** wires the three `OnXxxClick` Action delegates on the wrapper BEFORE `Hud.Show()` (SyncFieldsTo copies them into the rendered Panel during Show — assigning after leaves them null).
- **`OnUpdate`** runs four passes each frame: (1) bootstrap check for `ToastsContainer` capture; (2) drain `_pendingSpawns` queue and `SpawnToast` each; (3) promote any toast without `.show` class — adds the class, triggering the CSS intro transition; (4) auto-dismiss tick — adds `.dismiss` class when `DismissAt` passes, `Delete()`s the panel when `RemoveAt` passes.
- **`OnInfoClick` / `OnSuccessClick` / `OnWarningClick`** just enqueue a tuple `(kind, title, body)` and bump the line cycler.
- **`SpawnToast`** does the `AddChild` work — icon + title + body — and registers an `ActiveToast` record with timestamps. Also wires `onclick` for click-to-dismiss.
- **`DismissImmediate`** sets `DismissAt = Time.Now`, letting the normal OnUpdate tick handle the outro path uniformly.

## Extending it

- **Real Apex-style killfeed.** Replace `OnInfoClick` etc. with `[Rpc.Broadcast] PlayerKilled(string killer, string victim)` and spawn a Warning toast on every kill across the network.
- **Persistent toasts.** A 4th kind (`Quest`) without auto-dismiss — only click-to-dismiss. Set `ToastDuration = float.MaxValue` for the quest variant.
- **Action buttons in toasts.** Inside `SpawnToast`, add a sibling Panel after `textCol` with one or two action buttons (`Accept` / `Decline`). `onclick` on each closes the toast and fires the bound action.
- **Sound on spawn.** `Sound.Play( "ui/toast-info.sound" )` per kind inside `SpawnToast`.
- **Slide direction per kind.** Per-kind starting transform — Info slides from right (current), Success from top, Warning shakes. Add `.toast.toast-success { transform: translateY(-40px) }` etc.
- **Stack cap.** Track `_active.Count` — if more than 5, dismiss the oldest immediately (`_active[0].DismissAt = Time.Now`).
- **Replace with PNG icons.** Swap the letter `Label` inside `.toast-icon` for an `Image` and set `image.SetTexture(...)`.
