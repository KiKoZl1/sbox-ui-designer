namespace SboxUiDesigner.Runtime;

/// <summary>
/// Process-local handoff between the editor's Test-in-Play launcher and the
/// preview scene's <see cref="SuiPreviewMount"/> component.
///
/// Flow:
///   1. Launcher compiles the .sui to the preview cache (Code/_sui_preview/...)
///   2. Hot-reload makes the generated PanelComponent type available
///   3. Launcher sets <see cref="PendingTypeFullName"/>
///   4. Launcher loads the preview stage scene and triggers Play
///   5. SuiPreviewMount.OnAwake reads the FQN, resolves Type, mounts on a ScreenPanel
///
/// Cleared after the mount succeeds so a stale value can't leak into a later
/// Play that wasn't initiated through the launcher.
/// </summary>
public static class SuiPreviewState
{
	/// <summary>Fully-qualified type name (Namespace.ClassName) of the
	/// generated PanelComponent the preview should mount. Null = nothing
	/// pending (preview scene runs as just player + ground + skybox).</summary>
	public static string PendingTypeFullName { get; private set; }

	public static void Set( string typeFullName )
	{
		PendingTypeFullName = typeFullName;
	}

	public static void Clear()
	{
		PendingTypeFullName = null;
	}
}
