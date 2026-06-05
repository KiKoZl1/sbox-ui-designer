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
/// inside the input), and the message gets appended to a runtime-populated
/// message list with timestamp + author. The header counter ("12 messages")
/// updates after every send.</para>
///
/// <para>The UI lives entirely in <c>chat_panel.sui</c>. This Component owns:
/// the runtime message store (<see cref="_messages"/>), the OnSendClick handler,
/// the <see cref="RenderMessages"/> routine that rebuilds the runtime panel, and
/// /command coloring (system messages are tinted green; /me is italic).</para>
///
/// <para><b>Why ExposeAsVariable matters here.</b> The <c>MessageList</c> Panel
/// and <c>ChatInput</c> TextEntry are both flagged <c>ExposeAsVariable=true</c>
/// in the <c>.sui</c>. The generator therefore emits typed fields on the View
/// class, so this controller can reach them via <c>Hud.View?.MessageList</c> to
/// AddChild / DeleteChildren / ScrollToBottom, and call
/// <c>Hud.View?.ChatInputRef.Text = ""</c> to clear the input after a send.
/// Without the flag the only escape hatch would be <c>find-by-class</c> on the
/// rendered panel tree, which is brittle.</para>
///
/// <para><b>Enter to submit.</b> The Sandbox.UI TextEntry fires an
/// <c>"onsubmit"</c> event when the user presses Enter inside the focused field
/// (mirroring real chat clients). We wire that listener in <see cref="OnUpdate"/>
/// once the engine has captured the @ref — binding in OnStart would no-op
/// because the @ref is null until the first paint.</para>
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
	/// are trimmed off the front so the panel never grows unbounded.
	/// </summary>
	[Property] public int MaxMessages { get; set; } = 64;

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

		// 4) Seed a friendly welcome line so the panel isn't blank. RenderMessages
		//    here would no-op because the engine captures @ref to MessageList
		//    AFTER the first paint — the bootstrap one-shot in OnUpdate flushes
		//    it the moment View.MessageList becomes non-null.
		_messages.Add( new ChatMessage
		{
			Author = SystemAuthor,
			Text = "Welcome - type a message and hit Send (or press Enter).",
			SentAt = Time.Now,
			Kind = ChatMessageKind.System,
		} );
	}

	private bool _onsubmitWired;
	private bool _initialRendered;

	protected override void OnUpdate()
	{
		// Two independent one-shots. Each fires the frame its respective @ref
		// becomes non-null (which happens after the engine paints the panel for
		// the first time — OnStart sees both as null). Splitting them protects
		// against the unlikely case where the two refs capture on different
		// frames; each flips its flag and the branch never enters again.
		if ( !_onsubmitWired )
		{
			var entry = Hud.View?.ChatInputRef;
			if ( entry != null )
			{
				entry.AddEventListener( "onsubmit", () => OnSendClick() );
				_onsubmitWired = true;
			}
		}
		if ( !_initialRendered )
		{
			if ( Hud.View?.MessageList != null )
			{
				RenderMessages();
				_initialRendered = true;
			}
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

		// Clear the input for the next message. Writing to Hud.ChatInputText
		// updates the wrapper but Sandbox.UI's Razor doesn't push that back to
		// the rendered TextEntry on re-render, so the widget keeps showing the
		// last draft. Push directly onto the engine TextEntry via the @ref so
		// the field is visually empty.
		Hud.ChatInputText = "";
		if ( Hud.View?.ChatInputRef != null )
			Hud.View.ChatInputRef.Text = "";
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
