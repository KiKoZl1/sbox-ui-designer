# Security Policy

## Reporting a vulnerability

If you think you've found a security issue in SUI Designer, **do not open a public issue**. Instead:

- Email: **4knightsinteractivestudios@gmail.com**
- Subject line: `[sbox-ui-designer security]`
- Include: a description, repro steps, and the impact you think it has.

I'll acknowledge within 7 days and work toward a fix in private. After the fix ships, we can coordinate public disclosure.

## What counts as a security issue

SUI Designer is an editor addon — it runs inside the s&box editor, with full filesystem access to your project folder. Things that would count:

- **Path traversal** — a crafted `.sui` file causing writes outside the project folder.
- **Code execution** — a crafted `.sui` or asset triggering arbitrary code execution at compile or load time.
- **Sandbox escape** — the addon doing something the s&box sandbox shouldn't allow.

Things that don't count:

- **Engine bugs** (rgba alpha quirks, CSS parser bugs) — report those to Facepunch.
- **Bad-looking generated SCSS** — that's a normal bug, open a regular issue.
- **Crashes on malformed input** — also a normal bug.

## Supported versions

Only the **latest V1.x release** receives security fixes. If a vulnerability affects an older release, the fix lands in the next V1.x bump — no backports to V1.0 unless severity warrants it.

## Acknowledgments

Reporters who follow this process will be credited in the fix's release notes (with permission).
