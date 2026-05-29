using System.Text;
using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.Generation;

/// <summary>
/// V1.5 M3 — emits the <c>@ref="FieldName"</c> attribute + matching renderer
/// field declaration for every element whose <see cref="SuiElementFlags.ExposeAsVariable"/>
/// flag is set (PRD 20 § 5).
///
/// <para>The Razor native <c>@ref</c> attribute does the capture; we just emit
/// the attribute onto the markup tag and a typed property on the renderer
/// class so the user's <c>&lt;Name&gt;.partial.cs</c> can poke the live element
/// via <c>View?.FieldName</c>.</para>
/// </summary>
public static class SuiElementRefEmitter
{
	/// <summary>
	/// Append <c>@ref="FieldName"</c> to the open tag when the element opted in.
	/// Field name is the sanitized element <see cref="SuiElement.Name"/>.
	/// </summary>
	public static void EmitRazorRef( SuiElement el, StringBuilder sb )
	{
		if ( el?.Flags == null || !el.Flags.ExposeAsVariable ) return;
		var field = SuiNameSanitizer.ToCSharpIdentifier( el.Name );
		if ( string.IsNullOrEmpty( field ) ) return;
		sb.Append( " @ref=\"" ).Append( field ).Append( '"' );
	}

	/// <summary>
	/// Append renderer-side field declarations for every exposed element.
	/// The type comes from a per-SuiElementType map (Button → Sandbox.UI.Button,
	/// Text → Sandbox.UI.Label, etc.) matching PRD 20 § 5.4.
	/// </summary>
	public static void EmitRendererFields( SuiDocument doc, StringBuilder sb )
	{
		if ( doc?.Elements == null ) return;

		foreach ( var el in doc.Elements )
		{
			if ( el?.Flags == null || !el.Flags.ExposeAsVariable ) continue;
			var field = SuiNameSanitizer.ToCSharpIdentifier( el.Name );
			if ( string.IsNullOrEmpty( field ) ) continue;

			var type = MapElementTypeToRefType( el.Type );
			sb.Append( "\tpublic " ).Append( type ).Append( ' ' ).Append( field )
				.AppendLine( " { get; private set; }" );
		}
	}

	/// <summary>
	/// SuiElementType → Sandbox.UI type the Razor <c>@ref</c> captures into.
	/// Falls back to <c>Sandbox.UI.Panel</c> for any container-like type that
	/// doesn't have a specialised runtime class.
	/// </summary>
	public static string MapElementTypeToRefType( SuiElementType type ) => type switch
	{
		SuiElementType.Text        => "global::Sandbox.UI.Label",
		SuiElementType.Image       => "global::Sandbox.UI.Image",
		SuiElementType.Button      => "global::Sandbox.UI.Button",
		// ProgressBar in Sandbox.UI is a Panel subclass without a separate
		// public type at the time of writing; capture it as Panel until/unless
		// Facepunch exposes a typed one. The .partial.cs author can still
		// manipulate CSS classes / styles through Panel.
		SuiElementType.ProgressBar => "global::Sandbox.UI.Panel",
		_                          => "global::Sandbox.UI.Panel",
	};
}
