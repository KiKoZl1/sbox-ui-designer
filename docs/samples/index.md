---
layout: default
title: Samples
nav_order: 50
has_children: true
permalink: /samples/
---

# Samples

Per-sample documentation pages for the **16 showcase demos** that ship with SUI Designer V1.5.

Each page below mirrors the per-folder `README.md` from `samples/showcase/<name>/` in the source repo, formatted for the docs site with Jekyll frontmatter, table-of-contents, and cross-links.

## Where to start

- **New to SUI Designer?** Follow the [Sample tour]({% link getting-started/sample-tour.md %}) — six samples ordered from "just mount a panel" to "wire a full settings flow with Apply.All()".
- **Looking for a specific feature?** The [Concept map]({% link reference/concept-map.md %}) answers "which sample teaches me X?".
- **Browsing by category?** The [Showcase samples gallery]({% link reference/showcase-samples.md %}) groups all 16 by Starter, Input widgets, Interactive states, Runtime-rendered, and Full-feature.

## Samples in this section

Use the sidebar to navigate. Pages are ordered alphabetically; difficulty / category tags are noted on each page.

## Source

All sample source lives in [`samples/showcase/`](https://github.com/KiKoZl1/sbox-ui-designer/tree/main/samples/showcase) in the repo. Each folder is self-contained:

```text
samples/showcase/<sample_name>/
  <sample_name>.sui            UI definition — open in Designer + Compile
  <SampleName>Controller.cs    drop on any GameObject
  README.md                    source of these docs site pages
```
