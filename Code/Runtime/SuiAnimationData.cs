using System.Collections.Generic;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// V2 — animation timeline data. Reserved in the schema so MVP-saved documents
/// are forward-compatible.
/// </summary>
public sealed class SuiAnimationData
{
	public string Id { get; set; }
	public string Name { get; set; }

	/// <summary>Duration in seconds.</summary>
	public float Duration { get; set; } = 0.25f;

	public bool Loop { get; set; } = false;

	public List<SuiAnimationTrack> Tracks { get; set; } = new();

	public SuiAnimationData Clone()
	{
		var clone = new SuiAnimationData
		{
			Id = Id,
			Name = Name,
			Duration = Duration,
			Loop = Loop,
		};
		foreach ( var t in Tracks )
			clone.Tracks.Add( t.Clone() );
		return clone;
	}
}

public sealed class SuiAnimationTrack
{
	/// <summary>Stable id of the animated element.</summary>
	public string ElementId { get; set; }

	/// <summary>Property name, e.g. "opacity", "x", "scale".</summary>
	public string Property { get; set; }

	public List<SuiAnimationKey> Keys { get; set; } = new();

	public SuiAnimationTrack Clone()
	{
		var clone = new SuiAnimationTrack
		{
			ElementId = ElementId,
			Property = Property,
		};
		foreach ( var k in Keys )
			clone.Keys.Add( k.Clone() );
		return clone;
	}
}

public sealed class SuiAnimationKey
{
	public float Time { get; set; }
	public float Value { get; set; }
	public SuiAnimationEasing Easing { get; set; } = SuiAnimationEasing.EaseOut;

	public SuiAnimationKey Clone() => new()
	{
		Time = Time,
		Value = Value,
		Easing = Easing,
	};
}
