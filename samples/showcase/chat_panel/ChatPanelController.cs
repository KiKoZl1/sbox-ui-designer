using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>chat_panel</c> showcase sample.
///
/// <para>Drop this on a GameObject in any scene, hit Play, and a 480x320 dark chat
/// card mounts in the bottom-left corner with a scrollable message list, an input
/// field, and a green Send button. Type a message, click Send (or press Enter
/// via the optional fallback hotkey), and the message gets appended to a runtime-
/// populated message list with timestamp + author. The header counter ("12
/// messages") updates after every send.</para>
///
/// <para>The UI lives entirely in <c>chat_panel.sui</c>. This Component owns:
/// the runtime message store (<see cref="_messages"/>), the OnSendClick handler,
/// the <see cref="RenderMessages"/> routine that rebuilds the runtime panel,
/// /command coloring (system messages are tinted green; /me is italic), and the
/// optional <see cref="Input.Pressed(string)"/> Enter fallback so the user
/// doesn't need to reach for the mouse to send.</para>
///
/// <para><b>Why ExposeAsVariable matters here.</b> The <c>MessageList</c> Panel
/// and <c>ChatInput</c> TextEntry are both flagged <c>ExposeAsVariable=true</c>
/// in the <c>.sui</c>. The generator therefore emits <c>[Sui] public Panel
/// MessageList { get; set; }</c> and the equivalent for <c>ChatInput</c> on the
/// View class, so this controller can reach them via <c>Hud.View?.MessageList</c>
/// to AddChild / DeleteChildren / ScrollToBottom, and call
/// <c>Hud.View?.ChatInput?.Focus()</c> to put the keyboard caret back in the
/// input after a send. Without the flag the only escape hatch would be
/// <c>find-by-class</c> on the rendered panel tree, which is brittle.</para>
///
/// <para><b>Hotkey.</b> While the panel is mounted, pressing the input action
/// <c>Reload</c> (default key: <c>R</c>) triggers an Enter-fallback send so you
/// can hammer messages without leaving the keyboard. This intentionally uses a
/// built-in action so the sample works on a fresh project with no input config.</para>
/// </summary>
public sealed class ChatPanelController : Component
{
	/// <summary>
	/// Generated wrapper instance. Constructed inline so the Inspector surfaces
	/// the Variables (ChatInputText, MessageCountText) under the Hud foldout.
	/// </summary>
	[Property] public ChatPanel Hud { get; set; } = new();

	/// <summary>
	/// Optional system-bot author name used to seed the message list with a
	/// welcome line so the panel isn't blank on first frame. Tweak in the
	/// Inspector for flavour.
	/// </summary>
	[Property] public string SystemAuthor { get; set; } = "system";

	/// <summary>
	/// Local username stamped on every message you send. Defaults to "you" so
	/// the sample runs without dependencies on any account / steam system.
	/// </summary>
	[Property] public string LocalAuthor { get; set; } = "you";

	/// <summary>
	/// Maximum number of messages kept in <see cref="_messages"/>. Older entries
	/// are trimmed off the front so the panel never grows unbounded. Set to a
	/// Discord/Slack-friendly default of 50 so the visible sliding window stays
	/// readable even on long sessions.
	/// </summary>
	[Property] public int MaxMessages { get; set; } = 50;

	// ────────────────────────────────────────────────────────────────────────
	// Runtime state
	// ────────────────────────────────────────────────────────────────────────

	private readonly List<ChatMessage> _messages = new();

	/// <summary>
	/// One row in the chat list. Kept private + simple so the sample stays
	/// self-contained; promote to a record / asset if you want to networked-
	/// sync these in a real game.
	/// </summary>
	private struct ChatMessage
	{
		public string Author;
		public string Text;
		public float SentAt;
		public ChatMessageKind Kind;
	}

	private enum ChatMessageKind
	{
		Normal,
		System,
		Emote,
	}

