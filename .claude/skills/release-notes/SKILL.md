---
name: release-notes
description: Generate user-facing FieldWorks Lite release notes in two formats at once — SIL community forum (Discourse markdown) and Google Play "What's new" (plain text, ≤500 chars). Use whenever the user asks for release notes, forum notes, Android/Play Store notes, "what's new", or notes for an upcoming release. Released mode reads the latest GitHub release; pre-release mode ("upcoming release", "I just pushed to main") reconstructs the changes from PRs merged since the last tag.
when_to_use: User asks for "release notes", "forum notes", "Android notes", "Play Store notes", "what's new for this release", or wants notes for a build just pushed to main before the GitHub release exists.
argument-hint: "[released | upcoming]"
allowed-tools: Bash(gh api:*) Bash(gh release:*) Bash(gh pr:*) Bash(git fetch:*) Bash(git log:*) Bash(git show:*) Bash(git tag:*) Bash(git rev-parse:*) Read Glob Grep WebFetch Agent
---

# FieldWorks Lite release notes

Produce user-facing release notes for a FieldWorks Lite release and deliver **three copy/paste-ready blocks directly in chat** (never a .md file):

1. **Version tag** — the full tag including the commit hash (e.g. `v2026-06-24-8cfbce9c`). In pre-release mode this is predicted; label it clearly as *predicted*.
2. **Forum release notes** — Discourse markdown (format below), pasted into the SIL community forum topic (https://community.software.sil.org/t/10807 — id-only URL; the slugged form embeds a version and goes stale).
3. **Android release notes** — the Google Play "What's new" text (plain text, emoji per item, platform-filtered, ≤500 chars).

Produce both note sets in the same pass while the PR list is in context — platform filtering for Android needs the PR details, not just the forum wording.

The hard part is the filter: include everything a user would care about, and nothing else. A release with ~25 PRs often yields only ~6–8 posted bullets. A human reviews the draft, so aim for a good draft fast, not a perfect one at high cost.

## Two modes

- **Released mode** — the GitHub release exists. Build notes from its FieldWorks Lite section.
- **Pre-release mode** — the user wants notes before CI finishes and the release exists (e.g. so the Android build can go to Google Play early). Reconstruct the FieldWorks Lite section from PRs merged since the last tag. This is the mode when the user says "upcoming release" or "I just pushed to main".

## Step 1: Collect the FieldWorks Lite changes

### Released mode

```bash
gh release view --repo sillsdev/languageforge-lexbox --json tagName,publishedAt,body
```

Sanity-check `publishedAt` against today — you want the genuinely newest release. Extract only the **FieldWorks Lite** section of the body. Ignore the **Lexbox** and **Other Stuff 🤔** sections entirely — server-side or developer changes, never user-facing.

(If `gh` is unavailable, WebFetch `https://github.com/sillsdev/languageforge-lexbox/releases/latest` — that URL redirects to the real newest tag. Don't trust the `/releases` listing page; it can be served stale.)

### Pre-release mode

1. Get the baseline: the latest release tag (as above).
2. List PRs merged since that tag. PRs squash-merge to `develop` and `main` mirrors it, so PR numbers appear as `(#NNNN)` in commit subjects:

   ```bash
   git fetch origin main
   git log <TAG>..origin/main --oneline
   ```

   (or `gh api repos/sillsdev/languageforge-lexbox/compare/<TAG>...main` when the local clone is inconvenient).
3. Categorize each PR the way GitHub release notes would, per `.github/release.yml`: label `💻 FW Lite` → **FieldWorks Lite**; `📦 Lexbox` → Lexbox; everything else (incl. dependabot) → Other Stuff. First category wins, so a PR with both FW Lite and Lexbox labels counts as FieldWorks Lite. Batch the label lookups:

   ```bash
   gh pr view <N> --repo sillsdev/languageforge-lexbox --json number,title,labels
   ```
4. Predict the version tag as `v<YYYY-MM-DD>-<sha8>` from the expected release date and `git rev-parse --short=8 origin/main`. Caveats to state with the prediction: tag dates are UTC, so a build kicked off late in the day (Europe) may tag the next day; and if anything else lands on main before CI runs, the hash changes.

From here both modes are identical.

## Step 2: Research ambiguous PRs — sparingly

Many PR titles are self-explanatory ("Add activity filters to the activity view"). Only look up a PR when the title genuinely doesn't tell you what changed for the user (e.g. "Comments") — aim for ~3–5 lookups, not all of them. Skip obvious non-starters (package bumps, CI, tests) without fetching.

```bash
gh pr view <N> --repo sillsdev/languageforge-lexbox --json title,body,labels
```

The body usually has a plain-English summary; don't judge from the title alone when the title is technical or misleading. Don't second-guess a reasonable draft with extra fetches or verification passes — deliver, and let the reviewer adjust wording.

## Step 3: Feature-flag check

A merged PR is not the same as a shipped feature. FieldWorks Lite gates unreleased UI behind release-channel feature flags; the registry is `CHANNEL_FLAGS` in `frontend/viewer/src/lib/feature-flags/feature-flags.ts` (production is the empty channel and has no flags; features ship by *deleting* their flag). Read it at both ends of the release:

```bash
git show <TAG>:frontend/viewer/src/lib/feature-flags/feature-flags.ts        # released mode; pre-release mode uses origin/main (the tag doesn't exist yet)
git show <PREV_TAG>:frontend/viewer/src/lib/feature-flags/feature-flags.ts   # previous release (the baseline tag from Step 1)
```

- **Flag present at `<TAG>`** → that feature is invisible to users. Exclude every PR whose user-facing surface is behind it — including satellite items like fixes or panels that only exist inside the flagged UI. When unsure whether a PR's UI is gated, grep it for `hasFlag(` / `<FlagContent`.
- **Flag present at `<PREV_TAG>` but gone at `<TAG>`** (and not renamed) → that feature ships in THIS release. Announce it now, even though its PRs merged releases ago — pull the wording from the original feature PRs.

There's also per-project gating (`SupportedFeatures()` in `MiniLcmJsInvokable.cs`), but that's project-type capability (e.g. CRDT-only), not a release channel — it usually doesn't change what gets announced.

## Step 4: Filter ruthlessly

Include only items a typical user would notice, or that fix something a user could plausibly encounter. Target 5–10 forum bullets total — and lean toward the LOW end. Be more ruthless than feels natural. When unsure whether an item earns its own bullet, fold it into "Various other small bug fixes".

**Bias strongly toward cutting / folding** (each of these was individually drafted, then correctly removed from a real posted list):

- Cosmetic / styling tweaks (e.g. "Dictionary Preview styling") → fold into "Various other small bug fixes"
- Niche dialog behavior most users won't hit (a dialog accidentally dismissed, a recording dialog's writing-system selection) → fold or drop
- Multi-window / "open in new window" behavior → usually drop (few users use it)
- Error-*visibility* plumbing ("errors now show as a toast", "surface batch errors") → drop; essentially internal even though it sounds user-facing

**Bias toward KEEPING and surfacing prominently:**

- App freezes, hangs, and crashes — including race conditions that manifest as the app freezing (a race opening an entry → "Fixed app sometimes freezing when opening an entry"). Don't dismiss these as rare edge cases.
- Sync/data-correctness fixes, especially follow-ups fixing something broken by a feature in a recent release.

**Keep qualifiers minimal:** state the single most relevant condition, not every one ("Troubleshoot dialog not opening on broken projects" — not "on mobile or for broken projects").

**Always skip:**

- Dependency / package bumps
- CI, build system, or dev tooling changes
- Test additions or refactors
- Internal refactors with no visible effect
- Documentation or code comments
- Developer-only features (dev flavors, dev toasts, dev task variants)
- Example project changes
- Logging improvements (unless errors now surface visibly to users)
- Crash fixes that only occur in rare edge cases users wouldn't notice
- Anything in the Lexbox or Other Stuff sections
- Invisible under-the-hood data changes (e.g. internal text normalization) with no visible effect — unless it's a notable named capability worth flagging (e.g. homograph sorting)

**Include if user-facing:**

- Analytics/telemetry additions or changes — ALWAYS disclose, stating what is collected and how to opt out (with a privacy-policy link in the forum notes). Transparency matters even when nothing visible changes, precisely because users can turn it off.
- New visible features or UI elements
- Changes to existing behavior users rely on
- Performance improvements users would feel
- Bug fixes for things users would actually encounter
- Sync fixes that caused data to accumulate incorrect changes (these matter even if subtle)
- Fixes for things that were broken in a previously released feature

**Tricky cases:**

- "Foundational" changes that enable a future feature: include with a note like "(not displayed yet)"
- One-time migration costs on first open: always include a parenthetical, e.g. "(this will cause a one-time delay the first time each project is opened)"
- Crashes: only include if reliably reproducible by normal users doing normal things
- Android-specific fixes: include if they affect normal usage on Android

## Platform marking — drives the Android subset

Some items only apply to certain platforms. Mark them inline in the **forum** notes; items carrying either of these two markers are dropped from the **Android** notes (the "(Android only)" tag from Step 6 is the opposite — those stay):

- **Desktop / Windows-only** items (somewhat rare): mark " (Windows only)".
- **fw-headless items — the FieldWorks Lite ↔ FieldWorks Classic (FwData) sync**: mark " (FW Lite & FW sync)". That sync runs server-side/desktop via fw-headless, so Android users don't run it.

Live/CRDT sync via the Lexbox server (SignalR reconnection, live update notifications) is NOT fw-headless — it applies to Android and stays in the Android notes. The forum text alone can't tell you platform; the PRs (labels, descriptions, screenshots) can — which is why both note sets come from the same pass.

## Step 5: Forum notes

```markdown
## Version: vYYYY-MM-DD
#### ✨ New Features
* item

#### 🚀 Improvements
* item

#### 🐛 Bug Fixes
* item
```

- The forum `## Version:` header uses the **date only** — the commit hash appears only in the version-tag block of your chat reply, not in the forum header.
- Only include sections that have items; order New Features → Improvements → Bug Fixes; biggest impact first within each.

**Language style:**

- Sentence case, plain English — no PR numbers, author names, class names, or internal jargon
- Write from the user's perspective: what changed for them, not what the code does
- Bug fixes start with "Fixed"/"Fix"; features are a noun phrase or short sentence; improvements use active present tense ("View mode is now remembered per project")
- Parenthetical caveats when needed (migration cost, known limitation, "not displayed yet")
- Don't over-claim: groundwork for a future visible feature is described as such

**Good entries (from real releases):**

- `Added morpheme type markers to headwords (e.g. dashes "-" on affixes) (this will cause a one-time delay the first time each project is opened)`
- `View mode (simple/preview) and dictionary preview pin are now remembered per project`
- `Fixed complex form components accumulating spurious ordering changes on each sync`
- `Fixed app freezing during sync`

**Dropped entries (too internal):** "Move FwLite and Lexbox to .NET 10", "Bump pretty much ALL packages", "Add Android Dev flavor", "Use named GUIDs for predefined-data seed commits", "Silence update-available toast in dev builds", "Make example project slightly more realistic".

Full worked examples: [references/example-forum-notes.md](references/example-forum-notes.md).

## Step 6: Android (Play Store) notes

A tightened, platform-filtered subset of the forum notes — same research, not a separate effort. Target style: [references/example-android-notes.md](references/example-android-notes.md).

- **Hard cap 500 characters** (Google Play limit). Real posted notes are often just 1–5 short lines.
- Keep only **headline** items; fold the long tail into one line ("Numerous small bug fixes"). Drop secondary improvements that earned a forum bullet but aren't headline-worthy on mobile.
- Drop everything platform-marked in the forum notes (Windows-only, FW Lite & FW sync). Tag Android-specific fixes "(Android only)".
- Pure-maintenance releases (package bumps) get no Android note at all. When a release is thin, a couple of lines is fine — don't pad.
- Cadence isn't 1:1 with the forum: Android sometimes ships hotfixes that never get a forum post. Write notes for the build actually being released.

**Format** — plain text; Play Store renders no Markdown, only emoji and literal line breaks:

- One emoji per item: ✨ new feature, 🚀 improvement, 🐛 bug fix; one item per line
- Blank line between emoji groups; groups ordered ✨ → 🚀 → 🐛, biggest first within each
- Disclosures that are none of the three (e.g. an analytics/telemetry notice) go last as an ℹ️ line
- Wording tighter than the forum version; "Fixed" past tense, consistent within the batch; keep essential parentheticals but trim them hard

**Checklist before delivering:** every item applies to Android · Android-only fixes tagged · under 500 characters (count it) · long tail folded · headline items first · tense/phrasing consistent.
