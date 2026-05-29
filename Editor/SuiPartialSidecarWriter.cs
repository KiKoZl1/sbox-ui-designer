using System.IO;
using Sandbox;
using SboxUiDesigner.Generation;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// V1.5 M3 — write the <c>&lt;WrapperName&gt;.partial.cs</c> sidecar once per
/// document (PRD 20 § 3.3, § 4.4). Carries one empty <c>void OnX(...)</c> stub
/// per Code-mode event handler. The user owns the file from then on; subsequent
/// compiles never touch it.
///
/// <para>"Auto-add new stubs on subsequent compile" is a Roslyn polish deferred
/// to V2 (PRD 20 § 5.8) — adding a fresh event handler in the Designer after
/// the partial already exists requires the user to type the new method by hand
/// for now.</para>
/// </summary>
public static class SuiPartialSidecarWriter
{
	/// <summary>
	/// Ensure the sidecar exists. Returns the absolute path written (or already
	/// present) so the caller can record it on <see cref="SuiCompileResult.UserOwned"/>.
	/// Returns null when there are no Code-mode handlers and therefore no
	/// sidecar to write.
	/// </summary>
	public static string EnsureSidecar(
		SuiDocument doc,
		string wrapperNamespace,
		string wrapperClassName,
		string outputFolderAbs )
	{
		if ( doc == null || string.IsNullOrEmpty( outputFolderAbs ) ) return null;
		if ( string.IsNullOrEmpty( wrapperClassName ) ) return null;

		var stub = SuiEventEmitter.EmitPartialSidecarStub( doc, wrapperNamespace ?? "Game.UI", wrapperClassName );
		if ( string.IsNullOrEmpty( stub ) ) return null;

		try
		{
			Directory.CreateDirectory( outputFolderAbs );
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Sui] partial sidecar: failed to ensure output folder '{outputFolderAbs}': {ex.Message}" );
			return null;
		}

		var path = Path.GetFullPath( Path.Combine( outputFolderAbs, wrapperClassName + ".partial.cs" ) );

		// Defense in depth — refuse to escape the output folder via a bogus
		// class name. Mirrors the same check inside SuiCompileWriter.
		if ( !path.StartsWith( Path.GetFullPath( outputFolderAbs ), System.StringComparison.OrdinalIgnoreCase ) )
		{
			Log.Warning( $"[Sui] partial sidecar: refused write outside output folder ('{path}')." );
			return null;
		}

		if ( File.Exists( path ) )
		{
			// User owns it — never overwrite. Caller records it as UserOwned.
			return path;
		}

		try
		{
			File.WriteAllText( path, stub );
			return path;
		}
		catch ( System.Exception ex )
		{
			Log.Warning( $"[Sui] partial sidecar: failed to write '{path}': {ex.Message}" );
			return null;
		}
	}
}
