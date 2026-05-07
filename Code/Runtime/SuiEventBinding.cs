namespace SboxUiDesigner.Runtime;

/// <summary>
/// V1.5+ — event binding attached to an element.
/// Reserved in the schema so MVP-saved documents are forward-compatible.
/// </summary>
public sealed class SuiEventBinding
{
	/// <summary>Stable id of this binding (independent of element id).</summary>
	public string Id { get; set; }

	/// <summary>Stable id of the target element.</summary>
	public string ElementId { get; set; }

	/// <summary>Event type, e.g. "OnClick", "OnHover", "OnDrop". String-typed for forward-compat.</summary>
	public string EventType { get; set; }

	/// <summary>User-facing handler name, generated into .User.cs.</summary>
	public string HandlerName { get; set; }

	public SuiEventMode Mode { get; set; } = SuiEventMode.Code;

	public SuiEventBinding Clone() => new()
	{
		Id = Id,
		ElementId = ElementId,
		EventType = EventType,
		HandlerName = HandlerName,
		Mode = Mode,
	};
}
