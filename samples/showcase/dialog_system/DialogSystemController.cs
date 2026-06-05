using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>dialog_system</c> showcase. Drives a small
/// NPC conversation tree: portrait, speaker name, line of text revealed
/// letter-by-letter (typewriter), and 1-3 choice buttons that branch to
/// other nodes. Click anywhere on the card while text is still typing to
/// skip the animation; clicking a choice when the line is complete
/// advances the conversation.
///
/// <para>Stress-tests several pipeline areas at once:</para>
/// <list type="number">
/// <item>Per-frame mutation of an <c>ExposeAsVariable</c> Label
/// (<c>DialogText</c>) — the typewriter rewrites .Text every ~25ms.</item>
/// <item>Runtime <c>AddChild&lt;Panel&gt;</c> with <c>onclick</c> listeners
/// (the choice buttons) plus full teardown + rebuild on each node
/// transition.</item>
/// <item>State-machine driven UI swap — Portrait colour, SpeakerName,
/// DialogText, and ChoicesContainer all change in lockstep when the
/// player picks a choice.</item>
/// <item>Card-level <c>onclick</c> for skip-typewriter — listener attached
/// via the @ref capture one-shot since <c>Hud.View?.Card</c> is null at
/// OnStart time.</item>
/// </list>
/// </summary>
public sealed class DialogSystemController : Component
{
	[Property] public DialogSystem Hud { get; set; } = new();

	/// <summary>Seconds between each character of the typewriter.</summary>
	[Property] public float TypewriterDelay { get; set; } = 0.025f;

	// ────────────────────────────────────────────────────────────────────
	// Dialog model
	// ────────────────────────────────────────────────────────────────────

	private struct DialogNode
	{
		public string Speaker;
		public string PortraitColor;
		public string Text;
		public DialogChoice[] Choices;
	}

	private struct DialogChoice
	{
		public string Label;
		public int NextNodeId; // -1 = close dialog
	}

	private Dictionary<int, DialogNode> _nodes;
	private int _currentNodeId;

	// ────────────────────────────────────────────────────────────────────
	// Typewriter state
	// ────────────────────────────────────────────────────────────────────

	private string _currentText = "";
	private int _typewriterIndex;
	private float _nextCharAt;
	private bool _typewriterDone = true; // true so OnStart's first StartNode kicks it off

	// ────────────────────────────────────────────────────────────────────
	// Bootstrap flags — @ref fields are null until first paint.
	// ────────────────────────────────────────────────────────────────────

	private bool _viewBootstrapped;

	// Deferred node transition. Choice buttons fire onclick handlers from
	// inside Sandbox.UI's event dispatch loop; calling
	// ChoicesContainer.DeleteChildren mid-dispatch confuses the engine's
	// child iteration (logs "Index was out of range"). Setting these
	// flags inside the handler and processing them in OnUpdate next frame
	// keeps the mutation outside the dispatch.
	private int? _pendingNodeId;
	private bool _pendingClose;

	protected override void OnStart()
	{
		BuildDialogTree();

		Hud.Show( GameObject, SuiInputMode.All );

		// Stage the first node — actual visual application happens once
		// the @refs become available in OnUpdate.
		_currentNodeId = 0;
	}

	protected override void OnUpdate()
	{
		// Bootstrap: Card + ChoicesContainer must be live before we wire
		// the skip-click listener and stage the first conversation node.
		// DialogText is no longer ExposeAsVariable — it's driven through
		// the bound DialogText Variable, which works without @ref capture.
		if ( !_viewBootstrapped )
		{
			if ( Hud.View?.Card != null && Hud.View?.ChoicesContainer != null )
			{
				Hud.View.Card.AddEventListener( "onclick", OnCardClick );
				ApplyNode( _currentNodeId );
				_viewBootstrapped = true;
			}
		}

		// Process deferred choice transitions / close. Mutating the
		// ChoicesContainer's children from inside the button's onclick
		// throws Index-out-of-range inside the engine's event dispatch
		// loop; doing it here keeps the mutation outside the dispatch.
		if ( _pendingClose )
		{
			_pendingClose = false;
			Hud?.Remove();
			return;
		}
		if ( _pendingNodeId.HasValue )
		{
			var id = _pendingNodeId.Value;
			_pendingNodeId = null;
			ApplyNode( id );
		}

		// Typewriter advance — writes to the bound DialogText Variable
		// instead of poking the live label. Direct .Text mutation on
		// captured @ref labels gets stomped when the panel re-renders
		// (any AddChild / DeleteChildren on a sibling container can
		// trigger a re-render, which re-emits the static template body).
		// Variable-driven content survives because the binding re-applies
		// on every render.
		if ( !_typewriterDone && Time.Now >= _nextCharAt )
		{
			if ( _typewriterIndex < _currentText.Length )
			{
				_typewriterIndex++;
				Hud.DialogText = _currentText.Substring( 0, _typewriterIndex );
				_nextCharAt = Time.Now + TypewriterDelay;
			}
			else
			{
				FinishTypewriter();
			}
		}
	}

	protected override void OnDestroy()
	{
		Hud?.Remove();
	}

	// ────────────────────────────────────────────────────────────────────
	// Node transitions
	// ────────────────────────────────────────────────────────────────────

