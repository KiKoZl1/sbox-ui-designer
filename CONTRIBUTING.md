# Contributing to SUI Designer

Thanks for your interest! This document covers how to report bugs, propose features, and submit pull requests.

## Quick links

- 🐛 **Bug?** → [Open an issue](https://github.com/KiKoZl1/sbox-ui-designer/issues/new?template=bug_report.yml)
- ✨ **Feature idea?** → [Open an issue](https://github.com/KiKoZl1/sbox-ui-designer/issues/new?template=feature_request.yml)
- ❓ **Question / showcase / general talk?** → [Discussions](https://github.com/KiKoZl1/sbox-ui-designer/discussions)
- 🔒 **Security issue?** → See [SECURITY.md](SECURITY.md) — don't open a public issue

## Reporting bugs

Before opening an issue:

1. **Search [existing issues](https://github.com/KiKoZl1/sbox-ui-designer/issues)** — yours may already be reported.
2. **Check [Known issues](https://kikozl1.github.io/sbox-ui-designer/reference/known-issues/)** — some bugs are documented with workarounds.
3. **Reproduce on a clean `.sui`** — if you can, attach a minimal `.sui` that triggers the bug. Smaller repro = faster fix.

A good bug report includes:

- s&box editor version + OS
- Steps to reproduce
- Expected vs actual behavior
- The `.sui` file (paste contents or attach)
- Engine console output if compile-related
- Screenshot if visual

Use the [bug report template](https://github.com/KiKoZl1/sbox-ui-designer/issues/new?template=bug_report.yml) — it asks for exactly these.

## Proposing features

Open an issue using the [feature request template](https://github.com/KiKoZl1/sbox-ui-designer/issues/new?template=feature_request.yml). Describe:

- **The problem** — what are you trying to do that's hard or impossible today?
- **Proposed solution** — what you think should change.
- **Alternatives considered** — workarounds you tried; competing designs.

I'd rather hear about the **problem** than receive a fully-spec'd solution. Often the right design becomes obvious only after the problem is well-understood.

## Submitting pull requests

PRs are very welcome. To make review smooth:

### Before you start

- **Open an issue first** for anything beyond a tiny bugfix. Lets us agree on the approach before you spend time. Trivial fixes (typo, one-line bug) — just open the PR.
- **One change per PR**. If you're tempted to bundle "while I'm here, also …", split it.

### Building locally

This is a Sandbox Library, not a standalone .NET project. To work on it:

1. Clone into any s&box project's `Libraries/` folder:
   ```
   cd <your-sbox-project>/Libraries/
   git clone https://github.com/KiKoZl1/sbox-ui-designer.git kikozl.sbox_ui_designer
   ```
2. Open the s&box editor with that project loaded.
3. Open SUI Designer from the **View** menu.
4. Edit `.cs` files — the engine hot-reloads automatically.

### Code conventions

- **Existing style wins** — match what's already in the file. Tabs for indentation in C#, 2-space in `.razor`/`.scss`.
- **No new files unless needed** — prefer extending existing classes.
- **No comments explaining WHAT** — let the code speak. Comments are for non-obvious WHY (workarounds, references to engine quirks).
- **Tests** — there's no formal test suite yet. Verify in-editor: open a `.sui`, exercise your change, compile, Test in Play.

### What to commit

- ✅ Source changes (`Code/`, `Editor/`, `Assets/`, `docs/`, `Libraries/local.sboxpro/` config).
- ❌ Generated files (`_sui_preview/`, `.sui-backups/`, `.sui-manifest/`).
- ❌ Local-only assets (test scenes, debug `.sui` files).
- ❌ Editor session state (`.unsaved/`, etc).

The `.gitignore` should handle most of this. If you see anything sketchy in your `git status`, ask before committing.

### Commit messages

Imperative mood, lowercase prefix, short summary:

```
fix: anchor pivot calculation for Stretch mode
feat: add letter-spacing field to Text properties
docs: clarify Test in Play limitations
refactor: extract layout solver from canvas widget
```

Body optional but appreciated for non-trivial changes — explain the *why* and any trade-offs.

### Submitting the PR

1. Push your branch to your fork.
2. Open a PR against `main`.
3. Fill in the PR template — it asks for summary, testing, screenshots if visual.
4. Wait for review. I'll usually respond within a few days.

### Review process

I do all PR reviews personally. Expect:

- **Small PRs**: usually merged within a few days, often same week.
- **Medium PRs**: comments + back-and-forth. Don't take suggestions personally — I push back on my own ideas too when something better appears.
- **Large PRs**: I'll likely ask to split it.

If I haven't responded in 2 weeks, please ping the PR — sometimes notifications get lost.

## Areas where help is especially welcome

- 🐛 **Bug fixes** — anything in [Issues](https://github.com/KiKoZl1/sbox-ui-designer/issues) tagged `bug` is fair game.
- 📚 **Documentation** — typos, clarifications, additional examples. The `docs/` folder is plain markdown.
- 🎨 **Sample `.sui` files** — interesting HUDs, menus, inventories that demo techniques.
- 🌐 **Localization** — UI strings are currently English-only.
- 🧪 **Test coverage** — a real test suite is a wishlist item.

Things that are off the table for now:

- ❌ Switching the licensing model.
- ❌ Major architecture rewrites without a prior issue + agreement.
- ❌ Adding new dependencies — keep the addon zero-dependency.

## Code of conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). Be respectful, assume good faith, criticize ideas not people. Violations get reported per the document.

## License of your contributions

By submitting a PR, you agree your contributions will be licensed under the project's [MIT license](LICENSE). No CLA — your contribution implies consent.

## Questions

If anything here is unclear, [open a discussion](https://github.com/KiKoZl1/sbox-ui-designer/discussions). It's the lowest-friction way to ask "is this a good idea before I spend a weekend on it?"
