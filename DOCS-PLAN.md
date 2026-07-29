# Documentation plan

Plan and findings behind the `docs/` site on this branch. Written 2026-07-29; remove or move this file once the approach is settled.

## The problem

There is no documentation home. The real technical content (sync architecture, integrations, CI/CD) lives in nine `AGENTS.md` files and the root README — written for coding agents, invisible to anyone who won't browse the repo. User documentation doesn't exist here at all; the app links out to the classic FieldWorks help site and the SIL community forum. Colleagues asking "do you have technical docs / diagrams?" currently get pointed at agent-instruction files.

## What SIL uses (surveyed 2026-07)

| Project | Tooling | i18n |
|---|---|---|
| Bloom (docs.bloomlibrary.org) | Docusaurus + Algolia DocSearch + Ask AI chat | Crowdin scaffolded |
| Scripture Forge (help.scriptureforge.org) | Docusaurus | 6 locales via Crowdin |
| Paratext (manual.paratext.org) | Docusaurus | 5 locales |
| The Combine | MkDocs Material (chosen for offline in-app bundling) | 4 locales |
| Keyman | bespoke PHP (legacy) | — |
| FieldWorks | WordPress + GitHub wikis (stale since 2020–22) | — |

Docusaurus is the SIL convention: markdown in git, Crowdin for translation, static hosting. Bloom's site AI chat is Algolia **Ask AI** — a few lines in `docusaurus.config` pointing at an Algolia index of the rendered markdown site, so choosing Docusaurus gets the AI-chat path for free. GitHub wikis are where SIL dev docs go stale.

## Decisions

- **One Docusaurus site, two doc sections** (`/user-guide/`, `/technical/`): one pipeline, one search index, one AI-chat corpus, but the user guide stays plain-language and translatable while technical docs stay English-first.
- **Source lives in this monorepo** (unlike Bloom/SF/Paratext, which use separate repos): docs next to code is what makes "the AI updates the docs in the same PR as the code change" real, and reviewers see doc drift in the diff.
- **Terminology follows the shipped UI**: FieldWorks Lite, FieldWorks Classic, Lexbox, Send/Receive. CRDT/Harmony/Mercurial/FwHeadless are technical-section vocabulary only.
- **Seed content is adapted from already-reviewed sources** (README, AGENTS.md files, DEVELOPER-*.md), not newly authored prose. `AGENTS.md` files are untouched in this PR; de-duplicating them to link into the site is a follow-up.
- **Deployment**: GitHub Pages workflow included (build on PR, deploy on develop push). Target URL and DNS (`docs.lexbox.org`?) are a team decision.
- Later, in rough order: Crowdin wiring for the user guide (copy Scripture Forge's setup), Algolia DocSearch + Ask AI (copy Bloom's config), screenshots in the user guide, moving `AGENTS.md` architecture content into the site and linking back.

## The sync explainer

The hardest thing to explain is FieldWorks Lite ↔ FieldWorks Classic sync; several static-diagram and UI iterations still confused people. The user-guide page "How sync works" replaces the static diagram with an interactive, question-driven explainer (`SyncExplainer` component):

- **One fixed picture** — your device → Lexbox holding *two copies* of the project → your colleague's FieldWorks Classic — mirroring the app's own Sync dialog. Every answer replays on the same picture so answers build one mental model.
- **Question chips, not persona tabs** — users know their question ("Why doesn't my colleague see my edit?"), not their category. Selecting one plays a user-paced stepper: a token hops the numbered legs, one sentence per step, a badge naming who acts.
- **Progressive disclosure** — surface level bans internal tech names; an "under the hood" accordion maps friendly names to CRDT/Harmony, Mercurial, FwHeadless for experts.
- **All content in one data module** (`syncScenarios.ts`) — editing prose or fixing a behavior fact is a one-line change; strings are extractable for translation.

Design rationale (evidence: Mayer's segmenting principle, Tversky on animation, NN/g on audience-based navigation and progressive disclosure): learner-paced segments beat continuous animation for novice audiences; autoplay and personas are the classic failure modes.

### Ground-truth facts the explainer encodes (verified against code)

- Device ↔ Lexbox (CRDT) sync is fully automatic: after every edit (~100 ms debounce), on project open, on server push (SignalR), on reconnect; recovery loop every 5 min. `BackgroundSyncService`, `LexboxHubConnection`.
- The Lexbox-internal merge (Lite copy ↔ Classic copy) is **user-triggered only** — the Sync button in FieldWorks Lite or "Sync FieldWorks Lite" on the project page. No scheduler, cron, hg hook, or webhook exists. Jobs queue one project at a time server-wide (`SyncHostedService`); first sync clones + imports and can take minutes.
- Lexbox ↔ FieldWorks Classic is the user's Chorus **Send/Receive**, unchanged.
- Same-field concurrent edits: among Lite users, latest change wins (hybrid logical clock); between Lite and Classic since the last merge, the Classic value wins (`CrdtFwdataProjectSyncService.SyncInternal` applies FwData→CRDT first). Different-field edits both survive. Nothing is surfaced to users as a conflict.
- Found while researching: `backend/FwHeadless/AGENTS.md` wrongly says FwHeadless "detects Mercurial changes" — it only drains the queue fed by `/api/merge/execute`. Fix separately.

### Explainer maintenance rules

Hard caps are load-bearing (they fix the overwhelm that sank the static diagrams): ~8 questions, ≤6 steps each, one sentence per step. New user questions come from support channels; add a scenario in the data module, don't grow the picture. Never introduce doc-only terminology the app doesn't show.

## Site layout on this branch

```text
docs/
├── docusaurus.config.ts     # two docs instances, mermaid, no blog
├── user-guide/              # plain-language, translatable
│   └── how-sync-works.mdx   # ← SyncExplainer
├── technical/               # architecture, integrations, dev setup
└── src/components/SyncExplainer/
    ├── index.tsx            # rendering (writers never touch)
    ├── syncScenarios.ts     # ALL explainer content
    └── styles.module.css
.github/workflows/docs.yaml  # build check on PR; Pages deploy on develop
```