	private void ApplyNode( int nodeId )
	{
		if ( !_nodes.TryGetValue( nodeId, out var node ) ) return;

		_currentNodeId = nodeId;
		_currentText = node.Text;
		_typewriterIndex = 0;
		_typewriterDone = false;
		_nextCharAt = Time.Now + TypewriterDelay;

		if ( Hud.View?.SpeakerName != null )
			Hud.View.SpeakerName.Text = node.Speaker;

		if ( Hud.View?.Portrait != null )
			Hud.View.Portrait.Style.BackgroundColor = Color.Parse( node.PortraitColor ).Value;

		// Bound Variable — survives panel re-renders. Wrapper setter
		// triggers the OneWay binding which writes Text on the live Label.
		Hud.DialogText = "";

		if ( Hud.View?.ContinueHint != null )
		{
			Hud.View.ContinueHint.Style.Opacity = 1f;
			Hud.View.ContinueHint.Style.Dirty();
		}

		// Wipe any choice buttons left over from the previous node.
		if ( Hud.View?.ChoicesContainer != null )
			Hud.View.ChoicesContainer.DeleteChildren( true );
	}

	private void FinishTypewriter()
	{
		_typewriterDone = true;
		Hud.DialogText = _currentText;

		if ( Hud.View?.ContinueHint != null )
		{
			Hud.View.ContinueHint.Style.Opacity = 0f;
			Hud.View.ContinueHint.Style.Dirty();
		}

		SpawnChoices();
	}

	private void OnCardClick()
	{
		// Mid-line click — fast-forward the typewriter to the end. The
		// typewriter-done branch (full text shown) doesn't react to card
		// clicks; the player has to commit to a choice.
		if ( _typewriterDone ) return;

		_typewriterIndex = _currentText.Length;
		FinishTypewriter();
	}

	private void SpawnChoices()
	{
		var container = Hud.View?.ChoicesContainer;
		if ( container == null ) return;
		container.DeleteChildren( true );

		if ( !_nodes.TryGetValue( _currentNodeId, out var node ) || node.Choices == null )
			return;

		foreach ( var choice in node.Choices )
		{
			var btn = container.AddChild<Panel>();
			btn.AddClass( "dialog-choice" );

			var label = btn.AddChild<Label>();
			label.AddClass( "dialog-choice-label" );
			label.Text = choice.Label;

			int capturedNextId = choice.NextNodeId;
			btn.AddEventListener( "onclick", () => OnChoiceClick( capturedNextId ) );
		}
	}

	private void OnChoiceClick( int nextNodeId )
	{
		// Defer both branches to OnUpdate so the mutation (DeleteChildren /
		// Hud.Remove) happens outside Sandbox.UI's event dispatch loop —
		// mutating the container's children mid-dispatch throws
		// Index-out-of-range inside the engine while iterating siblings.
		if ( nextNodeId < 0 )
			_pendingClose = true;
		else
			_pendingNodeId = nextNodeId;
	}

	// ────────────────────────────────────────────────────────────────────
	// Dialog tree — small 6-node conversation with a quest-giver NPC.
	// ────────────────────────────────────────────────────────────────────

	private void BuildDialogTree()
	{
		_nodes = new Dictionary<int, DialogNode>
		{
			[0] = new DialogNode
			{
				Speaker = "Eldrin the Gatekeeper",
				PortraitColor = "#6366f1",
				Text = "Greetings, traveler. The road ahead is fraught with peril. State your business and be quick about it.",
				Choices = new[]
				{
					new DialogChoice { Label = "Who are you?", NextNodeId = 1 },
					new DialogChoice { Label = "I'm looking for work.", NextNodeId = 2 },
					new DialogChoice { Label = "Just passing through.", NextNodeId = 4 },
				}
			},
			[1] = new DialogNode
			{
				Speaker = "Eldrin the Gatekeeper",
				PortraitColor = "#6366f1",
				Text = "I am Eldrin, keeper of this gate for three hundred winters. Few who pass me return the way they came.",
				Choices = new[]
				{
					new DialogChoice { Label = "What lies beyond the gate?", NextNodeId = 3 },
					new DialogChoice { Label = "Tell me about that work.", NextNodeId = 2 },
					new DialogChoice { Label = "Farewell.", NextNodeId = 4 },
				}
			},
			[2] = new DialogNode
			{
				Speaker = "Eldrin the Gatekeeper",
				PortraitColor = "#6366f1",
				Text = "The old copper mine to the east has been overrun by goblins. Clear them out and I'll see the council pays you 200 gold.",
				Choices = new[]
				{
					new DialogChoice { Label = "I'll take the job.", NextNodeId = 5 },
					new DialogChoice { Label = "Too dangerous for me.", NextNodeId = 4 },
				}
			},
			[3] = new DialogNode
			{
				Speaker = "Eldrin the Gatekeeper",
				PortraitColor = "#6366f1",
				Text = "A cursed land. Skies the color of bruises, soil that whispers at night. Nothing alive returns from it.",
				Choices = new[]
				{
					new DialogChoice { Label = "I'm not afraid.", NextNodeId = 5 },
					new DialogChoice { Label = "I'll heed your warning.", NextNodeId = 4 },
				}
			},
			[4] = new DialogNode
			{
				Speaker = "Eldrin the Gatekeeper",
				PortraitColor = "#6366f1",
				Text = "Safe travels, then. May the road bring you home before the dark does.",
				Choices = new[]
				{
					new DialogChoice { Label = "Goodbye.", NextNodeId = -1 },
				}
			},
			[5] = new DialogNode
			{
				Speaker = "Eldrin the Gatekeeper",
				PortraitColor = "#22c55e",
				Text = "Then it is settled, brave one. The mine entrance is half a day's walk east. Return when the deed is done.",
				Choices = new[]
				{
					new DialogChoice { Label = "I'm on my way.", NextNodeId = -1 },
				}
			},
		};
	}
}
