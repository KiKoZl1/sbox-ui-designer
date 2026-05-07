# Tests

Unit + golden-file tests for the generator and document model.

Test categories (per `docs/prd/14_v1_v2_roadmap_tests_and_acceptance.md`):

- Unit: `SuiDocument` serialization, schema migration, hierarchy validation, name sanitization, hash computation, manifest conflict checks, BEGIN/END marker parsing, allowed-property list enforcement.
- Golden file: `.sui` fixture → expected `.razor` and `.razor.scss` output.
- Forbidden output: assert generator never emits `display: grid`, `display: contents`, `display: block`, `position: fixed`, or any property outside the allowed list. Also asserts MVP markup has zero `@expression` references and no `BuildHash()` override.
- File safety: hash mismatch backup-then-overwrite flow.
- Preview: cache lives inside compilable code root, survives `SceneRenderingWidget` hotload.
