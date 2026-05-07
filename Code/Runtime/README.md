# Runtime

Data models, schema, and generation support that run in user game code.

This layer must NOT reference `Editor.*` types. Everything here can be referenced by the runtime UI a consumer ships.

Files (planned):

- `SuiDocument.cs` — root document model
- `SuiElement.cs` — element node
- `SuiCanvasSettings.cs` — canvas/scale/safe area
- `SuiLayoutData.cs` — layout block (Absolute / Flex)
- `SuiStyleData.cs` — visual style block
- `SuiOutputSettings.cs` — output folder + namespace + class name
- `SuiGeneratedFileManifest.cs` — manifest of files owned by this `.sui`
- `SuiEventBinding.cs` — V1.5
- `SuiAnimationData.cs` — V2
