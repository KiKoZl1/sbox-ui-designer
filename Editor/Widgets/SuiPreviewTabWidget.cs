using System;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Widgets;

/// <summary>
/// Embedded preview widget — hosts a <see cref="SuiPreviewHost"/> + Editor's
/// <see cref="SceneRenderingWidget"/> inside the Designer center area as a
/// "Preview" tab. Useful for quick design-loop validation without leaving the
/// editor (vs. "Test in Play" which switches to a real Play-mode stage scene).
///
/// Lifecycle is editor-owned (no Game.IsPlaying), so PanelComponent lifecycle
/// is invoked via reflection by <c>SuiPreviewHost</c>. See <c>SuiPreviewHost</c>
/// for the workaround details.
/// </summary>
public sealed class SuiPreviewTabWidget : Widget
{
	private SuiDocument _document;
	private SuiPreviewHost _host;
	private SceneRenderingWidget _sceneWidget;
	private Label _statusLabel;
	private string _lastMountedTypeFqn;

	public SuiPreviewTabWidget( Widget parent ) : base( parent )
	{
		Layout = Layout.Column();
		Layout.Margin = 0;
		Layout.Spacing = 0;

		// Top bar: Refresh button + status label.
		var topBar = new Widget( this );
		topBar.Layout = Layout.Row();
		topBar.Layout.Margin = new Sandbox.UI.Margin( 6, 4, 6, 4 );
		topBar.Layout.Spacing = 6;
		topBar.FixedHeight = 32;

		var refreshBtn = new Button( "Refresh", "refresh", topBar );
		refreshBtn.ToolTip = "Re-run the generator and remount the panel";
		refreshBtn.Clicked += Reload;
		topBar.Layout.Add( refreshBtn );

		_statusLabel = new Label( "(no document)", topBar );
		_statusLabel.SetStyles( "color: #9ca3af; font-size: 11px;" );
		topBar.Layout.Add( _statusLabel, 1 );
		Layout.Add( topBar );

		// SceneRenderingWidget hosts our editor-owned preview scene.
		_host = new SuiPreviewHost();
		_sceneWidget = new SceneRenderingWidget( this );
		_sceneWidget.Scene = _host.Scene;
		_sceneWidget.Camera = _host.Camera;
		_sceneWidget.OnPreFrame += () =>
		{
			// Match camera distance to the widget's current aspect so the panel
			// fits inside the viewport regardless of how tall/wide the editor
			// docked us. Without this, the camera is fixed at the 16:9 sweet
			// spot and any other aspect crops the panel.
			if ( _sceneWidget.IsValid() )
				_host.FitCameraToWidgetSize( _sceneWidget.Size );
			_host.Tick();
		};
		Layout.Add( _sceneWidget, 1 );
	}

	public void SetDocument( SuiDocument document )
	{
		_document = document;
		// Don't auto-Reload — runs the full generation pipeline. Center tab
		// activation triggers it lazily.
	}

	/// <summary>
	/// Re-write the preview cache and (re)mount the resulting type. Idempotent
	/// when the document hasn't changed (cache writer hashes content first).
	/// </summary>
	public void Reload()
	{
		if ( _document == null )
		{
			SetStatus( "(no document loaded)" );
			return;
		}

		var write = SuiPreviewCacheWriter.Write( _document );
		if ( !write.Ok )
		{
			SetStatus( $"compile failed: {string.Join( "; ", write.Errors )}" );
			return;
		}

		// Hot-reload may not have produced the type yet; the host polls
		// TypeLibrary internally on each frame via its mount path.
		var ok = _host.TrySetPanelTypeByName( write.TypeFullName );
		if ( ok )
		{
			_lastMountedTypeFqn = write.TypeFullName;
			SetStatus( $"mounted {write.TypeFullName}" );
		}
		else
		{
			SetStatus( $"waiting for hot-reload of {write.TypeFullName}…" );
			// One-shot retry on next tick — gives the engine a frame to register the type.
			_ = RetryMountAsync( write.TypeFullName );
		}
	}

	private async System.Threading.Tasks.Task RetryMountAsync( string fqn )
	{
		for ( int i = 0; i < 30; i++ )
		{
			await System.Threading.Tasks.Task.Delay( 100 );
			if ( !this.IsValid() ) return;
			if ( _host.TrySetPanelTypeByName( fqn ) )
			{
				_lastMountedTypeFqn = fqn;
				SetStatus( $"mounted {fqn}" );
				return;
			}
		}
		SetStatus( $"timed out waiting for {fqn}" );
	}

	private void SetStatus( string text )
	{
		if ( _statusLabel.IsValid() ) _statusLabel.Text = text;
	}
}
