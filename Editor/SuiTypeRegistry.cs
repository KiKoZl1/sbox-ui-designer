namespace SboxUiDesigner.EditorUi;

/// <summary>
/// One UI-side description of a TypeRef — Material icon, accent colour, and
/// a friendly display name. Every type-aware surface in the SUI Designer
/// (Variables panel rows, bind-popup property + source dropdowns, the
/// "Expects: X" hint, …) reads from here, so the look stays consistent and
/// a single edit propagates everywhere.
/// </summary>
public sealed record SuiTypeMetadata( string Icon, string Color, string DisplayName );

/// <summary>
/// Per-TypeRef metadata registry — icon + colour + display name.
///
/// <para>Inspired by the Unreal Engine Blueprint pin convention (we sampled the
/// canonical FLinearColor values straight from <c>GraphEditorSettings.cpp</c>),
/// then adjusted for legibility on our dark theme. <b>Not a literal copy:</b>
/// Color stays a distinctive violet rather than UE's struct-blue (which would
/// collide with structs visually), and the dark UE values (Boolean #900000,
/// Byte #006F65) are brightened. We also pair every type with a <b>semantic</b>
/// Material icon — so a colour-blind user can still tell <c>Color</c> from
/// <c>string</c> by the icon SHAPE, not just the hue.</para>
/// </summary>
public static class SuiTypeRegistry
{
	private static readonly SuiTypeMetadata DefaultMeta
		= new( "data_object", "#9ca3af", "?" );

	/// <summary>Resolve metadata for any TypeRef — primitives, engine types, <c>Enum:*</c>, <c>Component:*</c>, <c>List&lt;T&gt;</c>.</summary>
	public static SuiTypeMetadata Get( string typeRef )
	{
		if ( string.IsNullOrEmpty( typeRef ) ) return DefaultMeta;

		// List<T> — inherits the element type's accent so a list-of-int shows up
		// teal like a single int. Icon swaps for the list glyph; the display
		// name preserves the generic shape.
		if ( typeRef.StartsWith( "List<" ) && typeRef.EndsWith( ">" ) )
		{
			var inner = typeRef.Substring( "List<".Length, typeRef.Length - "List<".Length - 1 );
			var innerMeta = Get( inner );
			return new SuiTypeMetadata( "format_list_bulleted", innerMeta.Color, $"List<{innerMeta.DisplayName}>" );
		}

		if ( typeRef.StartsWith( "Enum:" ) )
			return new SuiTypeMetadata( "list", "#34d399", typeRef.Substring( "Enum:".Length ) );

		if ( typeRef.StartsWith( "Component:" ) )
			return new SuiTypeMetadata( "extension", "#818cf8", typeRef.Substring( "Component:".Length ) );

		return typeRef switch
		{
			"bool" or "Boolean"                          => new( "toggle_on",    "#ef4444", "bool"   ),
			"int" or "Int32"                             => new( "pin",          "#2dd4bf", "int"    ),
			"long" or "Int64"                            => new( "pin",          "#2dd4bf", "long"   ),
			"float" or "Single"                          => new( "linear_scale", "#a3e635", "float"  ),
			"double" or "Double"                         => new( "linear_scale", "#a3e635", "double" ),
			"string" or "String"                         => new( "text_fields",  "#f472b6", "string" ),

			"Color"                                      => new( "palette",      "#c084fc", "Color"     ),

			"Vector2"                                    => new( "near_me",      "#facc15", "Vector2"   ),
			"Vector3"                                    => new( "near_me",      "#facc15", "Vector3"   ),
			"Vector4"                                    => new( "near_me",      "#facc15", "Vector4"   ),

			"Angles"                                     => new( "rotate_right", "#9eb2ff", "Angles"    ),
			"Rotation"                                   => new( "rotate_right", "#9eb2ff", "Rotation"  ),
			"Transform"                                  => new( "transform",    "#fb923c", "Transform" ),

			"Texture"                                    => new( "image",        "#60a5fa", "Texture"   ),
			"Resource"                                   => new( "folder_open",  "#60a5fa", "Resource"  ),
			"Sound"                                      => new( "volume_up",    "#60a5fa", "Sound"     ),
			"Material"                                   => new( "texture",      "#60a5fa", "Material"  ),

			"T"                                          => new( "data_object",  "#9ca3af", "T"        ),

			_                                            => new( "data_object",  "#9ca3af", typeRef    ),
		};
	}

	public static string Icon( string typeRef )        => Get( typeRef ).Icon;
	public static string Color( string typeRef )       => Get( typeRef ).Color;
	public static string DisplayName( string typeRef ) => Get( typeRef ).DisplayName;
}
