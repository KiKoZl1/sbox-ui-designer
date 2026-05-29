using System.Collections.Generic;
using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// V1.5 M3 — emits the event-related code fragments for a <see cref="SuiElement"/>
/// that has entries in <see cref="SuiElement.Events"/> (PRD 20 § 3.3 / 3.4).
///
/// <para>Three surfaces produce code per event slot:
/// <list type="bullet">
///   <item>Razor markup attribute (<c>onclick=@OnFireClick</c>) — inserted onto the tag.</item>
///   <item>Wrapper [Property] field (<c>[Property] public Action OnFireClick</c>) — declared on the wrapper class.</item>
///   <item>Renderer @code mirror (<c>public Action OnFireClick { get; set; }</c>) — declared on the renderer Panel.</item>
///   <item>Wrapper SyncFieldsTo assignment (<c>view.OnFireClick = OnFireClick;</c>) — forwards the delegate to the renderer.</item>
///   <item>Partial sidecar stub (<c>void OnFireClick() { /* TODO */ }</c>) — written once by SuiPartialSidecarWriter.</item>
/// </list>
/// Doo mode is fully wired in M3 Phase 3 — for now Code mode is exhaustive and
/// Doo entries fall back to no-op emit (the slot exists in the schema but
/// produces nothing in the generated code). The fallback keeps Phase 1 safe to
/// ship without the Doo runtime resolved.</para>
/// </summary>
public static class SuiEventEmitter
{
	/// <summary>
	/// Append all event attributes for this element onto the open tag (a space
	/// before each name). Skips Doo entries until Phase 3 wires the runtime path.
	/// </summary>
	public static void EmitRazorAttributes( SuiElement el, StringBuilder sb )
	{
		if ( el?.Events == null || el.Events.Count == 0 ) return;

		foreach ( var kv in el.Events )
		{
			var binding = kv.Value;
			if ( binding == null ) continue;
			if ( !SuiEventMatrix.TryGet( el.Type, kv.Key, out var entry ) ) continue;

			switch ( binding.Mode )
			{
				case SuiEventMode.Code:
					if ( string.IsNullOrEmpty( binding.Handler ) ) continue;
					sb.Append( ' ' ).Append( entry.RazorAttribute ).Append( "=@" ).Append( binding.Handler );
					break;
				case SuiEventMode.Doo:
					// Phase 3 will emit something like:
					//   onclick=@(() => RunDoo(OnFireClick))
					// or via a renderer-side trigger Action that the wrapper
					// assigns to a lambda calling Component.RunDoo. Skip in
					// Phase 1 so the schema can already round-trip Doo entries.
					break;
			}
		}
	}

	/// <summary>
	/// Append wrapper class field declarations for every Code-mode event slot.
	/// Each becomes a <c>[Property, Group("Events")] public Action OnX { get; set; }</c>
	/// (or <c>Action&lt;T&gt;</c> when the matrix entry has a typed delegate).
	/// </summary>
	public static void EmitWrapperProperties( SuiDocument doc, StringBuilder sb )
	{
		if ( doc?.Elements == null ) return;

		foreach ( var el in doc.Elements )
		{
			if ( el?.Events == null || el.Events.Count == 0 ) continue;
			foreach ( var kv in el.Events )
			{
				var binding = kv.Value;
				if ( binding == null || binding.Mode != SuiEventMode.Code ) continue;
				if ( string.IsNullOrEmpty( binding.Handler ) ) continue;
				if ( !SuiEventMatrix.TryGet( el.Type, kv.Key, out var entry ) ) continue;

				var delegateType = string.IsNullOrEmpty( entry.CodeDelegate ) ? "Action" : entry.CodeDelegate;
				sb.Append( "\t[Property, Group( \"Events\" )] public " )
					.Append( delegateType ).Append( ' ' ).Append( binding.Handler )
					.AppendLine( " { get; set; }" );
			}
		}
	}

	/// <summary>
	/// Append SyncFieldsTo assignments forwarding each Code-mode event slot to
	/// the renderer Panel (which has a matching mirror field — see
	/// <see cref="EmitRendererFields"/>).
	/// </summary>
	public static void EmitWrapperSyncAssignments( SuiDocument doc, StringBuilder sb, string viewVar )
	{
		if ( doc?.Elements == null ) return;

		foreach ( var el in doc.Elements )
		{
			if ( el?.Events == null || el.Events.Count == 0 ) continue;
			foreach ( var kv in el.Events )
			{
				var binding = kv.Value;
				if ( binding == null || binding.Mode != SuiEventMode.Code ) continue;
				if ( string.IsNullOrEmpty( binding.Handler ) ) continue;

				sb.Append( "\t\t" ).Append( viewVar ).Append( '.' ).Append( binding.Handler )
					.Append( " = " ).Append( binding.Handler ).AppendLine( ";" );
			}
		}
	}

