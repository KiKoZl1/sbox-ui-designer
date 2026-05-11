using System;
using System.Reflection;
using Editor;
using Sandbox;
using Sandbox.UI;
using SboxUiDesigner.Runtime;
using WorldPanel = Sandbox.WorldPanel;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Editor-owned <see cref="Scene"/> that hosts the runtime UI preview rendered
/// inside the designer canvas via <see cref="Editor.SceneRenderingWidget"/>.
///
/// The scene contains:
///  - A <see cref="CameraComponent"/> aimed at the panel
///  - Ambient + directional lights (so any 3D content composited inside is lit)
///  - A "UIHost" GameObject carrying a <see cref="WorldPanel"/> and the
///    currently-loaded PanelComponent (the type generated from the .sui doc;
///    falls back to <see cref="SuiTestPanel"/> until a doc is loaded).
///
/// Critical lifecycle quirk: editor-owned scenes don't dispatch
/// <c>IPreRenderSubscriber</c> callbacks automatically, so neither
/// <see cref="WorldPanel.OnEnabled"/> (which creates the runtime panel) nor
/// <see cref="WorldPanel.OnPreRender"/> (which positions it) ever fire on their
/// own. We manually invoke them via reflection — see <see cref="WireWorldPanelLifecycle"/>
/// and <see cref="WirePanelComponentLifecycle"/>. PanelComponent's
/// <c>OnEnabledInternal</c> is similarly dormant; reflection again.
/// </summary>
public sealed class SuiPreviewHost
{
	public Scene Scene { get; }
	public CameraComponent Camera { get; private set; }

	/// <summary>Logical pixel size of the preview surface (matches WorldPanel.PanelSize).</summary>
	public Vector2 PanelSize { get; } = new( 1920, 1080 );

	private GameObject _uiHost;
	private WorldPanel _worldPanel;
	private Component _panelComponent;          // currently mounted PanelComponent
	private string _panelComponentTypeName;      // for swap detection
	private int _frameCounter;

	private MethodInfo _wpOnEnabled;
	private MethodInfo _wpOnPreRender;

	public SuiPreviewHost()
	{
		Scene = Scene.CreateEditorScene();

		using ( Scene.Push() )
		{
			BuildCamera();
			BuildLights();
			BuildUiHost();
		}

		var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		_wpOnEnabled = typeof( WorldPanel ).GetMethod( "OnEnabled", bf );
		_wpOnPreRender = typeof( WorldPanel ).GetMethod( "OnPreRender", bf );

		WireWorldPanelLifecycle();

		// Mount the static fallback panel on first construction so the canvas
		// shows something before SetPanelType is ever called.
		SetPanelType( typeof( SuiTestPanel ) );

		Log.Info( $"[Sui preview] Scene built. IsEditor={Scene.IsEditor}, SceneWorld={(Scene.SceneWorld != null ? "valid" : "null")}, Camera={(Scene.Camera?.GameObject?.Name ?? "null")}" );
	}

	private void BuildCamera()
	{
		var cameraGo = new GameObject( true, "Camera" );
		Camera = cameraGo.GetOrAddComponent<CameraComponent>( false );
		Camera.BackgroundColor = new Color( 0.08f, 0.08f, 0.10f, 1f );
		Camera.ZFar = 4096;
		Camera.FieldOfView = 60;
		// CameraComponent default = Horizontal FOV (per Facepunch source). Setting
		// FovAxis=Vertical caused the panel to disappear (probably runs the conversion
		// at a moment where Screen aspect isn't valid yet). Stick with the default.
		// CameraComponent default = Horizontal FOV. Visible horizontal at D = D*tan(FOV/2)*2.
		// Pra panel 1920 logical (96 world) preencher horizontalmente: D = 96/1.155 = 83.
		// Em widget 16:9, isso também faz vertical fit (panel = 54 world, vertical visible
		// = 96/aspect = 54). Pra outras aspects o GetPreviewRect aspect-fita o panel.
		Camera.WorldPosition = new Vector3( -85f, 0, 0 );
		Camera.WorldRotation = Rotation.Identity;
		Camera.Enabled = true;
	}

