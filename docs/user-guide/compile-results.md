---
layout: default
title: Compile Results
parent: User Guide
nav_order: 8
---

# Compile Results

The third tab in the bottom panel. Shows the classification of every file `SuiCompileWriter` touched on the last `Ctrl+B`.

## Categories

| Category | Meaning |
|---|---|
| **Generated** | File did not exist on disk → wrote fresh. New code in your project. |
| **Skipped** | File existed, our header + hash match → no-op. Document hasn't changed since last compile. |
| **Preserved** | File existed, our header matched, but content **differs**. Prior content moved to a timestamped backup; new content written. |
| **User-Owned** | A `.User.scss` sidecar — created once if missing, **never overwritten**. Listed so you know it survived intact. |
| **Conflicts** | File existed **without** our header (or with another document's header). NOT touched. User must move/rename it manually. |
| **Obsolete** | File was in the previous manifest but **not** in this generation pass. Probably renamed or removed. Reported, but never auto-deleted in V1. |

## Counts

The summary line at the top:

```
✓ Compile OK — Generated 1, Skipped 1, Preserved 0, User 1
```

When there are conflicts or errors, it switches:

```
✗ Compile failed — Generated 0, Skipped 0, Preserved 0, ⚠ Conflicts 1, ✗ Errors 0
```

## Backup folder

When **Preserved** files are written, the prior content is backed up to:

```
<projectRoot>/.sui-backups/<DocumentName>/<UTC-timestamp>/<file>
```

Backups live **outside** `Code/` to avoid the engine compiling them as duplicate `partial class` declarations.

The Compile Results widget shows the backup folder path so you can navigate to it. **Backups are never auto-cleaned** — clear them yourself or via **Tools → Clean All SUI Caches** when comfortable.

## Manifest

Per-document manifest at:

```
<outputFolder>/.sui-manifest/<DocumentId>.json
```

Tracks `Path`, `LastHash`, `OwnedByDocumentId`, `GeneratorVersion` for every file the document owns. The writer compares the manifest against the generation pass to detect Obsolete entries.

## Conflict resolution

If a file lands as **Conflict**:

1. Look at the file. Does it have the `SUI:GENERATED:BEGIN` … `SUI:GENERATED:END` header?
2. If no header → it's foreign. Move/rename it (e.g., add `_user.razor` suffix), then recompile.
3. If header is from **another document's** DocumentId → some doc renamed and its old generated files were left behind. Delete them, recompile.
4. If header IS from this doc but hash check is failing for unexpected reasons → file the issue.

## Reference

- Source: [`Editor/SuiCompileWriter.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/SuiCompileWriter.cs)
- Source: [`Editor/SuiCompileResult.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/SuiCompileResult.cs)
- Source: [`Editor/Widgets/SuiCompileResultsWidget.cs`](https://github.com/KiKoZl1/sbox-ui-designer/blob/main/Editor/Widgets/SuiCompileResultsWidget.cs)
- See also: [Compile + output workflow]({% link workflows/compile-and-output.md %})
