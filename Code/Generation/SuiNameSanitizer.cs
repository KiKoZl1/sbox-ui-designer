using System.Linq;
using System.Text;

namespace SboxUiDesigner.Generation;

/// <summary>
/// Name sanitizers for the generator. Three flavours:
///
/// 1. <see cref="ToCssClass"/> — turn an arbitrary string into a CSS class name
///    (lowercase, hyphens for separators, no leading digit, no special chars).
/// 2. <see cref="ToCSharpIdentifier"/> — turn an arbitrary string into a valid
///    C# identifier (PascalCase, ASCII only, leading underscore for digits).
///    Used for the generated PanelComponent class name and namespaces.
/// 3. <see cref="EscapeRazorText"/> — escape text content so it's safe to
///    embed in Razor markup as plain text. Stops `@`, `&lt;`, `&amp;` etc.
///    from being interpreted as Razor directives or HTML.
/// </summary>
public static class SuiNameSanitizer
{
	// ─────────────────────────────────────────────────────────────────────
	//  CSS class
	// ─────────────────────────────────────────────────────────────────────

	public static string ToCssClass( string raw )
	{
		if ( string.IsNullOrEmpty( raw ) ) return "_";
		var sb = new StringBuilder( raw.Length );
		foreach ( var ch in raw )
		{
			if ( char.IsLetterOrDigit( ch ) ) sb.Append( char.ToLowerInvariant( ch ) );
			else if ( ch == '-' || ch == '_' ) sb.Append( '-' );
			else if ( char.IsWhiteSpace( ch ) ) sb.Append( '-' );
		}
		var s = sb.ToString().Trim( '-' );
		if ( s.Length == 0 ) return "_";
		if ( !char.IsLetter( s[0] ) ) s = "x" + s;
		// Collapse runs of '--' into a single '-'.
		while ( s.Contains( "--" ) ) s = s.Replace( "--", "-" );
		return s;
	}

	// ─────────────────────────────────────────────────────────────────────
	//  C# identifier
	// ─────────────────────────────────────────────────────────────────────

	public static string ToCSharpIdentifier( string raw )
	{
		if ( string.IsNullOrEmpty( raw ) ) return "_";

		var sb = new StringBuilder( raw.Length );
		var capitalizeNext = true;
		foreach ( var ch in raw )
		{
			if ( char.IsLetterOrDigit( ch ) )
			{
				sb.Append( capitalizeNext ? char.ToUpperInvariant( ch ) : ch );
				capitalizeNext = false;
			}
			else
			{
				// underscores, spaces, hyphens, etc. all act as word separators
				capitalizeNext = sb.Length > 0;
			}
		}

		var s = sb.ToString();
		if ( s.Length == 0 ) return "_";
		if ( char.IsDigit( s[0] ) ) s = "_" + s;
		// Avoid colliding with C# reserved keywords by prefixing if needed.
		if ( IsCSharpKeyword( s ) ) s = "@" + s;
		return s;
	}

	private static bool IsCSharpKeyword( string s ) => s switch
	{
		"abstract" or "as" or "base" or "bool" or "break" or "byte" or "case"
			or "catch" or "char" or "checked" or "class" or "const" or "continue"
			or "decimal" or "default" or "delegate" or "do" or "double" or "else"
			or "enum" or "event" or "explicit" or "extern" or "false" or "finally"
			or "fixed" or "float" or "for" or "foreach" or "goto" or "if" or "implicit"
			or "in" or "int" or "interface" or "internal" or "is" or "lock" or "long"
			or "namespace" or "new" or "null" or "object" or "operator" or "out"
			or "override" or "params" or "private" or "protected" or "public"
			or "readonly" or "ref" or "return" or "sbyte" or "sealed" or "short"
			or "sizeof" or "stackalloc" or "static" or "string" or "struct" or "switch"
			or "this" or "throw" or "true" or "try" or "typeof" or "uint" or "ulong"
			or "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or "void"
			or "volatile" or "while" => true,
		_ => false,
	};

	// ─────────────────────────────────────────────────────────────────────
	//  Razor / HTML text escape
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Escape arbitrary user text so it's safe to embed in Razor markup as a
	/// plain text node. Replaces `@` (Razor directive) with `&#64;`, `&lt;` /
	/// `&amp;` so HTML parsers don't reinterpret it.
	/// </summary>
	public static string EscapeRazorText( string raw )
	{
		if ( string.IsNullOrEmpty( raw ) ) return "";
		var sb = new StringBuilder( raw.Length + 8 );
		foreach ( var ch in raw )
		{
			switch ( ch )
			{
				case '@': sb.Append( "&#64;" ); break;
				case '<': sb.Append( "&lt;" ); break;
				case '>': sb.Append( "&gt;" ); break;
				case '&': sb.Append( "&amp;" ); break;
				case '"': sb.Append( "&quot;" ); break;
				default: sb.Append( ch ); break;
			}
		}
		return sb.ToString();
	}
}
