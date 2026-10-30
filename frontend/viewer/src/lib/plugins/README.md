# FW Lite plugins

A plugin is **one self-contained HTML file**, written by a project member (usually with AI help),
that runs inside FW Lite and works with the open project's dictionary data. The `Plugin` CRDT
entity carries only metadata (name, description, extracted manifest, file reference); the HTML
itself is stored as a **media resource** — the same Harmony `RemoteResource` path audio and
pictures use — so the bytes upload/download on demand, work offline once cached, travel through
Mercurial send/receive in the project's `LinkedFiles`, and are visible to FieldWorks as ordinary
(inert) linked files. Plugin files are immutable: each edit uploads a new content-named file and
repoints the entity, so synced/cached copies can never go stale.

## Architecture

```mermaid
flowchart LR
    subgraph iframe [sandboxed iframe — opaque origin]
        SDK[plugin-sdk.js<br/>window.fwlite] --> P[plugin HTML/JS]
    end
    SDK <-- postMessage --> HOST[plugin-host.ts]
    HOST --> ADAPTER[plugin-api-adapter.ts<br/>plugin API v1]
    ADAPTER --> API[IMiniLcmJsInvokable]
    ADAPTER --> CONFIRM[write-confirm dialog<br/>live: apply · preview: show only]
    ADAPTER --> STORE[plugin-local-data.ts<br/>localStorage]
    SVC[plugin-service.svelte.ts] --> API
    SVC --> FILES[media resources<br/>getFileStream / saveFile]
```

## Pages

- **`PluginsView`**: the project's plugins (Run, Edit, Export, ...) and, for managers only, a shelf of
  the bundled `examples/`. Nothing is in a project until a manager adds it.
- **`PluginRunView`** (`/plugins/:id`): runs a saved plugin `live`, behind the first-run consent card.
- **`PluginStudioView`**: one page for trying an example (`/plugins/examples/:key`), building a new
  plugin (`/plugins/new`) and editing one (`/plugins/:id/edit`); managers only. The draft runs in a
  `preview` sandbox on the left; tabs on the right hold the AI loop (copy a prompt, paste the reply),
  the source and the details. An example changed before adding is saved as "… (custom)". A pasted
  version is reviewed against the saved plugin or example: new permissions are flagged, with a
  follow-up prompt asking the AI to remove them. Drafts aren't kept: leaving with unsaved work asks
  first (`$lib/utils/leave-guard.ts` covers the sidebar).
- **`PluginSandbox`**: the iframe, host wiring, write dialog and navigation teardown, shared by the
  run view and the studio. Its `writeMode` is the only difference between them.

## Modules

- **`plugin-manifest.ts`** parses what a plugin declares about itself in `<meta name="fwlite-plugin-*">`
  tags: `permissions` (`internet`, `edit`), `contexts` (`entry`), `requires` (`comments`, `history`).
  The manifest is extracted into the entity at save time (so lists and the entry menu never need the
  file) and re-parsed from the actual HTML wherever it matters for safety.
- **`plugin-service.svelte.ts`** owns persistence: uploading the HTML as a content-hash-named media
  file, building the entity, fetching HTML back (with offline/error outcomes), and carrying
  device-local grants across local edits.
- **`plugin-srcdoc.ts`** composes the document the iframe runs: injects the SDK and, unless the
  plugin declares the `internet` permission, a CSP `<meta>` that blocks network fetches. The result
  is loaded via a revocable Blob URL (not a `srcdoc` attribute or retained state) so multi-MB plugin
  HTML stays GC-able; the on-disk cache file remains the source of truth. The iframe uses
  `sandbox="allow-scripts allow-forms allow-modals allow-downloads"` — critically **without**
  `allow-same-origin`, so the plugin gets an opaque origin: no cookies, no app localStorage, no
  `window.parent` DOM access. One caveat the code and copy are honest about: nothing can stop a
  script from navigating its own frame (a potential exfiltration channel), so PluginSandbox detects
  a second document load and stops the plugin.
- **`plugin-sdk.js`** is injected verbatim into every plugin and exposes the `fwlite` global.
  It is plain dependency-free JS by design.
- **`plugin-host.ts`** answers exactly one iframe (verified by window identity), forwards requests
  to the adapter, and caps in-flight requests. RPC errors are returned to the plugin, not the app's
  error handler.
- **`plugin-api-adapter.ts`** is the *entire* API surface plugins can reach (v1): the
  `PluginApiMethods` interface, an arg-decoder table (both compiler-checked to stay in sync), and
  the implementation. Reads are open; writes require the `edit` permission AND per-operation user
  approval with a complete field-level preview. In `preview` write mode (the studio) every write is
  validated and shown the same way, then rejected with code `preview` and never applied; `saveFile`
  rejects, and plugin storage lives in memory. `updateEntry` has compare-and-swap semantics
  (`conflict` error) so the preview provably matches the real effect. Comments/history are
  capability-gated areas a host may not support. Filters are structured — the gridify syntax is
  not exposed.
- **`plugin-local-data.ts`** provides per-plugin key-value storage (plugins have no storage of
  their own) and the two device-local, content-hash-pinned grants: run consent and write trust
  ("Always allow"). A local edit re-pins run consent but drops "Always allow" (granted to the old
  code); changes arriving via sync ask again.
- **`plugin-prompt.ts`** generates the project-aware AI prompt (writing systems, parts of speech,
  domains, entry count baked in), closed by either a new plugin's description or, to change a
  plugin, its current HTML plus the requested change. It doubles as the public API documentation — **keep it in sync
  when changing the SDK or adapter**.

## Versioning rules

The plugin API is a public contract: team members will have working plugins stored in their
projects. For `fwlite` (SDK + adapter):

- Adding methods/fields is fine.
- Never remove or change the behavior of an existing method within v1; if unavoidable, bump
  `PLUGIN_API_VERSION` and keep v1 behavior for plugins that expect it.
- The `Plugin` CRDT entity and its change classes are forever — see `backend/FwLite/AGENTS.md`.
- New optional host features go through `capabilities` + the `requires` manifest, so plugins can
  run on hosts that lack them (e.g. a future FieldWorks host without comments/history).

## Security model (summary)

1. Opaque-origin sandbox — no app/API/cookie access, only postMessage.
2. Offline by default — CSP blocks network fetches unless the plugin declares `internet` (surfaced
   as a badge in the UI and on the consent screen); self-navigation can't be blocked, so it is
   detected and the plugin stopped.
3. Consent per content hash (name + description + HTML) before first run on each device.
4. Reads are open (same data the user can already see). Writes need the declared `edit` permission
   and are user-approved per operation — or auto-approved with ambient notifications once the user
   picks "Always allow for this plugin" (hash-pinned, per project, per device, revocable from the
   plugin card and the run toolbar, and dropped when the plugin's code changes).
5. Write previews are complete and honest: full recursive diffs against the entry's REAL current
   state (compare-and-swap), no silent truncation, bidi/control characters stripped.
6. Only project managers can add/edit/delete plugins (enforced in `CrdtMiniLcmApi`).

## Testing

`frontend/viewer/tests/ui/plugins.test.ts` runs against the in-browser demo project
(`task test:ui-standalone -- plugins`), covering consent, the postMessage bridge, write
approval/trust, preview writes, the examples shelf and the studio flows (new, customized example,
edit with a new permission, leave guard). It also runs every example in the studio preview. The demo
project seeds two example plugins (see `examples/`), and `__PLAYWRIGHT_UTILS__.setProjectRole`
turns its user into a non-manager.
Unit tests live next to their modules (`*.test.ts`).
