# PR narrative — title & body style

Read this before filing findings on the PR title/body (or to suggest one
when no PR exists yet).

## Title

- **Imperative, sentence case.** "Fix race condition opening entry in FW",
  not "Fixed race…" or "fix: race condition…".
- **Under ~70 characters.** Anything longer truncates in lists.
- **No conventional-commit prefix in the title.** No `feat:` / `fix:`. The
  team's history is consistent: see PR #2298, #2295, #2284, #2282.
- **What, not why.** The body explains why; the title summarizes the change.

Counter-examples to fix:
- `feat: add morph type support` → `Add morph type support`
- `bug fix` → too vague; name what was broken
- `WIP - more work to do later` → indicates it's not ready; suggest waiting
  to open the PR or marking as draft.

## Body skeleton

The team's recurring sections, in order:

1. **Lede (1–2 sentences).** What problem was solved or what was added.
   Plain prose, no preamble.
2. **Bullet list of changes** when more than one area touched. Each bullet:
   - File/symbol in backticks
   - Sentence describing the change, focused on *why*
   - Example: `` - **`LocalMediaAdapter`**: cache audio file paths so
     re-renders don't re-fetch ``
3. **Test plan** as a markdown checklist when behavior changes:
   ```
   ## Test plan
   - [x] Ran `dotnet test FwLiteProjectSync.Tests --filter Sena3` locally
   - [x] Verified entry creation in FwLite Web
   - [ ] Verify on Android device
   ```
   `[x]` for what's done; `[ ]` for manual steps the reviewer or release
   testing should hit.
4. **Screenshots Before/After** for any visual change. Use `<img>` tags
   with explicit `width=` for predictable rendering:
   ```html
   <img width="400" alt="Before" src="…" />
   <img width="400" alt="After" src="…" />
   ```
5. **Issue references.** `Resolves #2150`, `Fixes most of #2263`, inline
   `See #2290`, `#2291`. Put issue links inline in the lede when natural.
6. **"Considered and rejected"** section for non-trivial design choices.
   Lists each alternative with a one-line "why not". PR #2285 is the gold
   standard.
7. **Forward-looking hooks** when relevant: "Hopefully future PRs will add
   more radical speedups" (PR #2254), or "see issue #2291 for the kill-
   switch checklist when upstream PR linq2db#5546 ships" (PR #2282).

## When tests are skipped

Acceptable if explicitly justified in the PR body. Example from PR #2298:

> No new tests — each case is timing-dependent and a deterministic repro
> isn't worth its own scaffolding. Build is clean.

Not acceptable: silently skipping tests for a behavior change. Reviewers
will ask.

## Screenshots

For any UI change, even small ones, include Before and After. PR #2270 did
this for a one-line CSS tweak — the reviewer immediately understood the
fix.

Pasting screenshots from local: drag-drop into the GitHub editor. The
upload becomes a GH-hosted URL.

For diffs the reviewer needs to compare side-by-side, use a 2x1 grid of
`<img width="...">` tags.

## Commit messages

- **Imperative, sentence case** titles (~50–70 chars).
  `Slim down test comments` ✓
  `Brand-color the system-bar gutter on Android edge-to-edge` ✓
- **Multi-paragraph body** for non-trivial commits. Explain *why*, not what.
- Co-author trailer when AI-assisted:
  ```
  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
  ```
  Note: the trailer should match the model running. Don't hard-code; the
  CLI fills it.
- Session link trailer is fine but not required:
  `https://claude.ai/code/session_…`
- The squash merge will pick up the PR title automatically (GitHub default).

## CI checks to verify before requesting review

Required (~23–28 jobs per PR; the badges you must see green):

- `check-and-lint` (frontend ESLint + svelte-check)
- `frontend` (build)
- `frontend-component-unit-tests`
- `Build FW Lite and run tests`
- `Build API / publish-api`, `Build UI / publish-ui`, `Build FwHeadless / publish-fw-headless`
- `Analyze (csharp)`, `Analyze (javascript-typescript)`, `Analyze (actions)` (CodeQL)
- `Publish FW Lite app for Linux / Mac / Windows / Android`

If a CI job is red, fix it before requesting review — don't ask the team to
review on red. If a job is flaky, mention it inline ("retried twice; this
job is flaky on develop too — see #NNNN").

