namespace SboxUiDesigner.Runtime;

/// <summary>
/// Runtime migrations for <see cref="SuiDocument"/>. Applied on load (after
/// JSON deserialize) so older documents pick up new defaults without losing
/// the user's intent.
///
/// Idempotent — running twice is a no-op.
/// </summary>
public static class SuiDocumentMigration
{
	public static void Apply( SuiDocument doc )
	{
		if ( doc?.Elements == null ) return;

		foreach ( var el in doc.Elements )
		{
			MigrateTextSizeMode( el );
		}
	}

	/// <summary>
	/// Pre-2026-05-08 documents had no <c>TextSizeMode</c> field. JSON
	/// deserialise gives the default <see cref="SuiTextSizeMode.Auto"/>, but
	/// legacy Text elements were sized with explicit Width/Height by the user.
	/// Auto mode derives size from content and would shrink the box, breaking
	/// the user's layout. We detect "Auto + has explicit W/H" and force Fixed
	/// to preserve visual fidelity.
	/// </summary>
	private static void MigrateTextSizeMode( SuiElement el )
	{
		if ( el.Type != SuiElementType.Text ) return;
		if ( el.Props == null ) return;
		if ( el.Layout == null ) return;
		if ( el.Props.TextSizeMode != SuiTextSizeMode.Auto ) return;

		// Auto with explicit size = legacy. Force Fixed.
		if ( el.Layout.Width > 0 || el.Layout.Height > 0 )
		{
			el.Props.TextSizeMode = SuiTextSizeMode.Fixed;
		}
	}
}
