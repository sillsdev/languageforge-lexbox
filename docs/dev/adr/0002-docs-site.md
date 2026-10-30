# ADR 0002: Product docs site in the monorepo

Status: Accepted

## Context

There was no documentation home. Technical content (sync architecture, integrations, CI/CD) lived in `AGENTS.md` files and the root README, written for coding agents. User documentation didn't exist in the repo; the apps link out to the FieldWorks help site, Scribe how-tos and the community forum.

Other SIL products (surveyed 2026-07): Bloom, Scripture Forge and Paratext use Docusaurus with Crowdin for translation; Bloom adds Algolia DocSearch and its Ask AI chat. The Combine uses MkDocs Material for offline in-app bundling. FieldWorks uses WordPress plus GitHub wikis that have gone stale.

FieldWorks Lite ↔ FieldWorks Classic sync is the hardest thing to explain: several static diagrams and UI iterations still confused people.

## Decision

- **One Docusaurus site** with product sections: `/fw-lite/` (FieldWorks Lite app users), `/lexbox/` (lexbox.org and Send/Receive users) and `/technical/` (how it works, for anyone curious; not how to develop it). One pipeline, one search index. "Which app do you use?" is a factual question; persona labels ("I'm a manager") are a known audience-navigation failure (NN/g). The homepage routes with product tiles; `/lexbox/fw-lite` bridges project managers to the FieldWorks Lite guide.
- **Source in this monorepo**, so the docs change in the same PR as the code and reviewers see drift in the diff. The site lives in `docs/site/` and everything there is published; developer docs (setup, troubleshooting, ADRs) live in `docs/dev/`. Rules for keeping the site current: `docs/site/AGENTS.md`.
- **Terminology follows the shipped UI.** CRDT, Harmony, Mercurial and FwHeadless are technical-section vocabulary only.
- **Link to FieldWorks Classic docs, don't duplicate them.** Sibling products, sibling doc sites.
- **The sync explainer** (`/fw-lite/how-sync-works`) is an interactive, question-driven stepper over one fixed picture (device → Lexbox holding two copies → colleague's FieldWorks Classic), mirroring the app's Sync dialog. Users pick their question instead of a persona tab; each answer is a learner-paced sequence of short steps (Mayer's segmenting principle; no autoplay). All content is in `syncScenarios.ts`. Its caps are load-bearing: they fix the overwhelm that sank the static diagrams.
- **React for interactive components** (Docusaurus), accepted though the products are Svelte. Content stays in framework-neutral files. Revisit if product-like components pile up: compile Svelte to web components, or move to Astro Starlight.
- **Deployment**: GitHub Pages from `.github/workflows/docs.yaml` (build on PR, deploy on develop). Final URL: `docs.lexbox.org`, a Cloudflare CNAME `docs` → `sillsdev.github.io`. Until that's set up, CI builds for the repo's own Pages URL (the site URL is env-configurable).
- **Later**: Crowdin for the user guides, Algolia DocSearch and Ask AI, screenshots, app help links pointing here, moving architecture content out of `AGENTS.md` files into `technical/` and linking back.