	/// <summary>
	/// Append renderer-side mirror field declarations matching each Code-mode
	/// event slot. The renderer doesn't take <c>[Property]</c> on these (the
	/// wrapper assigns through SyncFieldsTo) but it does need a public setter
	/// so the assignment compiles.
	/// </summary>
	public static void EmitRendererFields( SuiDocument doc, StringBuilder sb )
	{
		if ( doc?.Elements == null ) return;

		foreach ( var el in doc.Elements )
		{
			if ( el?.Events == null || el.Events.Count == 0 ) continue;
			foreach ( var kv in el.Events )
			{
				var binding = kv.Value;
				if ( binding == null || binding.Mode != SuiEventMode.Code ) continue;
				if ( string.IsNullOrEmpty( binding.Handler ) ) continue;
				if ( !SuiEventMatrix.TryGet( el.Type, kv.Key, out var entry ) ) continue;

				var delegateType = string.IsNullOrEmpty( entry.CodeDelegate ) ? "Action" : entry.CodeDelegate;
				sb.Append( "\tpublic " ).Append( delegateType ).Append( ' ' ).Append( binding.Handler )
					.AppendLine( " { get; set; }" );
			}
		}
	}

	/// <summary>
	/// Build the body of the <c>&lt;Name&gt;.partial.cs</c> sidecar — one empty
	/// method per Code-mode event handler. Written once by
	/// <c>SuiPartialSidecarWriter</c>; subsequent compiles never touch it.
	/// </summary>
	public static string EmitPartialSidecarStub( SuiDocument doc, string wrapperNamespace, string wrapperClassName )
	{
		if ( doc?.Elements == null ) return null;

		var bodies = new StringBuilder();
		var handlersSeen = new HashSet<string>();

		foreach ( var el in doc.Elements )
		{
			if ( el?.Events == null || el.Events.Count == 0 ) continue;
			foreach ( var kv in el.Events )
			{
				var binding = kv.Value;
				if ( binding == null || binding.Mode != SuiEventMode.Code ) continue;
				if ( string.IsNullOrEmpty( binding.Handler ) ) continue;
				if ( !handlersSeen.Add( binding.Handler ) ) continue;
				if ( !SuiEventMatrix.TryGet( el.Type, kv.Key, out var entry ) ) continue;

				var param = string.IsNullOrEmpty( entry.CodeDelegate )
					? ""
					: ExtractGenericArg( entry.CodeDelegate ) + " " + ( string.IsNullOrEmpty( entry.ParameterName ) ? "arg" : entry.ParameterName );

				bodies.Append( "\tvoid " ).Append( binding.Handler ).Append( "( " ).Append( param ).AppendLine( " )" );
				bodies.AppendLine( "\t{" );
				bodies.AppendLine( "\t\t// TODO: your code here" );
				bodies.AppendLine( "\t}" );
				bodies.AppendLine();
			}
		}

		// No Code-mode handlers → no sidecar to write. Caller checks for null.
		if ( bodies.Length == 0 ) return null;

		var sb = new StringBuilder();
		sb.AppendLine( "// User-owned partial — created once on first compile, never overwritten." );
		sb.AppendLine( "// Add your event handlers here. New events added in the Designer after the" );
		sb.AppendLine( "// initial compile won't auto-append stubs (V2 Roslyn polish); add them by hand." );
		sb.AppendLine();
		sb.AppendLine( "using Sandbox;" );
		sb.AppendLine();
		sb.Append( "namespace " ).Append( wrapperNamespace ).AppendLine( ";" );
		sb.AppendLine();
		sb.Append( "public partial class " ).AppendLine( wrapperClassName );
		sb.AppendLine( "{" );
		sb.Append( bodies );
		sb.AppendLine( "}" );
		return sb.ToString();
	}

	/// <summary>
	/// "Action&lt;float&gt;" → "float". Returns empty for plain "Action".
	/// Used only for the partial-stub parameter-name generation.
	/// </summary>
	private static string ExtractGenericArg( string delegateType )
	{
		if ( string.IsNullOrEmpty( delegateType ) ) return "";
		var lt = delegateType.IndexOf( '<' );
		var gt = delegateType.LastIndexOf( '>' );
		if ( lt < 0 || gt < 0 || gt <= lt + 1 ) return "";
		return delegateType.Substring( lt + 1, gt - lt - 1 );
	}
}
