using System.Security.Cryptography;
using System.Text;

namespace SboxUiDesigner.Generation;

/// <summary>
/// SHA-256 hash helpers for the manifest/file-ownership system. The manifest
/// stores a hash per generated file and uses it to detect hand-edits before
/// overwriting (see PRD doc 11).
/// </summary>
public static class SuiHashUtility
{
	/// <summary>Lowercase hex sha-256 of an arbitrary string (UTF-8).</summary>
	public static string Sha256( string content )
	{
		if ( content == null ) return Empty;
		var bytes = Encoding.UTF8.GetBytes( content );
		var hash = SHA256.HashData( bytes );
		var sb = new StringBuilder( hash.Length * 2 );
		foreach ( var b in hash ) sb.Append( b.ToString( "x2" ) );
		return sb.ToString();
	}

	/// <summary>Hex sha-256 of an empty payload — same value every time, useful for null-guard fallbacks.</summary>
	public static readonly string Empty = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
}
