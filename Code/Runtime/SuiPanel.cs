using Sandbox;
using Sandbox.UI;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Base for the user-facing wrapper class the generator emits for each
/// <c>.sui</c> (V1.5-M2-K7 — see DEVIATIONS D-014). The dev declares a field
/// on their own Component (<c>[Property] public WdgSelectJob Widget = new();</c>)
/// and controls the panel's lifetime from gameplay code:
///
/// <code>
///   Widget.Add();      // mount (hidden) — creates child GO + ScreenPanel + SuiHostPanelComponent + the Panel
///   Widget.Show();     // mount-if-needed + visible
///   Widget.Hide();     // hide (keeps the mount around)
///   Widget.Remove();   // tear down the mount entirely
///   Widget.Health = 75; // edit any [Property] mirror; call RefreshView() to push
/// </code>
///
/// <para><see cref="TView"/> is the generated <c>Panel</c> subclass that
/// actually renders the markup. Each <c>.sui</c> compiles to <c>&lt;Name&gt; : Panel</c>
/// (NOT a PanelComponent) because s&amp;box Razor only allows <c>Panel</c>
/// subclasses to be nested via <c>&lt;Game.UI.Foo /&gt;</c> markup. Composition
/// (parent embedding child via SuiReference) requires nestable subtypes.
/// Standalone mounting wraps the <c>Panel</c> in a generic host PanelComponent
/// (see <see cref="SuiHostPanelComponent"/>) so it has a ScreenPanel root.</para>
///
/// <para>V1.5 ships single-mount semantics: one Add() per instance. The
/// per-Connection variant (UMG/UEFN-style <c>UIMap[player]</c>) is V1.6
/// polish on top of this class via a manager wrapper.</para>
/// </summary>
public abstract class SuiPanel<TView> where TView : Panel, new()
{
	/// <summary>
	/// The mounted host GameObject; null until <see cref="Add"/> runs.
	/// <c>[Hide]</c> + <c>[JsonIgnore]</c> stop the s&amp;box reflection-based
	/// inspector and serializer from walking into runtime types (Panel has
	/// non-serializable members like <c>PanelCreator</c>).
	/// </summary>
	[Hide, System.Text.Json.Serialization.JsonIgnore]
	public GameObject MountedObject { get; private set; }

	/// <summary>The internal host PanelComponent. Same hide reason as <see cref="MountedObject"/>.</summary>
	[Hide, System.Text.Json.Serialization.JsonIgnore]
	public SuiHostPanelComponent Host { get; private set; }

	/// <summary>
	/// The live Panel doing the rendering; null until mounted. Public so a
	/// parent wrapper can reference this child wrapper's View from its own
	/// renderer when embedded as a named SuiReference. Hidden from
	/// inspector + serializer for the same Panel-non-serializable reason.
	/// </summary>
	[Hide, System.Text.Json.Serialization.JsonIgnore]
	public TView View { get; private set; }

	/// <summary>True while a mount exists (Add/Show called, Remove not yet).</summary>
	[Hide, System.Text.Json.Serialization.JsonIgnore]
	public bool IsMounted => MountedObject.IsValid();

	/// <summary>
	/// True when the wrapper considers itself "visible". For a standalone
	/// (mounted) wrapper this drives the View's CSS display; for an embedded
	/// wrapper (used as a SuiReference child of another wrapper) this flag is
	/// hashed into <see cref="ContentHash"/> so the parent re-renders and the
	/// nested tag emits <c>display: none</c> inline.
	/// </summary>
	[Hide, System.Text.Json.Serialization.JsonIgnore]
	public bool IsShown => _isVisible;

	[Hide, System.Text.Json.Serialization.JsonIgnore]
	private bool _isVisible = true;

	/// <summary>
	/// True when this wrapper has been claimed as the child of another wrapper
	/// (via the generated <c>SyncFieldsTo</c> call). Embedded wrappers don't
	/// own a mount — their View is constructed by the parent's Razor markup —
	/// so <see cref="Show"/> on an embedded wrapper must NOT auto-mount or
	/// you get a phantom second copy floating in the scene (V1.5-M2-K7-bugfix:
	/// "TestInnerHide press Slot3 spawned a duplicate HudBindtest" bug).
	/// </summary>
	[Hide, System.Text.Json.Serialization.JsonIgnore]
	public bool IsEmbedded { get; private set; }

	/// <summary>Generator hook — called from the parent's <c>SyncFieldsTo</c>.</summary>
	public void MarkEmbedded() => IsEmbedded = true;

