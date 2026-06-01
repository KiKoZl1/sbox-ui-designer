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

			var slotName = GetSlotName( binding );
			if ( string.IsNullOrEmpty( slotName ) ) continue;
			// Markup is mode-independent — the renderer always exposes an
			// `Action <SlotName>` property. The wrapper's SyncFieldsTo is
			// what differs: Code assigns a method reference, Doo assigns
			// `() => Host?.RunDoo(<SlotName>)` lambda.
			sb.Append( ' ' ).Append( entry.RazorAttribute ).Append( "=@" ).Append( slotName );
		}
	}

	/// <summary>
	/// Append wrapper class field declarations for every event slot. Code mode
	/// emits <c>[Property, Group("Events")] public Action OnX</c>; Doo mode
	/// emits <c>[Property, Group("Events")] public Doo OnX</c> with optional
	/// <c>Doo.ArgumentHint&lt;T&gt;("name")</c> attribute when the matrix entry
	/// has a typed delegate (PRD 20 § 3.4).
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
				if ( binding == null ) continue;
				var slotName = GetSlotName( binding );
				if ( string.IsNullOrEmpty( slotName ) ) continue;
				if ( !SuiEventMatrix.TryGet( el.Type, kv.Key, out var entry ) ) continue;

				switch ( binding.Mode )
				{
					case SuiEventMode.Code:
						var delegateType = string.IsNullOrEmpty( entry.CodeDelegate ) ? "Action" : entry.CodeDelegate;
						sb.Append( "\t[Property, Group( \"Events\" )] public " )
							.Append( delegateType ).Append( ' ' ).Append( slotName )
							.AppendLine( " { get; set; }" );
						break;

					case SuiEventMode.Doo:
						// WBP-like default: the .sui authors the Doo body; codegen
						// embeds the serialised JSON so 1 .sui = 1 Doo default
						// per slot, shared across every widget instance.
						//
						// We CANNOT use a plain `{ get; set; } = Json.Deserialize(...)`
						// initialiser — the engine serialises [Property] values
						// into the scene/prefab JSON on save, and an instance
						// that never had the slot picker touched still gets
						// written as `"OnFoo": null` (the serializer doesn't
						// know about field initialisers). On reload the engine
						// calls the setter with null AFTER the initialiser ran,
						// destroying the default. So we use a backing field
						// + defensive setter: get/set on null falls back to a
						// lazily-parsed static default. The picker still wins
						// — picking a new (non-null) Doo replaces the default.
						var hasDefault = binding.DooBody != null;
						var defaultField = "_" + char.ToLowerInvariant( slotName[0] ) + slotName.Substring( 1 ) + "Default";
						var backingField = "_" + char.ToLowerInvariant( slotName[0] ) + slotName.Substring( 1 );

						if ( hasDefault )
						{
							var json = Sandbox.Json.Serialize( binding.DooBody );
							var escaped = (json ?? "{}").Replace( "\"", "\"\"" );

							sb.Append( "\tprivate const string " ).Append( defaultField )
								.Append( "Json = @\"" ).Append( escaped ).AppendLine( "\";" );
							sb.Append( "\tprivate static global::Sandbox.Doo " ).Append( defaultField ).AppendLine( ";" );
							sb.Append( "\tprivate static global::Sandbox.Doo " ).Append( slotName )
								.Append( "DefaultFactory() => " ).Append( defaultField )
								.Append( " ??= global::Sandbox.Json.Deserialize<global::Sandbox.Doo>( " )
								.Append( defaultField ).AppendLine( "Json );" );
							sb.Append( "\tprivate global::Sandbox.Doo " ).Append( backingField ).AppendLine( ";" );
						}

						sb.Append( "\t[Property, Group( \"Events\" )" );
						if ( !string.IsNullOrEmpty( entry.CodeDelegate ) )
						{
							var argType = ExtractGenericArg( entry.CodeDelegate );
							var argName = string.IsNullOrEmpty( entry.ParameterName ) ? "value" : entry.ParameterName;
							if ( !string.IsNullOrEmpty( argType ) )
							{
								sb.Append( ", global::Sandbox.Doo.ArgumentHint<" ).Append( argType ).Append( ">( \"" )
									.Append( argName ).Append( "\" )" );
							}
						}
						sb.Append( " ] public global::Sandbox.Doo " ).Append( slotName );

						if ( hasDefault )
						{
							sb.AppendLine();
							sb.AppendLine( "\t{" );
							sb.Append( "\t\tget => " ).Append( backingField ).Append( " ??= " )
								.Append( slotName ).AppendLine( "DefaultFactory();" );
							sb.Append( "\t\tset => " ).Append( backingField ).Append( " = value ?? " )
								.Append( slotName ).AppendLine( "DefaultFactory();" );
							sb.AppendLine( "\t}" );
						}
						else
						{
							sb.AppendLine( " { get; set; }" );
						}
						break;
				}
			}
		}
	}

	/// <summary>
	/// Append SyncFieldsTo assignments forwarding each event slot to the
	/// renderer Panel. Code mode forwards the Action reference directly;
	/// Doo mode wraps the Doo invocation in a lambda so the renderer can
	/// fire it without dragging in <c>Component.RunDoo</c> — the wrapper's
	/// host Component owns that surface.
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
				if ( binding == null ) continue;
				var slotName = GetSlotName( binding );
				if ( string.IsNullOrEmpty( slotName ) ) continue;
				if ( !SuiEventMatrix.TryGet( el.Type, kv.Key, out var entry ) ) continue;

				switch ( binding.Mode )
				{
					case SuiEventMode.Code:
						sb.Append( "\t\t" ).Append( viewVar ).Append( '.' ).Append( slotName )
							.Append( " = " ).Append( slotName ).AppendLine( ";" );
						break;

					case SuiEventMode.Doo:
						// Bridge: renderer holds an Action, wrapper assigns a
						// lambda that calls Host.RunDoo with the slot's Doo
						// instance. Host is the SuiHostPanelComponent — a
						// Sandbox.Component — so RunDoo is inherited.
						if ( string.IsNullOrEmpty( entry.CodeDelegate ) )
						{
							sb.Append( "\t\t" ).Append( viewVar ).Append( '.' ).Append( slotName )
								.Append( " = () => Host?.RunDoo( " ).Append( slotName ).AppendLine( " );" );
						}
						else
						{
							var argType = ExtractGenericArg( entry.CodeDelegate );
							var argName = string.IsNullOrEmpty( entry.ParameterName ) ? "value" : entry.ParameterName;
							sb.Append( "\t\t" ).Append( viewVar ).Append( '.' ).Append( slotName )
								.Append( " = ( " ).Append( argType ).Append( ' ' ).Append( argName )
								.Append( " ) => Host?.RunDoo( " ).Append( slotName )
								.Append( ", c => c.SetArgument( \"" ).Append( argName ).Append( "\", " ).Append( argName )
								.AppendLine( " ) );" );
						}
						break;
				}
			}
		}
	}

	/// <summary>
	/// Append renderer-side mirror field declarations matching each event slot.
	/// Same shape for Code and Doo — the renderer always exposes an
	/// <c>Action</c> (typed when the matrix entry requires it) that the
	/// markup invokes via <c>onclick=@OnX</c>. The wrapper's SyncFieldsTo
	/// picks which side fills the Action.
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
				if ( binding == null ) continue;
				var slotName = GetSlotName( binding );
				if ( string.IsNullOrEmpty( slotName ) ) continue;
				if ( !SuiEventMatrix.TryGet( el.Type, kv.Key, out var entry ) ) continue;

				var delegateType = string.IsNullOrEmpty( entry.CodeDelegate ) ? "Action" : entry.CodeDelegate;
				sb.Append( "\tpublic " ).Append( delegateType ).Append( ' ' ).Append( slotName )
					.AppendLine( " { get; set; }" );
			}
		}
	}

	/// <summary>
	/// "Action&lt;float&gt;" → "float". Empty for plain "Action". Used by the
	/// Doo emit path to pin the argument hint type and to build the bridging
	/// lambda's parameter list.
	/// </summary>
	private static string ExtractGenericArg( string delegateType )
	{
		if ( string.IsNullOrEmpty( delegateType ) ) return "";
		var lt = delegateType.IndexOf( '<' );
		var gt = delegateType.LastIndexOf( '>' );
		if ( lt < 0 || gt < 0 || gt <= lt + 1 ) return "";
		return delegateType.Substring( lt + 1, gt - lt - 1 );
	}

	/// <summary>
	/// The schema-side identifier that becomes the generated slot name.
	/// Code mode uses <see cref="SuiEventBinding.Handler"/>; Doo mode uses
	/// <see cref="SuiEventBinding.DooPropertyName"/>. Returns null when the
	/// binding has neither (validator should have flagged that already).
	/// </summary>
	private static string GetSlotName( SuiEventBinding binding )
	{
		if ( binding == null ) return null;
		return binding.Mode == SuiEventMode.Code ? binding.Handler : binding.DooPropertyName;
	}

}
