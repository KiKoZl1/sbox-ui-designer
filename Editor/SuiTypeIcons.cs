namespace SboxUiDesigner.EditorUi;

/// <summary>
/// Thin shim over <see cref="SuiTypeRegistry"/> kept so existing call sites that
/// only want the icon stay readable. New code should call
/// <see cref="SuiTypeRegistry"/> directly to get the full <see cref="SuiTypeMetadata"/>
/// (icon + colour + display name).
/// </summary>
public static class SuiTypeIcons
{
	/// <summary>Material-icon name for a TypeRef. Delegates to <see cref="SuiTypeRegistry.Icon"/>.</summary>
	public static string ForType( string typeRef ) => SuiTypeRegistry.Icon( typeRef );
}
