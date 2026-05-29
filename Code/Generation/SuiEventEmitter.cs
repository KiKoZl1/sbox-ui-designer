using System.Collections.Generic;
using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// V1.5 M3 — emits the event-related code fragments for a <see cref="SuiElement"/>
/// that has entries in <see cref="SuiElement.Events"/> (PRD 20 § 3.3 / 3.4).
///
/// <para>Four surfaces produce code per event slot:
/// <list type="bullet">
///   <item>Razor markup attribute (<c>onclick=@OnFireClick</c>) — inserted onto the tag.</item>
///   <item>Wrapper [Property] field (<c>[Property] public Action OnFireClick</c>) — declared on the wrapper class.</item>
///   <item>Renderer @code mirror (<c>public Action OnFireClick { get; set; }</c>) — declared on the renderer Panel.</item>
///   <item>Wrapper SyncFieldsTo assignment (<c>view.OnFireClick = OnFireClick;</c>) — forwards the delegate to the renderer.</item>
/// </list>
/// The dev assigns the handler to the wrapper's Action property from their
/// Component (<c>Hud.OnFireClick = HandleFire;</c>), the same place they read
/// Variables and call Show/Hide. No <c>.partial.cs</c> sidecar is auto-emitted —
/// the dev creates a partial class manually if they prefer that style.
///
/// <para>Doo mode is fully wired in M3 Phase 3 — for now Code mode is exhaustive
/// and Doo entries fall back to no-op emit (the slot exists in the schema but
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

}
