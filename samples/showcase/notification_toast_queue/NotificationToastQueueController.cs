using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>notification_toast_queue</c> showcase.
/// Three trigger buttons in the bottom-left spawn coloured toasts that
/// stack in the top-right corner — Apex/Discord/COD style. Each toast
/// slides in from the right, sits visible for ~4 seconds, then slides
/// back out. Clicking a toast dismisses it immediately.
///
/// <para>Stress-tests:</para>
/// <list type="number">
/// <item>Simultaneous intro and outro CSS transitions — a new toast can
/// slide in at the same instant another fades out, both animating
/// independently without conflicting.</item>
/// <item>Per-toast timer state — each tracks its own spawn/dismiss-at
/// timestamps. Auto-dismiss fires per-toast based on Time.Now.</item>
/// <item>Stacking math via flex column gap on the container — no manual
/// position offsets needed; new AddChild lands at the bottom of the
/// flex stack and the container's flex layout handles the rest.</item>
/// <item>Cascading lifecycle — spawn → wait → dismiss-class-add → wait
/// for outro to finish → DeleteChild. Three pending lists pipeline the
/// state transitions outside the engine's event dispatch (mutating
/// children during onclick throws Index-out-of-range).</item>
/// </list>
/// </summary>
public sealed class NotificationToastQueueController : Component
{
	[Property] public NotificationToastQueue Hud { get; set; } = new();

	/// <summary>Seconds a toast stays on screen before auto-dismiss starts.</summary>
	[Property] public float ToastDuration { get; set; } = 4f;

	/// <summary>Seconds the outro animation takes (must match the CSS).</summary>
	[Property] public float OutroDuration { get; set; } = 0.3f;

	// ────────────────────────────────────────────────────────────────────
	// Active toast tracking
	// ────────────────────────────────────────────────────────────────────

	private enum ToastKind { Info, Success, Warning }

	private sealed class ActiveToast
	{
		public Panel Panel;
		public float DismissAt;
		public float RemoveAt;
		public bool DismissTriggered;
		public bool ShowTriggered;
	}

	private readonly List<ActiveToast> _active = new();

	private readonly Queue<(ToastKind kind, string title, string body)> _pendingSpawns = new();

	private static readonly (string title, string body)[] InfoLines =
	{
		( "New friend request", "Aurora wants to play with you." ),
		( "Quest updated", "Speak with Eldrin at the gate to continue." ),
		( "Lobby open", "Server #4 has 12 / 16 slots free." ),
		( "Patch available", "Update 1.4.2 is ready to install." ),
	};

	private static readonly (string title, string body)[] SuccessLines =
	{
		( "Achievement unlocked", "First Blood — Win a duel under 30 seconds." ),
		( "Item crafted", "Iron Sword +1 added to your inventory." ),
		( "Trade complete", "Sold 12 Gold Ore for 480 credits." ),
		( "Level up!", "You reached level 14. New perks available." ),
	};

	private static readonly (string title, string body)[] WarningLines =
	{
		( "Low ammunition", "Plasma Rifle has 3 rounds remaining." ),
		( "Connection unstable", "Ping spiked to 240 ms." ),
		( "Inventory full", "Drop something to pick up the loot." ),
		( "Server restart soon", "Reconnect window opens in 5 minutes." ),
	};

	private int _infoLineIndex;
	private int _successLineIndex;
	private int _warningLineIndex;

	// ────────────────────────────────────────────────────────────────────
	// Bootstrap
	// ────────────────────────────────────────────────────────────────────

	private bool _viewBootstrapped;

	protected override void OnStart()
	{
		// Wire ALL three Code-mode click handlers BEFORE Show — SyncFieldsTo
		// copies them into the renderer Panel during Show. Assigning after
		// leaves the renderer with null delegates and the clicks no-op.
		Hud.OnInfoClick = OnInfoClick;
		Hud.OnSuccessClick = OnSuccessClick;
		Hud.OnWarningClick = OnWarningClick;

		Hud.Show( GameObject, SuiInputMode.All );
	}

