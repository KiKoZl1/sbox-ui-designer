using System.Collections.Generic;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>quest_journal</c> showcase sample — a centred
/// RPG-style quest journal card with three tabs (Active / Completed / Failed),
/// a quest list column on the left, and a detail column on the right showing
/// the selected quest's title, description, three objective rows (label +
/// progress bar each), and a reward line.
///
/// <para>Drop this on a GameObject in any scene, hit Play, and the card mounts
/// over a dimming scrim. Click the tab buttons to switch the seed quest set,
/// click any quest card on the left to see its detail pane update on the right.
/// Press <c>Tab</c> (Score action) to nudge objective #1 forward by 10% so you
/// can watch the bound ProgressBar animate; press <c>R</c> (Reload action) to
/// reset all objectives back to their authored defaults.</para>
///
/// <para>The UI lives entirely in <c>quest_journal.sui</c>. This Component owns:
/// the in-memory <see cref="Quest"/> list per tab, the selection state, and the
/// six Code-mode click handlers (3 tabs + 3 quest cards). The visual swap for
/// "selected tab" / "selected card" is fully Designer-authored via
/// <c>HighlightedStyle</c> + the <c>IsHighlighted</c> bindable — the controller
/// just toggles six bound <c>bool</c> Variables and the runtime restyle follows.
/// No <c>Hud.View?.*</c> Panel mutation, no <c>Style.Dirty()</c>.</para>
/// </summary>
public sealed class QuestJournalController : Component
{
	// ─────────────────────────────────────────────────────────────────────────
	// Data model — kept in C# because List<custom-POCO> isn't a valid SUI
	// Variable type. The .sui only knows about the Variables that drive the
	// bound Text / ProgressBar / IsHighlighted elements; the data list pushes
	// into those Variables every time the user clicks a quest card or a tab.
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>One objective on a quest — label + 0..1 progress.</summary>
	public sealed class Objective
	{
		[Property] public string Text { get; set; } = "";
		[Property, Range( 0f, 1f )] public float Progress { get; set; } = 0f;
	}

	/// <summary>One quest entry in any of the three tabs.</summary>
	public sealed class Quest
	{
		[Property] public string Title { get; set; } = "";
		[Property] public string Description { get; set; } = "";
		[Property] public string Reward { get; set; } = "";
		/// <summary>Up to 3 objectives are rendered (slots 4/5/6 in the .sui).
		/// Extra entries are ignored; missing entries leave the row blank.</summary>
		[Property] public List<Objective> Objectives { get; set; } = new();
	}

	/// <summary>Which tab is on screen. Used by <see cref="CurrentQuests"/> to pick the list.</summary>
	public enum QuestTab
	{
		Active,
		Completed,
		Failed,
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Wrapper + public state.
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>Generated wrapper instance. Exposes the 19 Variables plus the
	/// 6 <c>OnXxxClick</c> Action properties. The 6 <c>IsXxxHighlighted</c>
	/// bools are bound OneWay to the matching Button's <c>IsHighlighted</c> in
	/// the .sui — set them from here and the runtime SUI host adds/removes the
	/// <c>.highlighted</c> CSS class, which triggers each Button's authored
	/// <c>HighlightedStyle</c> visuals.</summary>
	[Property] public QuestJournal Hud { get; set; } = new();