	protected override void OnStart()
	{
		// 1) Wire the single Code-mode click handler BEFORE Show().
		//    Show() triggers SyncFieldsTo, which copies the wrapper's Action
		//    delegates into the rendered Panel. Assigning AFTER Show() leaves
		//    the renderer with null and the click silently no-ops.
		Hud.OnSendClick = OnSendClick;

		// 2) Seed Variables so the first frame is correct.
		Hud.ChatInputText = "";
		Hud.MessageCountText = "0 messages";

		// 3) Mount as a child of this GameObject. SuiInputMode.All grabs cursor
		//    AND keyboard focus - TextEntry needs the keyboard, the SendButton
		//    needs the cursor.
		Hud.Show( GameObject, SuiInputMode.All );

		// 4) Seed a friendly welcome line so the panel isn't blank.
		_messages.Add( new ChatMessage
		{
			Author = SystemAuthor,
			Text = "Welcome - type a message and hit Enter or Send (R also works).",
			SentAt = Time.Now,
			Kind = ChatMessageKind.System,
		} );
		RenderMessages();

		// 5) Wire Enter-to-submit on the live TextEntry. The .sui Designer's
		//    Events schema doesn't (yet) expose OnSubmit as a Code-mode event
		//    for TextEntry, so we attach via the engine Panel API after Show().
		//    Sandbox.UI fires "onsubmit" when the user presses Enter inside a
		//    focused TextEntry, mirroring real chat clients.
		var entry = Hud.View?.ChatInput;
		if ( entry != null )
		{
			entry.AddEventListener( "onsubmit", () => OnSendClick() );

			// Put the caret in the input on mount so the player can type
			// immediately without clicking. Without this the SuiInputMode.All
			// capture grabs the root and the TextEntry never holds focus on
			// frame zero. Diagnosed in the Bug 3a investigation.
			entry.Focus();
		}
	}

	protected override void OnUpdate()
	{
		// Bonus hotkey: built-in Reload action (default R) also sends the draft
		// so the user can hammer messages even without the TextEntry focused.
		// The native Enter-submit path is wired in OnStart via AddEventListener
		// on the live ChatInput - this Reload binding stays as a secondary
		// hotkey because it's already documented in the README and works even
		// when focus has drifted off the TextEntry. Uses Input.Pressed (edge-
		// triggered) so holding the key doesn't spam-send. Guarded by
		// Hud.IsMounted so the hotkey is inert before OnStart finishes or
		// after OnDestroy removes the panel.
		if ( Hud.IsMounted && Input.Pressed( "Reload" ) )
		{
			OnSendClick();
		}
	}

	protected override void OnDestroy()
	{
		Hud?.Remove();
	}

	// ────────────────────────────────────────────────────────────────────────
	// Click handler - assigned to Hud.OnSendClick in OnStart.
	// ────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Commit the Manual TextEntry binding, build a <see cref="ChatMessage"/>
	/// (with /me + /system command recognition), append it to the list, rebuild
	/// the runtime panel, clear the input field, refocus the TextEntry so the
	/// player can keep typing without reaching for the mouse.
	/// </summary>
	private void OnSendClick()
	{
		// Flush every Manual binding into the wrapper. Without this the
		// TextEntry's draft string is still sitting on the rendered widget and
		// Hud.ChatInputText is stale. Apply.All() is the canonical "save now"
		// call - cheap because it's just a couple of reads.
		Hud.Apply.All();

		var draft = (Hud.ChatInputText ?? "").Trim();
		if ( string.IsNullOrEmpty( draft ) )
			return;

		// /command parsing - kept tiny on purpose. Adding more commands is one
		// of the suggestions in the README ("Extending it").
		var message = new ChatMessage { Author = LocalAuthor, SentAt = Time.Now };

		if ( draft.StartsWith( "/me ", StringComparison.OrdinalIgnoreCase ) )
		{
			message.Kind = ChatMessageKind.Emote;
			message.Text = draft.Substring( 4 );
		}
		else if ( draft.StartsWith( "/system ", StringComparison.OrdinalIgnoreCase ) )
		{
			message.Kind = ChatMessageKind.System;
			message.Author = SystemAuthor;
			message.Text = draft.Substring( 8 );
		}
		else
		{
			message.Kind = ChatMessageKind.Normal;
			message.Text = draft;
		}

		_messages.Add( message );

		// Trim oldest if we've exceeded the cap. Keeps the runtime panel bounded
		// even after a long session.
		while ( _messages.Count > MaxMessages )
		{
			_messages.RemoveAt( 0 );
		}

		RenderMessages();

		// Clear the input by writing to the live TextEntry's .Text directly.
		// Hud.ChatInputText = "" would NOT clear the widget — the codegen for
		// Manual UpdateTrigger does not emit a Value= pre-fill on the TextEntry
		// tag (intentional — see 2026-06-05 fix), so the wrapper's Variable is
		// pull-only via Apply.X. To push state back into the widget, reach the
		// typed exposed @ref directly.
		if ( Hud.View?.ChatInput != null )
		{
			Hud.View.ChatInput.Text = "";
			Hud.ChatInputText = ""; // keep the wrapper variable in sync for binds elsewhere
		}
		Hud.View?.ChatInput?.Focus();
	}

