using System.Collections.Generic;

namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Classification of what happened to each file during a compile run.
/// Produced by <see cref="SuiCompileWriter"/> and consumed by the
/// Compile Results dock to render per-section lists.
///
/// Categories (mutually exclusive per file):
///
/// - <b>Generated</b> — file did not exist on disk, written fresh.
/// - <b>Skipped</b> — file existed, our header confirms ownership, hash unchanged → no-op.
/// - <b>Preserved</b> — file existed, our header confirms ownership, content changed →
///   prior content moved to a timestamped backup folder before overwriting.
/// - <b>Conflict</b> — file existed but doesn't carry our header (or carries another
///   document's header). NOT touched. User must resolve manually.
/// - <b>Obsolete</b> — present in the previous manifest but not in this generation pass.
///   Reported (and optionally cleaned up by the user). We never auto-delete in V1.
/// - <b>UserOwned</b> — sidecar file the user is allowed to edit (e.g. `&lt;name&gt;.User.scss`).
///   Created once if missing; never overwritten on subsequent compiles.
/// </summary>
public sealed class SuiCompileResult
{
	public List<SuiCompileFileEntry> Generated { get; } = new();
	public List<SuiCompileFileEntry> Skipped { get; } = new();
	public List<SuiCompileFileEntry> Preserved { get; } = new();
	public List<SuiCompileFileEntry> Conflicts { get; } = new();
	public List<SuiCompileFileEntry> Obsolete { get; } = new();
	public List<SuiCompileFileEntry> UserOwned { get; } = new();

	public List<string> Errors { get; } = new();
	public List<string> Warnings { get; } = new();

	/// <summary>
	/// True iff Errors is empty AND Conflicts is empty. Conflicts are the
	/// "stuck" state that requires user action; Errors are operational
	/// failures (filesystem, permissions). Either blocks "compile success".
	/// </summary>
	public bool Ok => Errors.Count == 0 && Conflicts.Count == 0;

	/// <summary>Absolute path to the output folder used for this compile.</summary>
	public string OutputFolder { get; set; }

	/// <summary>Absolute path to the backup folder created (if any preservation happened).</summary>
	public string BackupFolder { get; set; }

	public int TotalFilesTouched =>
		Generated.Count + Skipped.Count + Preserved.Count + Conflicts.Count;
}

public sealed class SuiCompileFileEntry
{
	/// <summary>Absolute on-disk path.</summary>
	public string AbsolutePath { get; set; }

	/// <summary>Path relative to the output folder (for display).</summary>
	public string RelativePath { get; set; }

	/// <summary>Sha256 of the new content (post-write).</summary>
	public string Sha256 { get; set; }

	/// <summary>For Preserved entries: where the prior content was backed up.</summary>
	public string BackupPath { get; set; }

	/// <summary>For Conflict entries: human-readable reason.</summary>
	public string ConflictReason { get; set; }
}