	/// <summary>
	/// Move the camera so the 96×54 world-space panel fits inside the
	/// SceneRenderingWidget at the given aspect. With CameraComponent's
	/// horizontal FOV, the camera-tuned distance D=83 fits exactly at 16:9.
	/// Other aspects need a different distance:
	///
	///   visible_width  at distance D = 2·D·tan(FOV/2)
	///   visible_height = visible_width / widget_aspect
	///
	/// To keep BOTH panel dimensions inside view: take the larger of
	/// (fit-width distance, fit-height distance). Call every frame from the
	/// SceneRenderingWidget host so layout changes update the camera.
	/// </summary>
	public void FitCameraToWidgetSize( Vector2 widgetSize )
	{
		if ( Camera == null || !Camera.IsValid() ) return;
		if ( widgetSize.x < 1 || widgetSize.y < 1 ) return;

		var widgetAspect = widgetSize.x / widgetSize.y;

		const float panelWorldWidth = 96f;
		const float panelWorldHeight = 54f;
		const float tanHalfFovHorizontal = 0.57735f; // tan(60°/2)

		var distanceForWidth = panelWorldWidth / (2f * tanHalfFovHorizontal);            // ≈ 83
		var distanceForHeight = panelWorldHeight * widgetAspect / (2f * tanHalfFovHorizontal); // ≈ 46.75 · aspect

		var distance = System.MathF.Max( distanceForWidth, distanceForHeight );

		var current = Camera.WorldPosition;
		// Skip if effectively unchanged — avoid Update spam in OnPreFrame.
		if ( System.MathF.Abs( -current.x - distance ) < 0.05f ) return;
		Camera.WorldPosition = new Vector3( -distance, current.y, current.z );
	}

	private void BuildLights()
	{
		var ambient = new GameObject( true, "Ambient" ).GetOrAddComponent<AmbientLight>( false );
		ambient.Color = new Color( 0.3f, 0.32f, 0.36f, 1f );
		ambient.Enabled = true;

		var directional = new GameObject( true, "Directional" ).GetOrAddComponent<DirectionalLight>( false );
		directional.WorldRotation = Rotation.From( 45, 45, 0 );
		directional.LightColor = Color.White;
		directional.Enabled = true;
	}

