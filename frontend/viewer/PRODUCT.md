# Product

<!-- impeccable:product-schema 1 -->

Scope: the FieldWorks Lite (FW Lite) dictionary editor UI in `frontend/viewer/`. The LexBox web app has its own record in `frontend/PRODUCT.md`.

## Platform

web

One SvelteKit UI rendered in MAUI webviews (Windows, macOS, Android, iOS), in FwLiteWeb in a browser, and inside the Platform.Bible extension. The design language is web, not per-OS native; phone and desktop layouts are both first-class.

## Users

- **Language-community members**: native speakers and non-specialists contributing and checking entries, often less technical than FLEx users. Some hold the restricted Editor Lite role.
- **Field linguists**: trained lexicographers, many already FLEx users, doing serious dictionary work in the field.

Both often work on phones or low-end laptops with poor or no connectivity, and in UI languages other than English.

## Product Purpose

Let a language community build and maintain its dictionary together, on whatever device it has, with or without a connection, while keeping the data fully compatible with classic FieldWorks (FLEx). Success: people who would never open FLEx add, review, and discuss entries, and nothing they do loses or corrupts FLEx data.

## Positioning

An offline-first, CRDT-synced (Harmony) editor over the FieldWorks lexicon model. Each device holds a full local copy; changes merge through LexBox, which also bridges them to FLEx projects via Send/Receive. It is the lightweight, collaborative face of the same data FLEx edits, not a separate dictionary format.

## Operating Context

- Browse and edit entries, senses, and example sentences across vernacular and analysis writing systems; search, filter, sort by headword collation.
- Configurable views (which fields show, per role or task), tasks and review lists, a project dashboard, activity history with before/after diffs, and comment threads on entries/senses/examples.
- Explicit sync with LexBox; project download and roles come from LexBox accounts.
- Domain terms follow FieldWorks (entry, sense, gloss, definition, writing system, part of speech, semantic domain, complex form). The glossary in `backend/FwLite/CONTEXT.md` is authoritative.

## Capabilities and Constraints

- **Offline-first**: every editing flow works with no connection; sync is explicit, and its state and failures are visible and recoverable.
- **FLEx interop**: the data model and terminology stay faithful to FieldWorks. No UI may offer an edit that cannot round-trip to FLEx. Data-loss risk is the top concern (see `backend/FwLite/AGENTS.md`).
- **Any script, any UI locale**: vernacular text can be in any script or direction and uses per-writing-system fonts. UI strings are translated via Crowdin (currently en, es, fr, id, ko, ms, sw, vi); no layout may assume English length or Latin script.
- **Low-end devices**: phones and old laptops are first-class: touch targets, small screens, modest CPU and memory, large projects (thousands of entries) in virtualized lists.
- **Roles**: Manager, Editor, Editor Lite, Commenter, Observer; read-only roles must still read comfortably.
- Comment read state is local per device, not synced.

## Brand Commitments

Product names only: "FieldWorks Lite", short form "FW Lite". No external brand guide binds the UI.

## Evidence on Hand

- In-browser demo project (`src/project/demo/`), runnable at `/testing/project-view`; "Create Example Project" in FwLiteWeb.
- Real user projects are private. Never use their content, names, or stats in designs, screenshots, copy, or anything posted publicly.
- No testimonials, user research, or usage metrics are on hand; do not invent them.

## Product Principles

1. **Never risk the data.** Clarity about what is saved, synced, and shared outranks speed or flair.
2. **Works where the speaker is.** Offline, on a cheap phone, in their own UI language and script.
3. **Approachable without dumbing down the model.** Non-specialists get a guided path; linguists keep the full FieldWorks structure.
4. **Collaboration is visible.** Who changed what, what needs review, and what is being discussed should be easy to find.

## Accessibility & Inclusion

No formal standard has been set. Inclusion requirements already established: multilingual UI, any-script vernacular rendering, touch and keyboard use, light and dark themes.