	/// <summary>
	/// Inline-style fragment emitted into the parent's Razor markup so that
	/// an embedded wrapper's <see cref="Hide"/> takes effect on the nested
	/// Panel without us having to escape ternary expressions inside the
	/// generated <c>style="..."</c> attribute. Returns null when visible
	/// (Razor swallows null → no attribute), <c>"display: none;"</c> when not.
	/// </summary>
	[Hide, System.Text.Json.Serialization.JsonIgnore]
	public string HiddenStyle => _isVisible ? null : "display: none;";

	/// <summary>
	/// Mount the panel as a child of <paramref name="parent"/> (or scene root
	/// if null). The mount is created hidden — call <see cref="Show"/> to make
	/// it visible. Idempotent: a second Add() while mounted is a no-op.
	/// </summary>
	public void Add( GameObject parent = null )
	{
		if ( MountedObject.IsValid() ) return;

		var scene = parent?.Scene ?? Game.ActiveScene;
		if ( !scene.IsValid() )
		{
			Log.Warning( $"[SUI] {GetType().Name}.Add() — no active scene; mount aborted." );
			return;
		}

		// V1.5-M2-K7 — create the GameObject ENABLED so OnEnabled fires on
		// the host's PanelComponent immediately, populating Host.Panel before
		// we try to attach the View.
		MountedObject = scene.CreateObject( true );
		MountedObject.Name = typeof( TView ).Name + "_Mount";
		if ( parent.IsValid() ) MountedObject.SetParent( parent );

		MountedObject.Components.Create<ScreenPanel>();
		Host = MountedObject.Components.Create<SuiHostPanelComponent>();

		EnsureViewAttached();
	}

	/// <summary>
	/// Mark visible. For a standalone wrapper this auto-mounts (one-call
	/// entry from gameplay code: <c>Hud.Show();</c>). For an embedded wrapper
	/// this only flips <see cref="IsShown"/>; the parent's next render
	/// reflects it via the nested tag's inline style.
	/// </summary>
	public void Show( GameObject parent = null )
	{
		_isVisible = true;
		if ( !IsEmbedded && !MountedObject.IsValid() ) Add( parent );
		EnsureViewAttached();
		if ( View != null ) View.Style.Display = DisplayMode.Flex;
	}

	/// <summary>Mark hidden. Standalone: View.Style.Display = None. Embedded: flag only; parent re-renders with inline style.</summary>
	public void Hide()
	{
		_isVisible = false;
		if ( View != null ) View.Style.Display = DisplayMode.None;
	}

	/// <summary>Destroy the mount entirely. A subsequent Show()/Add() spawns a fresh one.</summary>
	public void Remove()
	{
		MountedObject?.Destroy();
		MountedObject = null;
		Host = null;
		View = null;
	}

	/// <summary>
	/// Re-push every wrapper field value to the live <see cref="View"/>. Call
	/// after a batch edit if the auto-sync (every set) is not granular enough
	/// for your use case (rare; most usage just sets one property at a time).
	/// </summary>
	public void RefreshView()
	{
		if ( View != null ) SyncFieldsTo( View );
	}

	/// <summary>
	/// Lazy-create the View Panel and parent it under the Host's root Panel.
	/// Idempotent. Called from Add() / Show() / RefreshView().
	/// </summary>
	private void EnsureViewAttached()
	{
		if ( Host == null || !Host.IsValid() ) return;
		if ( Host.Panel == null ) return;

		if ( View == null )
		{
			// Use the Sandbox.UI `Panel.AddChild<T>()` factory instead of `new T()`
			// because Razor-derived Panel classes only run their generated
			// BuildRenderTree (which populates children from the .razor markup)
			// when constructed through that factory path. Plain `new()` leaves
			// the Panel empty even though it's attached.
			View = Host.Panel.AddChild<TView>();
		}
		else if ( View.Parent != Host.Panel )
		{
			Host.Panel.AddChild( View );
		}

		SyncFieldsTo( View );
	}

	/// <summary>
	/// Generator override: copy each wrapper [Property] into the matching
	/// <see cref="View"/> [Property]. Called on Add() / Show() / RefreshView().
	/// </summary>
	protected abstract void SyncFieldsTo( TView view );

	/// <summary>
	/// Aggregate hash of every field that can affect rendering — own Variables
	/// plus the recursive <see cref="ContentHash"/> of every child wrapper.
	/// The generated wrapper class overrides this. Used by the parent's
	/// Razor <c>BuildHash()</c> so that a mutation at any depth
	/// (<c>grand.parent.hud.Health -= 10</c>) bubbles up and triggers a
	/// re-render at every level — without it, Razor only catches changes to
	/// the direct child's PublicVariables and deeper mutations look invisible.
	/// Base contribution is <see cref="IsShown"/> so Hide/Show on an embedded
	/// wrapper also bubbles up.
	/// </summary>
	public virtual int ContentHash() => _isVisible ? 1 : 0;
}
