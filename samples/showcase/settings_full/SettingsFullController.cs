using System;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace Sandbox.Samples;

/// <summary>
/// Companion Component for the <c>settings_full</c> showcase sample.
///
/// <para>Drop this on a GameObject in any scene, hit Play, and a centred dark card
/// labelled "Settings" appears with a name field, volume slider, music toggle,
/// quality dropdown, and Reset / Cancel / Apply buttons across the bottom.
/// It exercises every input widget the Designer ships plus the Manual /
/// OnChange / OnRelease commit triggers and the <c>Apply.All()</c> save-button
/// pattern.</para>
///
/// <para>The UI lives entirely in <c>settings_full.sui</c>. This Component owns:
/// the three click handlers (Reset / Cancel / Apply), the "dirty" detection that
/// drives <c>StatusText</c>, and the per-frame string conversion that produces
/// the live <c>VolumeLabel</c> ("73%") next to the slider.</para>
///
/// <para><b>Toast feedback.</b> The three buttons (Apply / Cancel / Reset) each
/// briefly overwrite <c>StatusText</c> with a confirmation ("✓ Settings saved",
/// "⤺ Reverted to last saved", "↺ Reset to factory defaults") and the per-frame
/// dirty detection is suppressed for <see cref="ToastDuration"/> seconds so the
/// message stays visible. Without this the buttons would have no visible effect
/// when the card was already clean — a frequent UX complaint.</para>
/// </summary>
public sealed class SettingsFullController : Component
{
	/// <summary>
	/// Generated wrapper instance. Constructed inline so the inspector can
	/// surface the Variables (PlayerName / Volume / MusicEnabled / GraphicsPreset
	/// / StatusText / VolumeLabel) under the Hud foldout.
	/// </summary>
	[Property] public SettingsFull Hud { get; set; } = new();

	// ---- "Saved" snapshot — the last values the player committed via Apply.
	//      Cancel restores these; Reset wipes both saved + current to defaults.
	private string _savedName = "Player";
	private float _savedVolume = 50f;
	private bool _savedMusic = true;
	private int _savedQuality = 2;

	// ---- Defaults — used by Reset and as the initial saved snapshot.
	private const string DefaultName = "Player";
	private const float DefaultVolume = 50f;
	private const bool DefaultMusic = true;
	private const int DefaultQuality = 2;

	// ---- Toast state — when Time.Now < _toastUntilTime the dirty detector in
	//      OnUpdate skips its StatusText write, leaving the confirmation visible.
	private const float ToastDuration = 1.5f;
	private float _toastUntilTime;

	protected override void OnStart()
	{
		// 1) Wire all three Code-mode click handlers BEFORE Show().
		//    Show() triggers SyncFieldsTo, which copies the wrapper's Action
		//    delegates into the rendered Panel. Assigning AFTER Show() leaves
		//    the renderer with null and the click silently no-ops.
		Hud.OnResetClick = OnResetClick;
		Hud.OnCancelClick = OnCancelClick;
		Hud.OnApplyClick = OnApplyClick;

		// 2) Seed the Variables before mount so the first frame is correct.
		Hud.PlayerName = _savedName;
		Hud.Volume = _savedVolume;
		Hud.MusicEnabled = _savedMusic;
		Hud.GraphicsPreset = _savedQuality;
		Hud.VolumeLabel = $"{(int)_savedVolume}%";
		Hud.StatusText = "";

		// 3) Mount as a child of this GameObject. SuiInputMode.All grabs both
		//    cursor and keyboard focus — TextEntry needs the keyboard, the
		//    slider / toggle / dropdown need the cursor.
		Hud.Show( GameObject, SuiInputMode.All );
	}

