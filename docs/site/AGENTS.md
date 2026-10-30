# Docs site

Docusaurus site with three sections: `fw-lite/` (FieldWorks Lite users), `lexbox/` (lexbox.org and Send/Receive users) and `technical/` (how the system works, for anyone curious; not how to develop it). Everything in this folder is published. Developer docs (setup, troubleshooting, ADRs) go in `docs/dev/`, CI/CD in `.github/AGENTS.md`. Check a change with `pnpm install && pnpm build` in this folder; broken links and anchors fail the build. `pnpm typecheck` covers the React components.

## Which page to update

| You changed | Update |
|---|---|
| FW Lite UI (`frontend/viewer/`) | the `fw-lite/` page that names it; Sync dialog changes also `src/components/SyncExplainer/syncScenarios.ts` |
| When sync runs or which edit wins (`backend/FwLite/`, `backend/FwHeadless/`) | `fw-lite/how-sync-works.mdx`, `syncScenarios.ts`, `lexbox/fw-lite.md`, `technical/sync/` |
| lexbox.org UI, roles, invites, orgs, project requests (`frontend/src/`, `backend/LexBoxApi/`, `backend/LexCore/`) | the `lexbox/` page that names it |

`git grep` the old label or behavior under `docs/site/` to find every page that mentions it.

## Adding pages

- A feature gets its own page in its section (e.g. `fw-lite/views.md` for choosing and customizing views), titled by what the user does, with headings that answer the questions support gets. Order it with `sidebar_position` between the existing pages; the FAQ sits last at 99.
- No stub pages: every page, section index pages included, must answer something on its own (at least a short intro plus content or a contents list). Fold thin content into its parent page instead.
- Add the page to its section's `index.md` "In this guide" list. Once a section has more than about six feature pages, group them in a folder with a `_category_.json`.
- Downloads always link to `https://lexbox.org/fw-lite`, never to files. Only exception: the Mac early-tester build (GitHub releases), with its warning.

## Writing rules

- Name buttons, menus and tabs exactly as the app shows them: FW Lite text is a `msgid` in `frontend/viewer/src/locales/en.po`, lexbox.org text a value in `frontend/src/lib/i18n/locales/en.json`. `fw-lite/` says FieldWorks Classic (FW Lite's name for the desktop app); `lexbox/` says FieldWorks (lexbox.org's name). No doc-only names.
- `fw-lite/` and `lexbox/` are for non-technical readers: plain words, short sentences, no em-dashes. CRDT, Harmony, Mercurial and FwHeadless belong in `technical/` and the "Under the hood" box. Call the sync engine Harmony; say CRDT only where you explain what Harmony is.
- Say who can see a feature when not everyone can (role, beta flag, admin only).
- No real project codes, names, people or project data, in text or screenshots. Example project codes use the private-use language code `qaa`. Screenshots come from FW Lite's example project or a local dev Lexbox.
- `technical/` explains how the system works; `AGENTS.md` files keep agent procedure (commands, gotchas) and link here. The docs never link to `AGENTS.md` files. Don't copy content between them.
- The sync explainer's limits are in the `syncScenarios.ts` header.

## Links must keep working

Support replies link to pages, headings and explainer questions (`/fw-lite/how-sync-works#not-seen`: scenario ids in `syncScenarios.ts` are URLs). The apps are meant to link to `/fw-lite/` (FW Lite help), `/lexbox/` (lexbox.org help), `/fw-lite/how-sync-works` (Sync help) and `/lexbox/fw-lite` (turning on FW Lite).

- Moving or renaming a page: add `@docusaurus/plugin-client-redirects` with a redirect from the old path.
- Rewording a heading changes its anchor. Keep the old one with `## New wording {/* #old-anchor */}` (`{#id}` doesn't build here because of `future.v4`).
- Don't rename a scenario id.

Decisions behind the site: `docs/dev/adr/0002-docs-site.md`.
