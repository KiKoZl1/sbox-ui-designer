# Generation

Razor/SCSS generator. Pure code — no editor dependency. Same generator drives preview AND final compile.

Files (planned):

- `SuiRazorGenerator.cs`
- `SuiScssGenerator.cs`
- `SuiGenerationContext.cs`
- `SuiGenerationResult.cs`
- `SuiValidator.cs`
- `SuiNameSanitizer.cs`
- `SuiHashUtility.cs`

Constraints (see `docs/prd/08_layout_systems.md` and `docs/prd/10_generator_razor_scss.md`):

- Allowed-property list enforced; `display` is only `flex` or `none`; no CSS Grid; no `position: fixed`.
- MVP: no `@expression` in markup body, no `BuildHash()` override.
- SCSS scoped under the panel-type selector (`InventoryUI { .root { ... } }`).