	/// <summary>Seed Active quests. Edit from the Inspector to reshape the demo
	/// without touching code — same pattern as <c>inventory_grid_full</c>.</summary>
	[Property, Group( "Quests" )]
	public List<Quest> ActiveQuests { get; set; } = new()
	{
		new Quest
		{
			Title = "Slay 10 Zombies",
			Description = "Reports of zombie sightings near the village graveyard. Clear them out.",
			Reward = "300 gold + Iron Sword",
			Objectives = new List<Objective>
			{
				new() { Text = "Slay zombies (3/10)", Progress = 0.3f },
				new() { Text = "Return to Mayor",     Progress = 0.0f },
			},
		},
		new Quest
		{
			Title = "Collect 5 Herbs",
			Description = "The alchemist needs fresh moonflower petals from the eastern meadow.",
			Reward = "150 gold + Health Potion x3",
			Objectives = new List<Objective>
			{
				new() { Text = "Gather moonflower (2/5)", Progress = 0.4f },
				new() { Text = "Deliver to alchemist",    Progress = 0.0f },
			},
		},
		new Quest
		{
			Title = "Talk to Mayor",
			Description = "The mayor sent a courier asking you to visit his office at noon.",
			Reward = "Story progression",
			Objectives = new List<Objective>
			{
				new() { Text = "Visit mayor's office", Progress = 0.0f },
			},
		},
	};

	/// <summary>Seed Completed quests.</summary>
	[Property, Group( "Quests" )]
	public List<Quest> CompletedQuests { get; set; } = new()
	{
		new Quest
		{
			Title = "Find the Lost Cat",
			Description = "Mrs. Greta's tabby was hiding in the church belfry. Reunited.",
			Reward = "50 gold",
			Objectives = new List<Objective>
			{
				new() { Text = "Search the church (1/1)", Progress = 1.0f },
				new() { Text = "Return cat to owner",     Progress = 1.0f },
			},
		},
		new Quest
		{
			Title = "Clear the Cellar",
			Description = "Rats had taken over the inn's wine cellar. They have been dealt with.",
			Reward = "75 gold + Free meal",
			Objectives = new List<Objective>
			{
				new() { Text = "Kill rats (8/8)", Progress = 1.0f },
			},
		},
	};

	/// <summary>Seed Failed quests.</summary>
	[Property, Group( "Quests" )]
	public List<Quest> FailedQuests { get; set; } = new()
	{
		new Quest
		{
			Title = "Escort the Merchant",
			Description = "The merchant's caravan was ambushed on the road north. He did not survive.",
			Reward = "—",
			Objectives = new List<Objective>
			{
				new() { Text = "Reach Northguard (failed)", Progress = 0.6f },
			},
		},
	};

	// ─────────────────────────────────────────────────────────────────────────
	// Runtime selection state.
	// ─────────────────────────────────────────────────────────────────────────

	private QuestTab _currentTab = QuestTab.Active;
	private int _selectedIndex = 0;

	/// <summary>Active tab's quest list (alias into one of the three seed Lists).</summary>
	private List<Quest> CurrentQuests => _currentTab switch
	{
		QuestTab.Active    => ActiveQuests,
		QuestTab.Completed => CompletedQuests,
		QuestTab.Failed    => FailedQuests,
		_                  => ActiveQuests,
	};

	// ─────────────────────────────────────────────────────────────────────────
	// Lifecycle.
	// ─────────────────────────────────────────────────────────────────────────

	protected override void OnStart()
	{
		// 1) Wire all six Code-mode click handlers BEFORE Show().
		//    Show() triggers SyncFieldsTo, which copies the wrapper's Action
		//    delegates into the rendered Panel. Assigning AFTER Show() leaves
		//    the renderer with null and the click silently no-ops — same gotcha
		//    documented in counter_button and settings_full.
		Hud.OnTabActiveClick    = OnTabActiveClick;
		Hud.OnTabCompletedClick = OnTabCompletedClick;
		Hud.OnTabFailedClick    = OnTabFailedClick;
		Hud.OnQuest1Click       = OnQuest1Click;
		Hud.OnQuest2Click       = OnQuest2Click;
		Hud.OnQuest3Click       = OnQuest3Click;

		// 2) Mount the card. MouseOnly so the player can click tabs / cards
		//    without the journal grabbing keyboard focus from gameplay. The
		//    six IsXxxHighlighted Variables default to (true, false, false)
		//    for tabs and (true, false, false) for cards in the .sui, so frame
		//    zero already matches _currentTab = Active + _selectedIndex = 0
		//    without any push from here.
		Hud.Show( GameObject, SuiInputMode.MouseOnly );

		// 3) Push the initial list labels + detail-pane fields so frame zero
		//    matches the runtime state instead of leaning on the authored
		//    canvas preview values (which would silently drift the second
		//    somebody re-orders the seed quests). No visual-restyle calls
		//    needed — that's all in the .sui now.
		RefreshCardLabels();
		PushSelectedQuest();
	}

