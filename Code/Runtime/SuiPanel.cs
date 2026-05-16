using Sandbox;
using Sandbox.UI;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Base for the user-facing wrapper class the Instance-mode generator emits
/// for each <c>.sui</c> (PRD 19 / 22 V1.5 revised). The dev declares a field
/// on their own Component (<c>[Property] public WdgSelectJob Widget = new();</c>)
/// and controls the panel's lifetime from gameplay code:
///
/// <code>
///   Widget.Add();      // mount (hidden) — creates child GO + ScreenPanel + Panel Component
///   Widget.Show();     // mount-if-needed + visible
///   Widget.Hide();     // hide (keeps the mount around)
///   Widget.Remove();   // tear down the mount entirely
///   Widget.Health = 75; // edit any [Property] mirror; call RefreshView() to push
/// </code>
///
/// <para><see cref="TView"/> is the generated <c>PanelComponent</c> that
/// actually renders the markup — internal-ish, the dev rarely names it.</para>
///
/// <para>V1.5 ships single-mount semantics: one Add() per instance. The
/// per-Connection variant (UMG/UEFN-style <c>UIMap[player]</c>) is V1.6
/// polish on top of this class via a manager wrapper.</para>
/// </summary>
public abstract class SuiPanel<TView> where TView : PanelComponent, new()
{
	/// <summary>The mounted host GameObject; null until <see cref="Add"/> runs.</summary>
	protected GameObject MountedObject { get; private set; }

	/// <summary>The live PanelComponent doing the rendering; null until mounted.</summary>
	protected TView View { get; private set; }

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
		View = MountedObject.Components.Create<TView>();

		SyncFieldsTo( View );
	}

	/// <summary>Mount-if-needed + visible. The common one-call entry point.</summary>
	public void Show( GameObject parent = null )
	{
		if ( !MountedObject.IsValid() ) Add( parent );
		if ( MountedObject.IsValid() ) MountedObject.Enabled = true;
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
		View = null;
	}

	/// <summary>
	/// Re-push every wrapper field value to the live <see cref="View"/>. Call
	/// after a batch edit if the auto-sync (every set) is not granular enough
	/// for your use case (rare; most usage just sets one property at a time).
	/// </summary>
	public void RefreshView()
	{
		if ( View.IsValid() ) SyncFieldsTo( View );
	}

	/// <summary>
	/// Generator override: copy each wrapper [Property] into the matching
	/// <see cref="View"/> [Property]. Called on Add() and on RefreshView().
	/// </summary>
	protected abstract void SyncFieldsTo( TView view );
}