	protected override void OnUpdate()
	{
		// Wait for the container @ref to capture before processing the
		// spawn queue. Until then we just accumulate clicks.
		if ( !_viewBootstrapped )
		{
			if ( Hud.View?.ToastsContainer != null )
				_viewBootstrapped = true;
			else
				return;
		}

		// PASS ORDER MATTERS: promote BEFORE draining new spawns. The CSS
		// transition needs at least one paint of the initial .toast state
		// (opacity:0, translateX off-screen) before .show flips to the
		// visible state. If we drain-then-promote in the same frame, the
		// engine paints once with .toast.show already applied, the
		// transition has no "from" state to animate from, and the toast
		// pops in instantly. Promoting first means toasts spawned this
		// frame paint as .toast (off-screen) at frame end, and next
		// frame's promote pass adds .show — the engine sees both states
		// across frames and the slide-in animates.
		for ( int i = 0; i < _active.Count; i++ )
		{
			var t = _active[i];
			if ( !t.ShowTriggered )
			{
				t.Panel?.AddClass( "show" );
				t.ShowTriggered = true;
			}
		}

		// Drain queued spawns AFTER the promote pass so newly-spawned
		// toasts stay in the off-screen .toast state for one paint cycle.
		while ( _pendingSpawns.Count > 0 )
		{
			var spawn = _pendingSpawns.Dequeue();
			SpawnToast( spawn.kind, spawn.title, spawn.body );
		}

		// Tick each toast for auto-dismiss and outro completion.
		for ( int i = _active.Count - 1; i >= 0; i-- )
		{
			var t = _active[i];
			if ( !t.DismissTriggered && Time.Now >= t.DismissAt )
			{
				t.Panel?.AddClass( "dismiss" );
				t.RemoveAt = Time.Now + OutroDuration;
				t.DismissTriggered = true;
			}
			if ( t.DismissTriggered && Time.Now >= t.RemoveAt )
			{
				t.Panel?.Delete();
				_active.RemoveAt( i );
			}
		}
	}

	protected override void OnDestroy()
	{
		Hud?.Remove();
	}

	// ────────────────────────────────────────────────────────────────────
	// Button handlers — assigned in OnStart, fired by the Code-mode events.
	// ────────────────────────────────────────────────────────────────────

	private void OnInfoClick()
	{
		var (title, body) = InfoLines[_infoLineIndex % InfoLines.Length];
		_infoLineIndex++;
		_pendingSpawns.Enqueue( (ToastKind.Info, title, body) );
	}

	private void OnSuccessClick()
	{
		var (title, body) = SuccessLines[_successLineIndex % SuccessLines.Length];
		_successLineIndex++;
		_pendingSpawns.Enqueue( (ToastKind.Success, title, body) );
	}

	private void OnWarningClick()
	{
		var (title, body) = WarningLines[_warningLineIndex % WarningLines.Length];
		_warningLineIndex++;
		_pendingSpawns.Enqueue( (ToastKind.Warning, title, body) );
	}

	// ────────────────────────────────────────────────────────────────────
	// Toast lifecycle
	// ────────────────────────────────────────────────────────────────────

	private void SpawnToast( ToastKind kind, string title, string body )
	{
		var container = Hud.View?.ToastsContainer;
		if ( container == null ) return;

		var toast = container.AddChild<Panel>();
		toast.AddClass( "toast" );
		toast.AddClass( "toast-" + kind.ToString().ToLower() );

		// Icon — coloured circle with a single-letter glyph. Background
		// colour is driven by the .toast-{kind} CSS class in User.scss.
		var icon = toast.AddChild<Panel>();
		icon.AddClass( "toast-icon" );

		var iconLabel = icon.AddChild<Label>();
		iconLabel.AddClass( "toast-icon-letter" );
		iconLabel.Text = GetIconLetter( kind );

		// Text column — title + body stacked vertically.
		var textCol = toast.AddChild<Panel>();
		textCol.AddClass( "toast-text" );

		var titleLabel = textCol.AddChild<Label>();
		titleLabel.AddClass( "toast-title" );
		titleLabel.Text = title;

		var bodyLabel = textCol.AddChild<Label>();
		bodyLabel.AddClass( "toast-body-text" );
		bodyLabel.Text = body;

		var active = new ActiveToast
		{
			Panel = toast,
			DismissAt = Time.Now + ToastDuration,
			RemoveAt = float.MaxValue,
			DismissTriggered = false,
			ShowTriggered = false,
		};
		_active.Add( active );

		// Click-to-dismiss — just adjusts the timer; the actual class flip
		// + delete happens in OnUpdate next frame.
		toast.AddEventListener( "onclick", () => DismissImmediate( active ) );
	}

	private void DismissImmediate( ActiveToast toast )
	{
		if ( toast.DismissTriggered ) return;
		toast.DismissAt = Time.Now;
	}

	private static string GetIconLetter( ToastKind kind ) => kind switch
	{
		ToastKind.Info    => "i",
		ToastKind.Success => "✓",
		ToastKind.Warning => "!",
		_                 => "?",
	};
}