	protected override void OnUpdate()
	{
		// Built-in test hotkeys — Tab (Score action) advances objective #1 of the
		// selected quest by 10% so you can watch the bound ProgressBar animate.
		// R (Reload action) resets every objective to its authored default. Both
		// actions are mapped in the default s&box input layout, so the sample
		// works drop-in without project-side input config. Remove or remap when
		// you wire the journal to a real quest-progress source.
		if ( Input.Pressed( "Score" ) )
		{
			var q = SelectedQuest();
			if ( q != null && q.Objectives.Count > 0 )
			{
				var obj = q.Objectives[0];
				obj.Progress = MathX.Clamp( obj.Progress + 0.1f, 0f, 1f );
				PushSelectedQuest();
				Log.Info( $"[QuestJournal] Bumped '{q.Title}' objective #1 to {(int)( obj.Progress * 100 )}%." );
			}
		}
		if ( Input.Pressed( "Reload" ) )
		{
			ResetAllProgress();
			PushSelectedQuest();
			Log.Info( "[QuestJournal] Reset all objective progress to defaults." );
		}
	}

	protected override void OnDestroy()
	{
		Hud?.Remove();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Click handlers — assigned to the wrapper's Action properties in OnStart.
	// ─────────────────────────────────────────────────────────────────────────

	private void OnTabActiveClick()    => SwitchTab( QuestTab.Active );
	private void OnTabCompletedClick() => SwitchTab( QuestTab.Completed );
	private void OnTabFailedClick()    => SwitchTab( QuestTab.Failed );

	private void OnQuest1Click() => SelectQuest( 0 );
	private void OnQuest2Click() => SelectQuest( 1 );
	private void OnQuest3Click() => SelectQuest( 2 );

	// ─────────────────────────────────────────────────────────────────────────
	// State mutation.
	//
	// The tab + card "selected" visual swap is driven entirely by toggling the
	// IsXxxHighlighted bools on the wrapper. The OneWay binding pipeline pushes
	// the change into each Button's IsHighlighted property next frame, which
	// adds/removes the .highlighted CSS class and lets the authored
	// HighlightedStyle take over. Zero direct Panel mutation from here.
	// ─────────────────────────────────────────────────────────────────────────

	private void SwitchTab( QuestTab tab )
	{
		// Always run the refresh chain — even on a same-tab reclick we want the
		// selection to reset to 0 and the card highlight to settle. Without this
		// re-entry the user gets a "dead click" feel if they re-click the tab
		// they already had open.
		_currentTab = tab;
		_selectedIndex = 0;
		Hud.CurrentTabText = tab.ToString();

		// Flip the three tab highlight bools so exactly one tab shows the
		// green HighlightedStyle. Same shape for the three card bools below —
		// _selectedIndex was just reset to 0, so card #1 lights up.
		Hud.IsTabActiveHighlighted    = tab == QuestTab.Active;
		Hud.IsTabCompletedHighlighted = tab == QuestTab.Completed;
		Hud.IsTabFailedHighlighted    = tab == QuestTab.Failed;

		Hud.IsQuestCard1Highlighted = true;
		Hud.IsQuestCard2Highlighted = false;
		Hud.IsQuestCard3Highlighted = false;

		// Refresh the list column with the first 3 entries of the new tab,
		// then push the first quest into the detail pane.
		RefreshCardLabels();
		PushSelectedQuest();

		Log.Info( $"[QuestJournal] Switched to tab '{tab}'." );
	}

	private void SelectQuest( int index )
	{
		var list = CurrentQuests;
		if ( index < 0 || index >= list.Count ) return;
		if ( _selectedIndex == index ) return;

		_selectedIndex = index;

		// One of three quest cards is highlighted at any time — flip the bools
		// so the bound IsHighlighted on each Button reflects the selection.
		Hud.IsQuestCard1Highlighted = index == 0;
		Hud.IsQuestCard2Highlighted = index == 1;
		Hud.IsQuestCard3Highlighted = index == 2;

		PushSelectedQuest();

		Log.Info( $"[QuestJournal] Selected quest #{index}: '{list[index].Title}'." );
	}

	private Quest SelectedQuest()
	{
		var list = CurrentQuests;
		if ( _selectedIndex < 0 || _selectedIndex >= list.Count ) return null;
		return list[_selectedIndex];
	}

	private void ResetAllProgress()
	{
		// Reset uses each List's authored seed via re-reading the [Property]
		// defaults isn't possible at runtime — instead we just zero every
		// in-flight objective. Authors who want a "true" reset should keep a
		// snapshot in OnStart (same pattern as settings_full's _savedX
		// fields).
		foreach ( var q in CurrentQuests )
		{
			foreach ( var obj in q.Objectives )
			{
				obj.Progress = 0f;
			}
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Variable push helpers — controller → SUI Variables → bound elements.
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>Push the three quest-card titles into the
	/// <c>QuestCard1Text</c>/<c>QuestCard2Text</c>/<c>QuestCard3Text</c>
	/// Variables. The three OneWay bindings on the card Buttons' ButtonText
	/// property pick the change up on the next frame, so the LEFT column
	/// always reflects the active tab's quest list.</summary>
	private void RefreshCardLabels()
	{
		var list = CurrentQuests;

		Hud.QuestCard1Text = list.Count > 0 ? list[0].Title : "—";
		Hud.QuestCard2Text = list.Count > 1 ? list[1].Title : "—";
		Hud.QuestCard3Text = list.Count > 2 ? list[2].Title : "—";
	}

	/// <summary>Push the currently-selected quest's title/description/objectives/
	/// reward into the SUI Variables. The bound Text and ProgressBar elements
	/// pick up the change on the next frame via the OneWay binding pipeline.</summary>
	private void PushSelectedQuest()
	{
		var q = SelectedQuest();
		if ( q == null )
		{
			// Empty tab fallback — clear every detail field.
			Hud.SelectedQuestTitle = "—";
			Hud.SelectedQuestDescription = "No quest selected.";
			Hud.Objective1Text = "";
			Hud.Objective1Progress = 0f;
			Hud.Objective2Text = "";
			Hud.Objective2Progress = 0f;
			Hud.Objective3Text = "";
			Hud.Objective3Progress = 0f;
			Hud.SelectedQuestRewardText = "—";
			return;
		}

		Hud.SelectedQuestTitle = q.Title;
		Hud.SelectedQuestDescription = q.Description;
		Hud.SelectedQuestRewardText = q.Reward;

		Hud.Objective1Text     = q.Objectives.Count > 0 ? q.Objectives[0].Text     : "";
		Hud.Objective1Progress = q.Objectives.Count > 0 ? q.Objectives[0].Progress : 0f;
		Hud.Objective2Text     = q.Objectives.Count > 1 ? q.Objectives[1].Text     : "";
		Hud.Objective2Progress = q.Objectives.Count > 1 ? q.Objectives[1].Progress : 0f;
		Hud.Objective3Text     = q.Objectives.Count > 2 ? q.Objectives[2].Text     : "";
		Hud.Objective3Progress = q.Objectives.Count > 2 ? q.Objectives[2].Progress : 0f;
	}
}
