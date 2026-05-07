using System.Collections.Generic;
using System.Text;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Validates a <see cref="SuiDocument"/> against the schema invariants documented
/// in PRD doc 05. Pure logic — no I/O, no editor dependency.
/// </summary>
public static class SuiDocumentValidator
{
	/// <summary>Errors block compile. Warnings are surfaced to the user but don't block.</summary>
	public sealed class Result
	{
		public List<string> Errors { get; } = new();
		public List<string> Warnings { get; } = new();
		public bool IsValid => Errors.Count == 0;
	}

	public static Result Validate( SuiDocument doc )
	{
		var r = new Result();
		if ( doc == null )
		{
			r.Errors.Add( "document is null" );
			return r;
		}

		if ( doc.SchemaVersion < SuiSchemaVersion.MinimumSupported )
			r.Errors.Add( $"schemaVersion {doc.SchemaVersion} is older than minimum supported {SuiSchemaVersion.MinimumSupported}" );

		if ( string.IsNullOrEmpty( doc.DocumentId ) )
			r.Errors.Add( "documentId is missing" );

		if ( string.IsNullOrEmpty( doc.Name ) )
			r.Errors.Add( "name is missing" );

		// Exactly one root.
		int rootCount = 0;
		foreach ( var el in doc.Elements )
		{
			if ( string.IsNullOrEmpty( el.ParentId ) ) rootCount++;
		}
		if ( rootCount == 0 ) r.Errors.Add( "no root element (expected exactly one element with null parentId)" );
		if ( rootCount > 1 ) r.Errors.Add( $"multiple root elements ({rootCount}) — expected exactly one" );

		// No duplicate ids.
		var seen = new HashSet<string>();
		foreach ( var el in doc.Elements )
		{
			if ( string.IsNullOrEmpty( el.Id ) )
			{
				r.Errors.Add( $"element with name '{el.Name}' has no id" );
				continue;
			}
			if ( !seen.Add( el.Id ) )
				r.Errors.Add( $"duplicate element id: {el.Id}" );
		}

		// Build a lookup for parent/child checks.
		var byId = new Dictionary<string, SuiElement>();
		foreach ( var el in doc.Elements )
		{
			if ( !string.IsNullOrEmpty( el.Id ) )
				byId[el.Id] = el;
		}

		// Every parentId must resolve to an existing element (or be null for root).
		foreach ( var el in doc.Elements )
		{
			if ( string.IsNullOrEmpty( el.ParentId ) ) continue;
			if ( !byId.ContainsKey( el.ParentId ) )
				r.Errors.Add( $"element '{el.Id}' references unknown parentId '{el.ParentId}'" );
		}

		// Every children id must resolve and must agree with the child's ParentId.
		foreach ( var el in doc.Elements )
		{
			foreach ( var childId in el.Children )
			{
				if ( !byId.TryGetValue( childId, out var child ) )
				{
					r.Errors.Add( $"element '{el.Id}' lists child '{childId}' which does not exist" );
					continue;
				}
				if ( child.ParentId != el.Id )
					r.Errors.Add( $"element '{el.Id}' lists child '{childId}' but child.parentId is '{child.ParentId}'" );
			}
		}

		// No hierarchy cycles.
		foreach ( var el in doc.Elements )
		{
			if ( HasCycle( el, byId ) )
				r.Errors.Add( $"hierarchy cycle detected starting at element '{el.Id}'" );
		}

		// Per-element style/layout sanity.
		foreach ( var el in doc.Elements )
		{
			if ( el.Style != null )
			{
				if ( el.Style.Opacity < 0f || el.Style.Opacity > 1f )
					r.Errors.Add( $"element '{el.Id}' opacity={el.Style.Opacity} out of range [0..1]" );
			}
			if ( el.Layout != null )
			{
				if ( el.Layout.Width < 0f || el.Layout.Height < 0f )
					r.Errors.Add( $"element '{el.Id}' has negative width/height" );
			}
			if ( el.Props != null )
			{
				if ( el.Props.FontSize <= 0f && el.Type == SuiElementType.Text )
					r.Errors.Add( $"text element '{el.Id}' has fontSize <= 0" );
				if ( (el.Type == SuiElementType.Grid || el.Type == SuiElementType.InventoryGrid)
					&& (el.Props.Columns < 1 || el.Props.Rows < 1) )
					r.Errors.Add( $"grid element '{el.Id}' has columns/rows < 1" );
			}
		}

		// Output validity (warnings only when not configured).
		if ( doc.Output != null && doc.Output.Configured )
		{
			if ( string.IsNullOrEmpty( doc.Output.RootFolder ) )
				r.Errors.Add( "output is configured but rootFolder is empty" );
			if ( string.IsNullOrEmpty( doc.Output.ClassName ) )
				r.Errors.Add( "output is configured but className is empty" );
		}

		return r;
	}

	private static bool HasCycle( SuiElement start, Dictionary<string, SuiElement> byId )
	{
		var visited = new HashSet<string>();
		var current = start;
		while ( current != null && !string.IsNullOrEmpty( current.ParentId ) )
		{
			if ( !visited.Add( current.Id ) ) return true;
			if ( !byId.TryGetValue( current.ParentId, out var parent ) ) return false;
			current = parent;
		}
		return false;
	}

	// ---------- Sanitizers ----------

	/// <summary>
	/// Convert an arbitrary string into a CSS-safe class name.
	/// Lowercases, replaces invalid chars with hyphens, strips leading non-alpha.
	/// </summary>
	public static string SanitizeClassName( string raw )
	{
		if ( string.IsNullOrEmpty( raw ) ) return "_";
		var sb = new StringBuilder( raw.Length );
		foreach ( var ch in raw )
		{
			if ( char.IsLetterOrDigit( ch ) ) sb.Append( char.ToLowerInvariant( ch ) );
			else if ( ch == '-' || ch == '_' ) sb.Append( '-' );
			else if ( char.IsWhiteSpace( ch ) ) sb.Append( '-' );
			// drop everything else
		}
		var s = sb.ToString().Trim( '-' );
		// Ensure leading char is a letter (CSS allows starting with letter/underscore;
		// digits at the start would require escaping).
		if ( s.Length == 0 ) return "_";
		if ( !char.IsLetter( s[0] ) ) s = "x" + s;
		return s;
	}

	/// <summary>
	/// Convert an arbitrary string into a slug suitable for embedding in a documentId
	/// or filename — lowercase, alphanumerics + underscores only, max 24 chars.
	/// </summary>
	public static string SanitizeIdentifierSlug( string raw )
	{
		if ( string.IsNullOrEmpty( raw ) ) return "";
		var sb = new StringBuilder( raw.Length );
		foreach ( var ch in raw )
		{
			if ( char.IsLetterOrDigit( ch ) ) sb.Append( char.ToLowerInvariant( ch ) );
			else if ( sb.Length > 0 && sb[sb.Length - 1] != '_' ) sb.Append( '_' );
		}
		var s = sb.ToString().Trim( '_' );
		if ( s.Length > 24 ) s = s.Substring( 0, 24 ).TrimEnd( '_' );
		return s;
	}
}
