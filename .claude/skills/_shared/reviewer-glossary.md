# Review voice

Reference for tone and signal level. Patterns are mined from real review
threads; names are omitted because the codebase outlives team composition.

## Severity emoji prefixes

The team uses these in inline comments. Mirror them in skill output.

| Emoji | Use |
|---|---|
| ⛏️ | Nitpick |
| 🔧 | Refactor suggestion |
| ❓ | Question (genuine uncertainty) |
| 🤔 | "I might be missing context here" |

Severity mapping: 🚫 blocking · ⚠️ important (🔧/🤔) · 💭 nit (⛏️) · ✨ praise.

## Collective voice

Reviewers write as **co-owners**, not as authority correcting authors.

- **`let's …`** — the default opener for prescriptive nits. Use it.
- `I'd suggest …` — stronger preference, narrower scope.
- `should probably …` — softener for a clear correction.
- `Hum, …` — when reconsidering mid-comment.
- `why …?` — leading question to expose flawed reasoning. Sharp tool;
  use sparingly.
- Short, declarative, no hedging for clear corrections.
- Concrete suggestions almost always come with a code block. If it fits
  in 3 lines, write it inline.

Avoid: ALLCAPS MUSTs, lecturing paragraphs, LLM hedging (*"It might be
worth considering whether perhaps…"*), and *"you should …"* (replace
with *"let's …"*).

## Framing rules

1. Prefer questions over commands when there's room for the author to
   know better.
2. Cite the source — PR number, `AGENTS.md` section, or an existing file
   as precedent. *"take a look at `UpdateSense`, copy how that works."*
3. Be willing to be wrong. End with *"let me know if I'm missing
   context"* on genuine uncertainty.
4. Praise specifically. Vague praise reads as filler.
5. Don't be polite at the expense of accuracy. Data-loss findings are
   blunt.

## Voices to channel (in order of weight)

Distinct standards held by different reviewers. The skill embodies the
patterns; the specific rules live in the references each voice maps to.

1. **CRDT/Harmony ownership** — heaviest weight. Resource lifetime, DI
   safety, threading honesty, API stability, framework primitive
   correctness, conditional performance, true cancellation, atomic
   operations, DbContext re-use, behavior-on-records-without-leaking-
   state, defense-in-depth. Full checklist in
   `.claude/agents/dotnet-stylist.md`.
2. **Sync correctness** — async/nullable rigor, naming consistency
   (catches partial renames in test data, generated TS, snapshots),
   two-pass sync invariant, bash hygiene, edge-case depth. Full
   checklist in `.claude/agents/fwlite-sentinel.md` (shell rules in
   `.claude/agents/bash-discipline.md`).
3. **Architecture / frontend taste** — Svelte runes, no try/catch in
   viewer, naming matches behavior, validation in the wrapper layer,
   self-documenting parameter names. Full checklist in
   `.claude/agents/viewer-watcher.md`. Uses the ⛏️/🔧/❓/🤔 prefixes.
4. **Deployment / infra** — `deployment/`, workflows, Dockerfile. Pushes
   back on bundling unrelated infra into one PR (PR #2222 → split #2235).
5. **CRDT-correctness probe** — recurring question on any new
   query/projection code: *"Does this exclude the deleted entries?"*
   Use it on every query change; near-100 % hit rate.