	// ────────────────────────────────────────────────────────────────────────
	// Render loop - rebuilds the runtime panel from _messages.
	// ────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Wipe the MessageList panel and rebuild one Label per message. Updates
	/// MessageCountText so the header counter stays in sync, and scrolls the
	/// list to the bottom so the newest message is always visible.
	/// </summary>
	private void RenderMessages()
	{
		var list = Hud.View?.MessageList;
		if ( list is null )
			return;

		list.DeleteChildren( true );

		foreach ( var msg in _messages )
		{
			var row = list.AddChild<Panel>();
			row.AddClass( "chat-row" );

			// Force each row to span the full list width via Style.Width. The
			// matching `flex-shrink: 0` rule lives in ChatPanelPanel.User.scss
			// so we don't have to depend on a runtime FlexShrink property that
			// may not exist on every Sandbox.UI build. The .sui's
			// FlexDirection=Column on MessageList stacks them vertically; this
			// width keeps the row from collapsing into a content-sized box.
			row.Style.Width = Length.Percent( 100 );
			row.Style.Dirty();

			// Author + timestamp prefix.
			var stamp = $"[{FormatClock( msg.SentAt )}] {msg.Author}: ";

			// Use a single Label per row so the renderer doesn't have to flex-
			// align child panels - simpler and faster than two Text widgets.
			var label = row.AddChild<Label>();
			label.Text = stamp + msg.Text;

			// Tint per kind. SCSS handles the bulk of the styling; this is the
			// hook for per-message colouring without touching the .sui file.
			// Color.Parse returns Color? - all three hex literals are valid so
			// .Value is safe; defensive callers should null-check in production.
			switch ( msg.Kind )
			{
				case ChatMessageKind.System:
					label.Style.FontColor = Color.Parse( "#4ade80" ).Value;
					break;
				case ChatMessageKind.Emote:
					label.Style.FontColor = Color.Parse( "#fbbf24" ).Value;
					break;
				default:
					label.Style.FontColor = Color.Parse( "#e5e7eb" ).Value;
					break;
			}
			label.Style.Dirty();
		}

		Hud.MessageCountText = _messages.Count == 1
			? "1 message"
			: $"{_messages.Count} messages";

		// Engine Panel API - scroll to the bottom so the newest message lands
		// in view. Cheap no-op if the content already fits.
		list.TryScrollToBottom();
	}

	/// <summary>
	/// Cheap MM:SS formatter for the message timestamp prefix. Time.Now is
	/// seconds-since-server-start; modulo 3600 keeps the display readable.
	/// </summary>
	private static string FormatClock( float secondsSinceStart )
	{
		var s = (int)secondsSinceStart;
		var mm = (s / 60) % 60;
		var ss = s % 60;
		return $"{mm:D2}:{ss:D2}";
	}
}
