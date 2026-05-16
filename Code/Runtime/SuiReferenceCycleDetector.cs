using System;
using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Cycle detection for the <see cref="SuiElementType.SuiReference"/> graph
/// (PRD 19 § 8.1). DFS visited-set walk over outgoing references, starting
/// from a host document. Used by the validator at save time and by the
/// compile pipeline before topologically sorting.
///
/// <para>The detector does NOT load .sui files itself — the caller supplies a
/// resolver delegate that returns the outgoing SourceGuids of a given doc Id.
/// This keeps the runtime layer free of file IO so it composes with both
/// editor (filesystem) and test (in-memory) callers.</para>
/// </summary>
public static class SuiReferenceCycleDetector
{
	/// <summary>
	/// Walks the reference graph starting at <paramref name="rootDocumentId"/>.
	/// Returns the first cycle found as an ordered list of doc Ids
	/// (cycle[0] == cycle[last]), or null if no cycle exists.
	/// </summary>
	/// <param name="rootDocumentId">DocumentId of the host doc to start from.</param>
	/// <param name="outgoingRefs">Returns the SourceGuids referenced by the doc with this Id. Empty when no outgoing refs; null treated as empty.</param>
	public static IReadOnlyList<string> FindCycle( string rootDocumentId, Func<string, IEnumerable<string>> outgoingRefs )
	{
		if ( string.IsNullOrEmpty( rootDocumentId ) || outgoingRefs == null ) return null;

		var stack = new List<string>();
		var onStack = new HashSet<string>();
		var fullyVisited = new HashSet<string>();
		return Visit( rootDocumentId, outgoingRefs, stack, onStack, fullyVisited );
	}

	private static IReadOnlyList<string> Visit(
		string docId,
		Func<string, IEnumerable<string>> outgoingRefs,
		List<string> stack,
		HashSet<string> onStack,
		HashSet<string> fullyVisited )
	{
		if ( onStack.Contains( docId ) )
		{
			// Cycle. Slice the stack from first occurrence onward + close the loop.
			var startIdx = stack.IndexOf( docId );
			var cycle = new List<string>();
			for ( int i = startIdx; i < stack.Count; i++ ) cycle.Add( stack[i] );
			cycle.Add( docId );
			return cycle;
		}

		if ( fullyVisited.Contains( docId ) ) return null;

		stack.Add( docId );
		onStack.Add( docId );

		var children = outgoingRefs( docId );
		if ( children != null )
		{
			foreach ( var childId in children )
			{
				if ( string.IsNullOrEmpty( childId ) ) continue;
				var found = Visit( childId, outgoingRefs, stack, onStack, fullyVisited );
				if ( found != null ) return found;
			}
		}

		stack.RemoveAt( stack.Count - 1 );
		onStack.Remove( docId );
		fullyVisited.Add( docId );
		return null;
	}

	/// <summary>
	/// Compute max reference depth from <paramref name="rootDocumentId"/>. Used by
	/// the validator to warn at depth &gt; 8 and hard-fail at &gt; 16 (PRD 19 § 8.7).
	/// Returns 0 for documents with no outgoing refs.
	/// </summary>
	public static int MaxDepth( string rootDocumentId, Func<string, IEnumerable<string>> outgoingRefs )
	{
		if ( string.IsNullOrEmpty( rootDocumentId ) || outgoingRefs == null ) return 0;
		var memo = new Dictionary<string, int>();
		return Depth( rootDocumentId, outgoingRefs, memo, new HashSet<string>() );
	}

	private static int Depth(
		string docId,
		Func<string, IEnumerable<string>> outgoingRefs,
		Dictionary<string, int> memo,
		HashSet<string> onStack )
	{
		if ( memo.TryGetValue( docId, out var cached ) ) return cached;
		if ( !onStack.Add( docId ) ) return 0; // cycle short-circuit; reported by FindCycle

		var max = 0;
		var children = outgoingRefs( docId );
		if ( children != null )
		{
			foreach ( var childId in children )
			{
				if ( string.IsNullOrEmpty( childId ) ) continue;
				var d = Depth( childId, outgoingRefs, memo, onStack );
				if ( d + 1 > max ) max = d + 1;
			}
		}

		onStack.Remove( docId );
		memo[docId] = max;
		return max;
	}

	/// <summary>
	/// Convenience: enumerate all outgoing SourceGuids from a document's
	/// <see cref="SuiElement.SuiReference"/> blocks. Skips nulls and duplicates.
	/// </summary>
	public static IEnumerable<string> CollectOutgoingRefs( SuiDocument doc )
	{
		if ( doc?.Elements == null ) yield break;
		var seen = new HashSet<string>( StringComparer.Ordinal );
		foreach ( var el in doc.Elements )
		{
			var src = el?.SuiReference?.SourceGuid;
			if ( string.IsNullOrEmpty( src ) ) continue;
			if ( !seen.Add( src ) ) continue;
			yield return src;
		}
	}
}
