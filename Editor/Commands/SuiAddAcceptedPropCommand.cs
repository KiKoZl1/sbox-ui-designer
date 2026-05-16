using SboxUiDesigner.Runtime;

namespace SboxUiDesigner.EditorUi.Commands;

/// <summary>
/// Add a new <see cref="SuiAcceptedProp"/> to the document (PRD 19 § 4.3).
/// When <paramref name="matchingVariable"/> is non-null it is added in the same
/// command so the bridge Variable (Source.Kind = FromAcceptedProp) appears
/// atomically — undo removes both.
/// </summary>
public sealed class SuiAddAcceptedPropCommand : ISuiCommand
{
	private readonly SuiAcceptedProp _prop;
	private readonly SuiVariable _matchingVariable;

	public string Description => $"Add Accepted Prop '{_prop?.Name}'";

	public SuiAddAcceptedPropCommand( SuiAcceptedProp prop, SuiVariable matchingVariable = null )
	{
		_prop = prop;
		_matchingVariable = matchingVariable;
	}

	public void Apply( SuiDocument doc )
	{
		if ( doc == null || _prop == null ) return;
		doc.AcceptedProps ??= new();
		if ( !doc.AcceptedProps.Contains( _prop ) )
			doc.AcceptedProps.Add( _prop );

		if ( _matchingVariable != null )
		{
			doc.Variables ??= new();
			if ( !doc.Variables.Contains( _matchingVariable ) )
				doc.Variables.Add( _matchingVariable );
		}
	}

	public void Undo( SuiDocument doc )
	{
		doc?.AcceptedProps?.Remove( _prop );
		if ( _matchingVariable != null )
			doc?.Variables?.Remove( _matchingVariable );
	}
}
