# SUI Designer Samples

This folder holds the sample `.sui` documents that ship with SUI Designer. **The place to learn is [`showcase/`](showcase/README.md)** — 16 polished demos that cover every V1.5 feature with per-folder `README.md`, controller code, and Variables / Bindings / Events tables.

## → [Browse the full showcase](showcase/README.md)

The showcase index is the gateway:

- **Browse by category** — Starter, Input widgets, Interactive states, Runtime-rendered, Full-feature showcase.
- **Browse by concept** — every sample that demonstrates Bindings, Events, ExposeAsVariable patterns, Layout, Visual effects, Widgets, Runtime patterns.
- **Pattern recipes** — "I want to..." → "Look at..." lookup.

## What lives where

```text
samples/
  showcase/    16 production-quality demo .sui samples (start here)
  ui/          legacy tutorial-flavored stubs kept for golden-file regression tests
```

- **`showcase/`** — the polished V1.5 suite. Each sub-folder is self-contained: `.sui` + `<Name>Controller.cs` + `README.md`. Open in the SUI Designer, hit Compile, attach the controller, press Play.
- **`ui/`** — historical placeholder folder retained as fixtures for the generator's golden-file tests. Not a learning resource. New samples go under `showcase/`.

## Installing into your project

The SUI Designer Tools menu has **Install Sample Documents**, which copies the showcase set into your project's `Assets/SuiSamples/`. Use that if you want to play with the samples inside your own project without cloning this repo.

## Contributing a new sample

New samples land under `samples/showcase/<your_sample>/`. Follow the canonical README template (see flagship references in [`showcase/README.md#how-these-are-authored`](showcase/README.md#how-these-are-authored)) and add the sample to both lookup tables in the showcase index file. Full step-by-step in [`showcase/README.md#contributing-a-new-sample`](showcase/README.md#contributing-a-new-sample).
