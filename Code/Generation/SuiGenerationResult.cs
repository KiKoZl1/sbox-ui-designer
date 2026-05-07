using System.Collections.Generic;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Output of a generation pass. Doesn't touch the filesystem — the pipeline
/// produces the result, and a separate writer step handles I/O + manifest
/// updates so the generator stays pure and testable.
/// </summary>
public sealed class SuiGenerationResult
{
	public List<SuiGeneratedFile> Files { get; } = new();

	public List<string> Errors { get; } = new();
	public List<string> Warnings { get; } = new();

	public bool Ok => Errors.Count == 0;

	public void AddFile( SuiGeneratedFileKind kind, string path, string content )
	{
		Files.Add( new SuiGeneratedFile
		{
			Kind = kind,
			Path = path,
			Content = content,
			Sha256 = SuiHashUtility.Sha256( content ),
		} );
	}

	public SuiGeneratedFile FindByKind( SuiGeneratedFileKind kind )
	{
		foreach ( var f in Files ) if ( f.Kind == kind ) return f;
		return null;
	}
}

public sealed class SuiGeneratedFile
{
	public SuiGeneratedFileKind Kind { get; set; }

	/// <summary>Project-relative output path including filename.</summary>
	public string Path { get; set; }

	public string Content { get; set; }

	public string Sha256 { get; set; }
}
