using System.Collections.Generic;
using SboxUiDesigner.Generation;

namespace SboxUiDesigner.Runtime;

/// <summary>
/// V1.5 M3 (PRD 20 § 4 / § 8.2) — unified collision detector across every
/// paradigm that contributes a public identifier to the generated wrapper:
///
/// <list type="bullet">
///   <item>Variable names (one identifier per <see cref="SuiVariable.Name"/>).</item>
///   <item>SuiReference field names (sanitised element <see cref="SuiElement.Name"/>).</item>
///   <item>Code-mode event handlers (one <c>[Property] Action</c> per handler).</item>
///   <item>Doo-mode event slots (one <c>[Property] Doo</c> per DooPropertyName).</item>
///   <item><c>@ref</c> exposed element field names (sanitised element name).</item>
/// </list>
///
/// <para>Two members with the same name on the generated wrapper produce a
/// guaranteed CS0102 (or worse, a silent shadow on the renderer). The detector
/// surfaces every collision in one pass so the Designer can show all of them
/// at once instead of forcing the user to fix-recompile-fix-recompile.</para>
/// </summary>
public static class SuiNameConflictDetector
{
	public enum Source
	{
		Variable,
		SuiReference,
		EventCodeHandler,
		EventDooProperty,
		ExposedElement,
	}

	public readonly struct Conflict
	{
		public string Name { get; init; }
		public Source SourceKind { get; init; }
		/// <summary>Element or Variable id the offender lives on. Empty for Variables (they're top-level).</summary>
		public string OwnerId { get; init; }
		/// <summary>Human-readable origin (e.g. "FireButton.OnClick" or "Variable 'Score'").</summary>
		public string Where { get; init; }
	}

	/// <summary>
	/// Walk every identifier source and return the groups whose name is
	/// claimed by more than one contributor. Each group lists every
	/// contributor so the Designer can flag all of them simultaneously.
	/// </summary>
	public static IReadOnlyDictionary<string, IReadOnlyList<Conflict>> Detect( SuiDocument doc )
	{
		var contributors = new Dictionary<string, List<Conflict>>( System.StringComparer.Ordinal );

		void Add( string name, Conflict c )
		{
			if ( string.IsNullOrEmpty( name ) ) return;
			if ( !contributors.TryGetValue( name, out var list ) )
			{
				list = new List<Conflict>();
				contributors[name] = list;
			}
			list.Add( c );
		}

		if ( doc?.Variables != null )
		{
			foreach ( var v in doc.Variables )
			{
				if ( v == null || string.IsNullOrEmpty( v.Name ) ) continue;
				Add( v.Name, new Conflict
				{
					Name = v.Name,
					SourceKind = Source.Variable,
					OwnerId = v.Id ?? "",
					Where = $"Variable '{v.Name}'",
				} );
			}
		}

		if ( doc?.Elements != null )
		{
			foreach ( var el in doc.Elements )
			{
				if ( el == null ) continue;

				// SuiReference field (sanitised name; matches what the wrapper emitter writes).
				if ( el.Type == SuiElementType.SuiReference && !string.IsNullOrEmpty( el.Name ) )
				{
					var field = SuiNameSanitizer.ToCSharpIdentifier( el.Name );
					Add( field, new Conflict
					{
						Name = field,
						SourceKind = Source.SuiReference,
						OwnerId = el.Id ?? "",
						Where = $"SuiReference '{el.Name}'",
					} );
				}

				// Exposed element field (also sanitised; same emitter rule).
				if ( el.Flags != null && el.Flags.ExposeAsVariable && !string.IsNullOrEmpty( el.Name ) )
				{
					var field = SuiNameSanitizer.ToCSharpIdentifier( el.Name );
					Add( field, new Conflict
					{
						Name = field,
						SourceKind = Source.ExposedElement,
						OwnerId = el.Id ?? "",
						Where = $"@ref on '{el.Name}'",
					} );
				}

				if ( el.Events != null )
				{
					foreach ( var kv in el.Events )
					{
						var binding = kv.Value;
						if ( binding == null ) continue;
						switch ( binding.Mode )
						{
							case SuiEventMode.Code:
								if ( string.IsNullOrEmpty( binding.Handler ) ) continue;
								Add( binding.Handler, new Conflict
								{
									Name = binding.Handler,
									SourceKind = Source.EventCodeHandler,
									OwnerId = el.Id ?? "",
									Where = $"{el.Name}.{kv.Key} (Code handler)",
								} );
								break;
							case SuiEventMode.Doo:
								if ( string.IsNullOrEmpty( binding.DooPropertyName ) ) continue;
								Add( binding.DooPropertyName, new Conflict
								{
									Name = binding.DooPropertyName,
									SourceKind = Source.EventDooProperty,
									OwnerId = el.Id ?? "",
									Where = $"{el.Name}.{kv.Key} (Doo slot)",
								} );
								break;
						}
					}
				}
			}
		}

		var result = new Dictionary<string, IReadOnlyList<Conflict>>( System.StringComparer.Ordinal );
		foreach ( var kv in contributors )
		{
			if ( kv.Value.Count > 1 ) result[kv.Key] = kv.Value;
		}
		return result;
	}
}
