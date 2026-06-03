using System;
using System.IO;
using System.Text.Json;
using Sandbox;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Persistent editor-local state for the SUI Designer — lives at
/// <c>&lt;projectRoot&gt;/.sui-designer-state.json</c>. Tracks dismissal of the
/// "old schema detected — migrate?" upgrade pop-up so it only nags users
/// once per schema version per project.
///
/// <para>Format is small and human-readable. Future flags (e.g. window layout
/// memory, last-opened-doc) can land in the same file without versioning ceremony —
/// keep one POCO, add fields with safe defaults.</para>
/// </summary>
public sealed class SuiDesignerState
{
	private const string FileName = ".sui-designer-state.json";

	/// <summary>
	/// The schema version that was current the last time this project's
	/// designer state was written. Used to detect "we upgraded since you
	/// last looked" vs "you're already current."
	/// </summary>
	public int LastSeenSchemaVersion { get; set; } = 0;

	/// <summary>
	/// If equal to <see cref="Runtime.SuiSchemaVersion.Current"/>, the user
	/// already saw + dismissed the upgrade prompt for this schema. Stop
	/// nagging until a future schema bump.
	/// </summary>
	public int DismissedUpgradePromptForVersion { get; set; } = 0;

	/// <summary>
	/// True once the user successfully completed a Force Regen pass. We
	/// don't gate behaviour on this — it's diagnostic for support / future
	/// "are you on a clean upgrade?" health checks.
	/// </summary>
	public bool UpgradeRegenCompletedAtLeastOnce { get; set; } = false;

	/// <summary>UTC timestamp of the last successful Force Regen, or null if never run.</summary>
	public string LastForceRegenUtc { get; set; }

	// ─────────────────────────────────────────────────────────────────────
	//  Load / Save
	// ─────────────────────────────────────────────────────────────────────

	private static string PathFor( string projectRoot )
	{
		if ( string.IsNullOrEmpty( projectRoot ) ) return null;
		return Path.Combine( projectRoot, FileName );
	}

	private static string ResolveProjectRoot()
		=> Sandbox.Project.Current?.RootDirectory?.FullName;

	/// <summary>
	/// Load the state file from the current project. Returns a fresh
	/// default-valued instance when the file is missing or corrupt — never
	/// throws. Corrupt files are silently rebuilt next time someone calls
	/// <see cref="Save"/>.
	/// </summary>
	public static SuiDesignerState Load()
	{
		var root = ResolveProjectRoot();
		var path = PathFor( root );
		if ( string.IsNullOrEmpty( path ) || !File.Exists( path ) )
			return new SuiDesignerState();

		try
		{
			var json = File.ReadAllText( path );
			var state = JsonSerializer.Deserialize<SuiDesignerState>( json );
			return state ?? new SuiDesignerState();
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[Sui] designer-state load failed ({ex.Message}) — resetting." );
			return new SuiDesignerState();
		}
	}

	/// <summary>Atomically persist this state instance.</summary>
	public void Save()
	{
		var root = ResolveProjectRoot();
		var path = PathFor( root );
		if ( string.IsNullOrEmpty( path ) ) return;

		try
		{
			var json = JsonSerializer.Serialize( this, new JsonSerializerOptions { WriteIndented = true } );
			var tmp = path + ".tmp";
			File.WriteAllText( tmp, json );
			if ( File.Exists( path ) )
			{
				try { File.Replace( tmp, path, null ); }
				catch
				{
					File.Copy( tmp, path, overwrite: true );
					try { File.Delete( tmp ); } catch { }
				}
			}
			else
			{
				File.Move( tmp, path );
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[Sui] designer-state save failed: {ex.Message}" );
		}
	}
}