	private void BuildUiHost()
	{
		_uiHost = new GameObject( true, "UIHost" );
		_uiHost.WorldPosition = Vector3.Zero;
		_uiHost.WorldScale = Vector3.One;

		_worldPanel = _uiHost.AddComponent<WorldPanel>();
		_worldPanel.LookAtCamera = true;
		_worldPanel.PanelSize = PanelSize;
		_worldPanel.RenderScale = 1.0f;
		_worldPanel.HorizontalAlign = WorldPanel.HAlignment.Center;
		_worldPanel.VerticalAlign = WorldPanel.VAlignment.Center;

		// SceneRenderingWidget doesn't fire the Game render pass for panels in
		// editor-owned scenes — Overlay + AfterUI are drawn outside the camera's
		// post-process compositor and reliably show up.
		_worldPanel.RenderOptions.Game = true;
		_worldPanel.RenderOptions.Overlay = true;
		_worldPanel.RenderOptions.AfterUI = true;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Public API for the canvas widget
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Swap the currently-mounted PanelComponent for one of the given known C# type.
	/// Used for the static SuiTestPanel fallback. Generated Razor types should go
	/// through <see cref="TrySetPanelTypeByName"/> because their TargetType isn't
	/// always exposed.
	/// </summary>
	public bool SetPanelType( Type panelType )
	{
		if ( panelType == null ) return false;
		var desc = TypeLibrary.GetType( panelType );
		if ( desc == null ) return false;
		return MountByDescriptor( desc );
	}

	/// <summary>
	/// Look up <paramref name="typeFullName"/> in the TypeLibrary and mount it as
	/// the preview's PanelComponent. Returns false if the type isn't loaded yet
	/// (caller should retry — the editor's hotload may still be compiling).
	/// </summary>
	public bool TrySetPanelTypeByName( string typeFullName )
	{
		if ( string.IsNullOrEmpty( typeFullName ) ) return false;
		var desc = TypeLibrary.GetType( typeFullName );
		if ( desc == null )
		{
			// Throttled — only log first miss per pending name (caller polls every frame).
			if ( _lastTypeMissLogged != typeFullName )
			{
				Log.Info( $"[Sui preview] TypeLibrary.GetType('{typeFullName}') returned null — type not loaded yet." );
				_lastTypeMissLogged = typeFullName;
			}
			return false;
		}
		_lastTypeMissLogged = null;
		return MountByDescriptor( desc );
	}

	private string _lastTypeMissLogged;

	/// <summary>The currently-mounted PanelComponent, or null.</summary>
	public Component PanelComponent => _panelComponent;

	// ─────────────────────────────────────────────────────────────────────
	//  Mount / swap mechanics
	// ─────────────────────────────────────────────────────────────────────

	private bool MountByDescriptor( Sandbox.TypeDescription typeDesc )
	{
		var fullName = typeDesc?.FullName ?? "<null>";

		// Already-mounted is treated as success — caller is asking "is this type
		// now mounted?" and the answer is yes. Avoids the polling loop
		// re-firing every frame because we kept returning false.
		if ( _panelComponent != null && _panelComponent.IsValid() && _panelComponentTypeName == fullName )
			return true;

		Log.Info( $"[Sui preview] MountByDescriptor: fullName='{fullName}', current='{_panelComponentTypeName}', current valid={_panelComponent?.IsValid() ?? false}" );

		try
		{
			using ( Scene.Push() )
			{
				// Tear down old component cleanly.
				if ( _panelComponent != null && _panelComponent.IsValid() )
				{
					Log.Info( $"[Sui preview] tearing down old '{_panelComponentTypeName}'" );
					TryInvokeLifecycle( _panelComponent, "OnDisabledInternal" );
					try { _panelComponent.Destroy(); } catch ( Exception destroyEx ) { Log.Warning( $"[Sui preview] destroy old threw: {destroyEx.Message}" ); }
					_panelComponent = null;
				}

				Log.Info( $"[Sui preview] calling Components.Create for '{fullName}'" );
				var created = _uiHost.Components.Create( typeDesc );
				Log.Info( $"[Sui preview] Components.Create returned: {(created == null ? "null" : created.GetType().FullName)}" );
				_panelComponent = created as Component;

				if ( _panelComponent == null )
				{
					Log.Warning( $"[Sui preview] Components.Create returned non-Component or null for {fullName} (raw type: {created?.GetType().FullName ?? "null"})" );
					return false;
				}

				_panelComponentTypeName = fullName;
				WirePanelComponentLifecycle();
				Log.Info( $"[Sui preview] MountByDescriptor: success, mounted '{fullName}'" );
				return true;
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[Sui preview] MountByDescriptor threw for '{fullName}': {ex.GetType().Name}: {ex.Message}" );
			if ( ex.InnerException != null ) Log.Warning( $"[Sui preview]   inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" );
			return false;
		}
	}

	private void WireWorldPanelLifecycle()
	{
		// WorldPanel.OnEnabled creates the runtime Sandbox.UI.WorldPanel that
		// hosts the panel tree — the GetPanel() call returns null until this runs.
		try
		{
			_wpOnEnabled?.Invoke( _worldPanel, null );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[Sui preview] WorldPanel.OnEnabled threw: {ex.InnerException?.Message ?? ex.Message}" );
		}

		// Sandbox.UI.WorldPanel constructor sets Scale = 2.0f, which makes every
		// child render at twice its CSS size (300px shows as 600). Override to 1.0
		// so the user-authored pixel sizes match what the SCSS generator emits and
		// what our hit-test math expects. Note: GetPanel() returns the base Panel —
		// the Scale field lives on RootPanel and we set it via reflection.
		var rootPanel = _worldPanel.GetPanel();
		if ( rootPanel != null )
		{
			try
			{
				var scaleProp = rootPanel.GetType().GetProperty( "Scale",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
				if ( scaleProp != null && scaleProp.CanWrite )
				{
					scaleProp.SetValue( rootPanel, 1.0f );
					Log.Info( "[Sui preview] forced runtime panel Scale=1.0 (override default 2.0)" );
				}
				else
				{
					Log.Warning( "[Sui preview] couldn't find writable Scale property on runtime panel" );
				}
			}
			catch ( Exception ex )
			{
				Log.Warning( $"[Sui preview] setting Scale threw: {ex.Message}" );
			}
		}
	}

	private void WirePanelComponentLifecycle()
	{
		// PanelComponent.OnEnabledInternal -> EnsurePanelCreated + UpdateParent.
		// The latter walks Components and binds to the IRootPanelComponent
		// (our WorldPanel), which is the moment the user's UI goes live.
		TryInvokeLifecycle( _panelComponent, "OnEnabledInternal" );
	}

	private static void TryInvokeLifecycle( Component target, string methodName )
	{
		if ( target == null ) return;
		var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var m = FindMethodInHierarchy( target.GetType(), methodName, bf );
		if ( m == null )
		{
			Log.Warning( $"[Sui preview] {target.GetType().Name}.{methodName} not found via reflection." );
			return;
		}
		try
		{
			m.Invoke( target, null );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[Sui preview] {target.GetType().Name}.{methodName} threw: {ex.InnerException?.Message ?? ex.Message}" );
		}
	}

	private static MethodInfo FindMethodInHierarchy( Type type, string methodName, BindingFlags flags )
	{
		while ( type != null )
		{
			var m = type.GetMethod( methodName, flags | BindingFlags.DeclaredOnly );
			if ( m != null ) return m;
			type = type.BaseType;
		}
		return null;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  Tick — driven by SceneRenderingWidget.OnPreFrame
	// ─────────────────────────────────────────────────────────────────────

	public void Tick()
	{
		if ( Scene == null ) return;
		Scene.GameTick( RealTime.Delta );

		// Re-run WorldPanel.OnPreRender every frame so Transform/PanelBounds
		// reflect the latest WorldPosition/Rotation (LookAtCamera).
		if ( _worldPanel != null && _worldPanel.IsValid() && _wpOnPreRender != null )
		{
			try { _wpOnPreRender.Invoke( _worldPanel, null ); }
			catch { /* swallowed — would flood log on transient errors */ }
		}

		_frameCounter++;
	}

	/// <summary>
	/// Pixel-space → logical-pixel-space mapping. The preview is always laid out
	/// to fill the canvas while preserving the panel's aspect ratio (see
	/// SuiCanvasWidget.LayoutPreviewRect). Returns true if the point falls inside
	/// the panel and outputs the logical pixel position (0..PanelSize).
	/// </summary>
	public bool MapWidgetPixelToPanel( Vector2 widgetPixel, Rect previewRect, out Vector2 logical )
	{
		logical = default;
		if ( previewRect.Width <= 0 || previewRect.Height <= 0 ) return false;

		var local = widgetPixel - previewRect.TopLeft;
		if ( local.x < 0 || local.y < 0 || local.x > previewRect.Width || local.y > previewRect.Height )
			return false;

		logical = new Vector2(
			local.x / previewRect.Width * PanelSize.x,
			local.y / previewRect.Height * PanelSize.y );
		return true;
	}

	/// <summary>
	/// Logical-pixel-space → widget pixel-space. Inverse of MapWidgetPixelToPanel.
	/// </summary>
	public Vector2 MapPanelToWidgetPixel( Vector2 logical, Rect previewRect )
	{
		return new Vector2(
			previewRect.TopLeft.x + logical.x / PanelSize.x * previewRect.Width,
			previewRect.TopLeft.y + logical.y / PanelSize.y * previewRect.Height );
	}
}
