# Reviewer voice — how to file findings the team accepts

Short reference for the *tone* and *signal level* of polish findings.
Whatever the substance, the framing matters.

## Severity emoji prefixes (the team's actual convention)

@myieye uses these prefixes consistently in inline comments. Adopting them
in skill output makes findings feel familiar:

| Emoji | Meaning | When to use |
|---|---|---|
| ⛏️ | Nitpick | Style, tiny improvement, taste. Reviewer wouldn't block. |
| 🔧 | Refactor suggestion | "Consider extracting…" / "Could be cleaner with…" |
| ❓ | Question | "Why this approach vs. X?" — genuine uncertainty, not rhetorical. |
| 🤔 | Uncertainty | The reviewer isn't sure if this is right — "I might be missing context here." |

These map onto the severity ladder:

| Skill severity | Maps to |
|---|---|
| 🚫 blocking | No emoji needed; framing is "this will lose data" / "this breaks invariant X" |
| ⚠️ important | 🔧 refactor or 🤔 uncertainty when offering an alternative |
| 💭 nit | ⛏️ nitpick |
| ✨ praise | (no emoji; plain praise) |

## Framing rules

1. **Prefer questions over commands when there's room for the author to
   know better.** "Could `MorphType` here cause the legacy snapshot path
   to break?" beats "Add a fallback for legacy snapshots."
2. **Cite the source.** "Per `backend/FwLite/AGENTS.md` § Harmony Projected
   Tables" or "We hit this in #2202" is far more persuasive than "this is
   wrong."
3. **Don't be polite at the expense of accuracy.** A data-loss finding
   should be blunt: "This will lose data when X — see PR #2252."
4. **Be willing to be wrong.** End with "let me know if I'm missing
   context" on anything genuinely uncertain. Reviewers respect this.
5. **Praise specifically.** "Nice catch on the per-iteration try/catch in
   the reconnect loop — that exact pattern was missing in #2174." Vague
   praise reads as filler.

## Phrases to borrow

- "Missed a rename here: `m.MorphType` should be `m.Kind`." (rmunn, PR #2202)
- "I have the same comment, consider it implied further down." (rmunn —
   tolerant of repeated patterns, flag once and move on)
- "Looks pretty snazzy, but sadly, I don't think we need it." (myieye,
   PR #2192 — kindly rejecting over-engineering)
- "Chesterton's Fence — don't tear it down until you've checked it."
  (recurring justification when about to remove a parameter that "looks
  unused")
- "Does this exclude the deleted entries?" (jasonleenaylor — the team's
  standard CRDT-correctness probe)

## Phrases to avoid

- "MUST" / "REQUIRED" / "NEVER" in ALLCAPS — the team's `AGENTS.md` uses
  these sparingly and intentionally. Overusing them in findings makes the
  skill feel preachy.
- Long lecturing paragraphs. Reviewers skim. One short paragraph + a code
  block + a source citation is the format.
- Generic LLM hedging ("It might be worth considering whether perhaps…").
  Be direct.

## Reviewers to emulate

For weighting findings when synthesizing. Names below reflect the team as of
the skill's creation; if composition shifts, prefer the *patterns* over the
specific handles — re-run the PR-review-history pass to refresh.

- **@myieye** — primary taste-maker. Deepest opinions on:
  - Architecture (where checks belong; wrapper order)
  - Frontend idiom (Svelte 5 runes, no try/catch in viewer)
  - Naming (function names matching behavior; self-documenting parameters)
  - "Considered and rejected" sections (rewards thoroughness)
  Emoji habit: ⛏️🔧❓🤔 consistently.

- **@rmunn** — co-lead. Deep expertise in:
  - .NET / async / nullable / records
  - Sync correctness (two-pass invariant, ProjectSnapshot regeneration)
  - Mercurial / SendReceive / FwData
  - Catches missed renames exhaustively
  - Bash hygiene (`readlink -f`, `grep -c`, `set -e` vs. `set -o pipefail`)
  Tone: pedantic on correctness, kind on style; willing to debate himself
  mid-thread.

- **@jasonleenaylor**, **@hahn-kev** — occasional but high-signal on CRDT
  and FW data model.

- **@tim-eves** — `deployment/` and infrastructure. Pushes back on
  bundling unrelated infra changes in one PR (see #2222 → #2235 split).

- **@imnasnainaec** — `platform.bible-extension/` reviewer.

## Reviewers' pet peeves (don't ship code that triggers these)

- `try/catch` in viewer that swallows / logs only (@myieye, PR #2215).
- `DeletedAt is null` on a projected DbSet (codified post-PR #2286).
- Renaming a symbol but leaving stragglers in generated TS / snapshots
  (@rmunn, recurring).
- Adding a new sena-3 snapshot fixture for no clear reason (@myieye:
  "1 new one per PR at most", PR #2192).
- Conventional-commit prefix in PR title.
- Skipped tests without justification.
- Bundling unrelated infrastructure changes (@tim-eves).
- `.Result` / `.Wait()` in async-reachable code.
- Validation in the API class or sync helper instead of the wrapper.

## Reviewers' "neutrals" (don't manufacture findings on these)

- Comment density — team accepts both well-commented and sparsely-commented
  code as long as the *why* is captured somewhere (PR body, AGENTS.md,
  inline). Don't ding for "needs more comments" unless an actual readability
  problem.
- Test count — the team accepts no-new-tests PRs when justified. Don't ding
  for "missing tests" without naming a specific case the diff fails to cover.
- Conventional/idiomatic alternatives — only flag when there's a clear
  established pattern in the repo, not when there's a textbook "better way".
