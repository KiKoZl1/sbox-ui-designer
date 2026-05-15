using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// Connects one property of a UI element to a document <see cref="SuiVariable"/>,
/// optionally through a chain of converters (PRD 18 § 4).
///
/// <para><b>Storage reconciliation (M1):</b> PRD 18 § 4.1 idealised an inline
/// <c>$bind</c> object inside a generic <c>Props</c> map. The real
/// <see cref="SuiElementProps"/> is a typed field bag with no generic slot, so
/// V1.5 stores bindings as a list on <see cref="SuiElement.Bindings"/> keyed by
/// <see cref="Property"/> name — the codebase's existing element-property-binding
/// shape. Functionally identical: a binding overrides the literal for its
/// <see cref="Property"/>. See the M1 reconciliation patch note in PRD 18.</para>
/// </summary>
public sealed class SuiBinding
{
	/// <summary>Stable GUID <c>bind_XXXXXXXX</c> — rename-safe internal identity + find-usages indexing.</summary>
	public string Id { get; set; }

	/// <summary>Target property name on the owning element (e.g. <c>"Value"</c>, <c>"Text"</c>, <c>"Tint"</c>).</summary>
	public string Property { get; set; }

	/// <summary>How the binding flows. Defaults to <see cref="SuiBindingMode.OneWay"/>.</summary>
	public SuiBindingMode Mode { get; set; } = SuiBindingMode.OneWay;

	/// <summary>The data origin — a document Variable.</summary>
	public SuiBindingSource Source { get; set; } = new();

	/// <summary>
	/// Ordered converter chain applied from <see cref="Source"/> to the property.
	/// Empty = a direct bind with no conversion.
	/// </summary>
	public List<SuiBindingConverterStep> Converters { get; set; } = new();

	/// <summary>
	/// Runtime value used when the source resolves to null/missing. Null means
	/// the target property's type-default is used instead (PRD 18 § 4.2).
	/// </summary>
	public JsonNode FallbackValue { get; set; }

	/// <summary>Generate a stable binding id like <c>bind_c4d6e7a8</c>.</summary>
	public static string NewBindingId()
		=> "bind_" + Guid.NewGuid().ToString( "N" ).Substring( 0, 8 );

	public SuiBinding Clone()
	{
		var c = new SuiBinding
		{
			Id = Id,
			Property = Property,
			Mode = Mode,
			Source = Source?.Clone() ?? new(),
			FallbackValue = FallbackValue?.DeepClone(),
		};
		foreach ( var step in Converters ) c.Converters.Add( step?.Clone() );
		return c;
	}
}
