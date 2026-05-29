namespace SboxUiDesigner.Runtime;

/// <summary>
/// V1.5 M3 — one event slot on a <see cref="SuiElement"/> (PRD 20 § 3.2).
/// Each entry in <see cref="SuiElement.Events"/> maps an event name from the
/// matrix (<see cref="SuiEventMatrix"/>) to either a C# method stub
/// (<see cref="SuiEventMode.Code"/>) or a Doo asset slot
/// (<see cref="SuiEventMode.Doo"/>).
///
/// <para>The two modes coexist in the schema so toggling between them is
/// non-destructive — the user can flip back and forth without losing either
/// side. Codegen picks whichever <see cref="Mode"/> is currently set.</para>
/// </summary>
public sealed class SuiEventBinding
{
	/// <summary>Which side of the toggle is active. Defaults to <see cref="SuiEventMode.Code"/>.</summary>
	public SuiEventMode Mode { get; set; } = SuiEventMode.Code;

	/// <summary>
	/// C# method name to emit into <c>&lt;Name&gt;.partial.cs</c> as a stub.
	/// Required when <see cref="Mode"/> is <see cref="SuiEventMode.Code"/>.
	/// Validator checks it is a legal C# identifier; it does NOT verify the
	/// method exists at the call site — Razor compile catches that.
	/// </summary>
	public string Handler { get; set; }

	/// <summary>
	/// The <c>[Property] public Doo &lt;Name&gt; { get; set; }</c> field name
	/// emitted on the wrapper class. Required when <see cref="Mode"/> is
	/// <see cref="SuiEventMode.Doo"/>. Must be a legal C# identifier and
	/// unique within the document (validator covers collisions with Variables,
	/// child reference field names, other Doo properties, and @ref names).
	/// </summary>
	public string DooPropertyName { get; set; }

	/// <summary>
	/// WBP-like Doo storage (PRD 20 spike result, 2026-05-29). When
	/// <see cref="Mode"/> is <see cref="SuiEventMode.Doo"/>, the Designer
	/// authors the Doo's <c>Body</c> directly inside the <c>.sui</c> via the
	/// embedded BlockTree + DooEditorWidget popup. The codegen wrapper
	/// initializes the runtime <c>Doo</c> property from this serialized body,
	/// so 1 <c>.sui</c> = 1 default Doo per slot — exactly how UE5
	/// WidgetBlueprint stores its Graph inside the widget class. Inspector
	/// override per-instance still works (engine-native behavior on Doo
	/// <c>[Property]</c>).
	///
	/// <para>Null when <see cref="Mode"/> is Code or when the user hasn't
	/// touched the Doo yet. Sandbox.Json serializes <c>Doo : IJsonConvert</c>
	/// natively into the <c>.sui</c> file (key shape: <c>{"body": [...]}</c>).</para>
	/// </summary>
	public Sandbox.Doo DooBody { get; set; }

	public SuiEventBinding Clone() => new()
	{
		Mode = Mode,
		Handler = Handler,
		DooPropertyName = DooPropertyName,
		// Doo has no native Clone; deep-copy via JSON round-trip so undo /
		// duplicate-element flows don't share the same Body reference and
		// edits leak across the two slots.
		DooBody = DooBody == null ? null
			: Sandbox.Json.Deserialize<Sandbox.Doo>( Sandbox.Json.Serialize( DooBody ) ),
	};
}
