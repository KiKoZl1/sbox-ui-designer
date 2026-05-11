using System;
using System.IO;
using System.Threading.Tasks;
using Editor;
using Sandbox;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Editor-side glue for the "Test in Play" button. Sequence:
///
///   1. Compile the .sui to the preview cache (Code/_sui_preview/...)
///      — generates a real PanelComponent type via the engine's hot-reload.
///   2. Poll TypeLibrary for the generated type until it's loaded (or timeout).
///   3. Set <see cref="SuiPreviewState.PendingTypeFullName"/> so the preview
///      scene's <see cref="SuiPreviewMount"/> knows what to mount.
///   4. Open the preview stage scene (player + ground + light + Mount).
///   5. <c>EditorScene.Play(session)</c> — engine enters real Play mode.
///
/// No window minimize/restore (sbox editor doesn't expose that API publicly).
/// No Stop polling either — when the user stops play, they're back in the
/// preview stage scene; if they want their previous scene, they re-open it.
/// </summary>
public static class SuiPreviewLauncher
{
	/// <summary>
	/// Addon-relative path to the bundled preview stage scene. The AssetSystem
	/// indexes Library assets under their addon-local path (without the
	/// `Libraries/&lt;addon&gt;/Assets/` prefix), confirmed via engine log:
	///   "On-demand recompile of asset sui_preview/preview_stage.scene"
	/// </summary>
	public const string PreviewStageScenePath = "sui_preview/preview_stage.scene";

	/// <summary>
	/// Fire-and-forget entry point — wires from the toolbar button click.
	/// </summary>
	public static void Launch( SuiDocument document )
	{
		_ = LaunchAsync( document );
	}

	private static async Task LaunchAsync( SuiDocument document )
	{
		if ( document == null )
		{
			Log.Warning( "[SuiPreviewLauncher] No document — open a .sui first." );
			return;
		}

		var session = SceneEditorSession.Active;
		if ( session == null )
		{
			Log.Warning( "[SuiPreviewLauncher] No active SceneEditorSession — can't enter Play." );
			return;
		}

		if ( session.IsPlaying )
		{
			Log.Warning( "[SuiPreviewLauncher] Already in Play. Stop the current play first." );
			return;
		}

		// 1. Compile to preview cache.
		var write = SuiPreviewCacheWriter.Write( document );
		if ( !write.Ok )
		{
			Log.Warning( $"[SuiPreviewLauncher] Compile failed: {string.Join( "; ", write.Errors )}" );
			return;
		}
		Log.Info( $"[SuiPreviewLauncher] Compiled '{write.TypeFullName}' (razorChanged={write.RazorChanged}, scssChanged={write.ScssChanged})" );

		// 2. Wait for hot-reload to make the generated type visible. Poll TypeLibrary.
		// Engine usually picks up new .razor in ~200-800 ms; we cap at 6 s.
		const int maxAttempts = 60;
		const int delayMs = 100;
		Sandbox.TypeDescription typeDesc = null;
		for ( int i = 0; i < maxAttempts; i++ )
		{
			typeDesc = TypeLibrary.GetType( write.TypeFullName );
			if ( typeDesc != null ) break;
			await Task.Delay( delayMs );
		}
		if ( typeDesc == null )
		{
			Log.Warning( $"[SuiPreviewLauncher] Timed out waiting for type '{write.TypeFullName}' to compile (≈{maxAttempts * delayMs / 1000.0}s). The .razor was written; check the engine console for compile errors." );
			return;
		}
		Log.Info( $"[SuiPreviewLauncher] Type loaded: {typeDesc.FullName}" );

		// 3. Set the handoff state.
		SuiPreviewState.Set( write.TypeFullName );

		// 4. Open the preview stage scene in the editor session.
		var stageAsset = AssetSystem.FindByPath( PreviewStageScenePath );
		if ( stageAsset == null )
		{
			Log.Warning( $"[SuiPreviewLauncher] Preview stage not found at '{PreviewStageScenePath}'. Did the addon's Assets/sui_preview/preview_stage.scene get installed?" );
			SuiPreviewState.Clear();
			return;
		}

		try
		{
			stageAsset.OpenInEditor();
			Log.Info( $"[SuiPreviewLauncher] Opened preview stage scene." );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[SuiPreviewLauncher] Failed to open preview stage: {ex.Message}" );
			SuiPreviewState.Clear();
			return;
		}

		// 5. Re-fetch session — opening a new scene may have created a new session.
		session = SceneEditorSession.Active;
		if ( session == null )
		{
			Log.Warning( "[SuiPreviewLauncher] Active session went null after scene load." );
			SuiPreviewState.Clear();
			return;
		}

		EditorScene.Play( session );
		Log.Info( "[SuiPreviewLauncher] EditorScene.Play() invoked. UI should mount in OnAwake of SuiPreviewMount." );
	}
}
