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
	public bool IsMounted => MountedObject.IsValid();

	/// <summary>True when mounted and currently visible.</summary>
	public bool IsShown => MountedObject.IsValid() && MountedObject.Enabled;

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

		MountedObject = scene.CreateObject( false );
		MountedObject.Name = typeof( TView ).Name + "_Mount";
		if ( parent.IsValid() ) MountedObject.SetParent( parent );

		MountedObject.Components.Create<ScreenPanel>();
		Host = MountedObject.Components.Create<SuiHostPanelComponent>();

		// SuiHostPanelComponent.Panel becomes available once OnEnabled fires.
		// MountedObject was created disabled; enabling it (Show) will trigger
		// OnEnabled, at which point Host.Panel exists. So we defer the actual
		// Panel instantiation + parenting to the first Show() / RefreshView().
		// To still let user code touch field mirrors before Show(), we keep
		// the Panel field cached at the wrapper level (View) and lazy-create it.
		EnsureViewAttached();
	}

	/// <summary>Mount-if-needed + visible. The common one-call entry point.</summary>
	public void Show( GameObject parent = null )
	{
		if ( !MountedObject.IsValid() ) Add( parent );
		if ( MountedObject.IsValid() )
		{
			MountedObject.Enabled = true;
			EnsureViewAttached();
		}
	}

	/// <summary>Hide without tearing down — cheaper than Remove + re-Add.</summary>
	public void Hide()
	{
		if ( MountedObject.IsValid() ) MountedObject.Enabled = false;
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
		if ( Host.Panel == null ) return; // OnEnabled hasn't fired yet

		if ( View == null )
		{
			View = new TView();
			Host.Panel.AddChild( View );
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
}