	protected override void OnUpdate()
	{
		// ---- Live label: Volume is TwoWay/OnChange so it auto-commits every
		//      frame the slider moves. Mirror it into the string Variable so
		//      the bound Text element reads "73%" without a converter.
		Hud.VolumeLabel = $"{(int)Hud.Volume}%";

		// ---- Dirty detection: TextEntry is Manual, so its wrapper field is
		//      only fresh after Apply.All(). To detect typing live we ALSO
		//      flush the TextEntry every frame — cheap because it just reads
		//      the current string out of the rendered TextEntry. The Manual
		//      trigger is still meaningful: the Saved* snapshot only moves on
		//      OnApplyClick, so Cancel restores the pre-edit state correctly.
		Hud.Apply.All();

		// ---- Toast suppression: while a confirmation toast is on-screen the
		//      dirty detector must NOT overwrite StatusText, otherwise the
		//      "✓ Settings saved" message vanishes one frame after Apply.
		if ( Time.Now < _toastUntilTime )
			return;

		bool dirty =
			Hud.PlayerName != _savedName ||
			Math.Abs( Hud.Volume - _savedVolume ) > 0.5f ||
			Hud.MusicEnabled != _savedMusic ||
			Hud.GraphicsPreset != _savedQuality;

		Hud.StatusText = dirty ? "● Unsaved changes" : "";
	}

	protected override void OnDestroy()
	{
		Hud?.Remove();
	}

	// ────────────────────────────────────────────────────────────────────────
	// Click handlers — assigned to the wrapper's Action properties in OnStart.
	// ────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Apply button: commit every Manual binding (TextEntry → PlayerName),
	/// then snapshot the current values as "saved" so the dirty check
	/// settles back to clean. Surfaces a ✓ toast for <see cref="ToastDuration"/>s.
	/// </summary>
	private void OnApplyClick()
	{
		// Volume / Music / Quality auto-commit (OnChange) — no Apply needed.
		// TextEntry is Manual; Apply.All() flushes every Manual binding the
		// document declares in one call, which is the canonical Save pattern.
		Hud.Apply.All();

		_savedName = Hud.PlayerName;
		_savedVolume = Hud.Volume;
		_savedMusic = Hud.MusicEnabled;
		_savedQuality = Hud.GraphicsPreset;

		SetToast( "✓ Settings saved" );
		Log.Info(
			$"[SettingsFull] Applied: Name='{_savedName}' Volume={(int)_savedVolume} " +
			$"Music={_savedMusic} Quality={_savedQuality}" );
	}

	/// <summary>
	/// Cancel button: revert the in-flight edits back to the last saved
	/// snapshot. Writing the wrapper field updates the rendered widget
	/// because the bindings are TwoWay. Surfaces a ⤺ toast.
	/// </summary>
	private void OnCancelClick()
	{
		Hud.PlayerName = _savedName;
		Hud.Volume = _savedVolume;
		Hud.MusicEnabled = _savedMusic;
		Hud.GraphicsPreset = _savedQuality;

		SetToast( "⤺ Reverted to last saved" );
		Log.Info(
			$"[SettingsFull] Reverted to: Name='{_savedName}' Volume={(int)_savedVolume} " +
			$"Music={_savedMusic} Quality={_savedQuality}" );
	}

	/// <summary>
	/// Reset button: wipe everything (current AND saved) back to the hard-coded
	/// defaults. The card visually returns to "Player / 50% / Music on / High".
	/// Surfaces a ↺ toast.
	/// </summary>
	private void OnResetClick()
	{
		_savedName = DefaultName;
		_savedVolume = DefaultVolume;
		_savedMusic = DefaultMusic;
		_savedQuality = DefaultQuality;

		Hud.PlayerName = DefaultName;
		Hud.Volume = DefaultVolume;
		Hud.MusicEnabled = DefaultMusic;
		Hud.GraphicsPreset = DefaultQuality;

		SetToast( "↺ Reset to factory defaults" );
		Log.Info(
			$"[SettingsFull] Reset to factory defaults: Name='{DefaultName}' " +
			$"Volume={(int)DefaultVolume} Music={DefaultMusic} Quality={DefaultQuality}" );
	}

	/// <summary>
	/// Latches a confirmation message into <see cref="SettingsFull.StatusText"/>
	/// for <see cref="ToastDuration"/> seconds. OnUpdate's dirty detector
	/// honours the latch and skips its own StatusText write until the timer
	/// expires, so the toast stays on-screen instead of being overwritten by
	/// the next frame.
	/// </summary>
	private void SetToast( string message )
	{
		_toastUntilTime = Time.Now + ToastDuration;
		Hud.StatusText = message;
	}
}
