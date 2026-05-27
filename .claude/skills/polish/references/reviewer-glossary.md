# Review voice — how to file findings the team accepts

Short reference for the *tone* and *signal level* of polish findings.
Whatever the substance, the framing matters. The patterns below are mined
from real review threads; the people who originated them aren't named here
because the patterns matter, not the handles, and the codebase outlives
team composition.

## Severity emoji prefixes

The team uses these prefixes in inline comments. Adopting them in skill
output makes findings feel familiar:

| Emoji | Meaning | When to use |
|---|---|---|
| ⛏️ | Nitpick | Style, tiny improvement, taste. Wouldn't block. |
| 🔧 | Refactor suggestion | *"Consider extracting…"* / *"Could be cleaner with…"* |
| ❓ | Question | *"Why this approach vs. X?"* — genuine uncertainty. |
| 🤔 | Uncertainty | *"I might be missing context here."* |

Severity mapping:

| Skill severity | Maps to |
|---|---|
| 🚫 blocking | No emoji needed; frame as *"this will lose data"* / *"breaks invariant X"* |
| ⚠️ important | 🔧 refactor or 🤔 uncertainty when offering an alternative |
| 💭 nit | ⛏️ nitpick |
| ✨ praise | (no emoji; plain praise) |

## The team's collective voice

The team writes review comments as **co-owners** of the codebase, not as
individuals correcting authors. Findings open with collective framing:

- **`let's …`** — the default opener for prescriptive nits. *"let's use
  `ICollection<T>` instead"*, *"let's drop the server"*, *"let's tweak
  these error messages a bit"*. Use this as the skill's default voice for
  changes you'd apply.
- **`I'd … / I'd suggest …`** — stronger personal preference, narrower
  scope. *"I'd just put it below the FieldWorksLiteProject record."*
- **`I think we should …`** / **`I think let's not …`** — moderate
  strength when there's a real trade-off.
- **`should probably`** — softener for a clear correction. *"should
  probably actually check if the folder exists."*
- **`Hum, …`** — when reconsidering mid-comment. Authentic; use it when
  changing direction within a finding.
- **`why …?`** — leading question to expose flawed reasoning. *"why was
  the LogFileName parameter removed?"*, *"why are there 2 ways to provide
  metadata?"*, *"why's the Id excluded?"*. Sharp tool — use sparingly.
- **Short, declarative, no hedging** for clear corrections — *"drop this
  line"*, *"this is on the wrong line"*, *"should be disabled when
  loading"*.

Concrete suggestions almost always come with a code block. If the
alternative fits in 3 lines, write it inline.

## Framing rules

1. **Prefer questions over commands** when there's room for the author to
   know better.
2. **Cite the source.** PR number, `AGENTS.md` section, or a specific
   existing file as precedent — far more persuasive than *"this is wrong."*
3. **Reference existing patterns by file name.** *"take a look at
   `UpdateSense`, copy how that works"* — the team learns from precedents
   rather than reinventing.
4. **Don't be polite at the expense of accuracy.** A data-loss finding
   should be blunt: *"This will lose data when X — see PR #2252."*
5. **Be willing to be wrong.** End with *"let me know if I'm missing
   context"* on genuine uncertainty.
6. **Praise specifically.** *"Nice catch on the per-iteration try/catch in
   the reconnect loop — that pattern was missing in #2174."* Vague praise
   reads as filler.

## Voices to channel (in order of weight)

The skill embodies these distinct review voices. They're defined by the
*standards they hold*, not the people who hold them.

### Voice 1 · The CRDT/Harmony ownership voice  (heaviest weight)

The substrate author voice — the person who wrote the CRDT library this
codebase rests on, and most of the FwLite sync surface. This voice is
**decisive, not consultative**, on the standards below. When a finding
involves any of them, weight it as 🚫 blocking or ⚠️ important by default,
and frame it bluntly with *"let's …"* and a code block.

Standards they hold:

- **Resource lifetime / disposal.** `using` (or `await using`) around
  anything that can throw mid-operation. *"You should use a `using` block
  as this code will not run if `CopyToAsync` throws."*
- **DI safety.** Value types and constants don't go in the container —
  *"something somewhere could accidentally change it by registering
  `NormalizationForm` in the IOC container"*. Use `const` / `static
  readonly` for those.
- **Threading model honesty.** `Task.Run` only to escape the UI thread
  (MAUI/Blazor `JSInvokable` runs on UI thread). Anywhere else it's noise
  hiding the fact that the underlying code isn't actually async.
- **API stability across deployed clients.** *"I'd like to avoid breaking
  any existing clients, can we make a new API which returns the new
  ListProjectsResult and preserve this API. … we can always remove
  `listProjects` in a month or so when it's reasonable to expect people to
  have upgraded."* Default to `*V2`-style additive evolution.
- **Framework primitive correctness.** `IOptions<T>` for typed config
  binding, not raw `IConfiguration` (*"options are how you consume
  configuration in a type-safe way"*). `JsonSerializerOptions` cached at
  module load (the options instance caches reflection metadata).
- **Conditional performance.** *"I still want to do the filtering in
  Postgres, but only when `keys.Length` is < 100 or something like that."*
  No blanket "always pushdown / never pushdown" rules.
- **`ICollection<T>` over `IEnumerable<T>`** when consuming. *"When
  consuming an `IEnumerable` it's not safe to assume that you can iterate
  it more than once."*
- **Behavior on records / instance methods**, not detached static
  helpers — *but* never let DI-only or progress-tracking state leak into
  serializable shapes. Use a second constructor for that.
- **HTTP status correctness.** *"this API currently always returns ok, we
  should at the very least return a 500 if the result is not success."*
  *"can we use `StatusCodes.Status406NotAcceptable` here?"* Never 200-OK
  with an error body.
- **Backend defense-in-depth.** *"shouldn't there be a check here that the
  project already exists? or does that happen on the frontend?"* →
  *"there's a check on the frontend … but a backend check is probably a
  good idea also in case of frontend bugs."*
- **True cancellation via `CancellationToken`**, not a cancellation flag.
  *"this isn't actually going to cancel the job, all this does is tell
  anyone awaiting the job that it's cancelled."*
- **Atomic operations.** *"this and the previous block should be done all
  at once, we shouldn't be saving changes twice if the data is missing."*
  One `SaveChangesAsync` per logical unit.
- **DbContext re-use.** *"the way this works now 2 db contexts are
  created once for the first get publication call and once for the
  second."* — flag dual contexts in a single logical op.
- **i18n toolchain awareness.** Lingui parses templates at build time;
  runtime-only string assembly is invisible to the parser. Structure
  roles/labels so each translatable phrase is a literal at extract time.
- **Auth at the boundary.** *"auth should be handled in the LexBox API,
  so before the request is forwarded."*
- **Polling loops for long-poll endpoints** to avoid 30 s Cloudflare
  timeouts.

Opener: **`let's …`**. Frequently cites existing files by name as the
authoritative precedent.

### Voice 2 · The sync-correctness voice

The reviewer who knows the .NET / Chorus / Mercurial / CRDT-merge layer
cold. Pedantic in `.cs` reviews. Standards:

- Async/nullable correctness; no `.Result` / `.Wait()`.
- Naming rigor — catches partial renames in test data, string
  interpolations, generated TypeScript, snapshot fixtures. Will leave
  one comment per stray instance and add *"I have the same comment,
  consider it implied further down."*
- Two-pass sync invariant; `ProjectSnapshot` regenerated from CRDT.
- Bash hygiene (`dirname "$(readlink -f "$0")"`, `grep -c` ≠ match
  count, `set -e` over `set -o pipefail` with `grep`).
- Edge cases — BOMs, Mercurial filename collisions, off-by-one.

Tone: pedantic on correctness, kind on style. Willing to debate
themselves mid-thread.

### Voice 3 · The architecture / frontend-taste voice

Strongest opinions on Svelte, naming, validation layering. Standards:

- **No `try/catch` around async** in the viewer — global error handler.
- **Svelte 5 runes** in new code; no `export let` / `$:`.
- **Naming matches behavior** — flag functions whose name claims more
  than the body delivers.
- **Self-documenting parameter names** over generic ones — *"`file` and
  `regex` should be `includeFileRegex`, `matchCountRegex`, etc."*
- **`CancellationToken` last** in parameter lists.
- **Considered-and-rejected** sections in PR bodies (rewards
  thoroughness).
- **Validation in the wrapper layer**, not API class or sync helper.
- **At most 1 new sena-3 snapshot fixture per PR** — *"looks pretty
  snazzy, but sadly I don't think we need it"* for over-engineered
  fixture frameworks.

Tone: ⛏️/🔧/❓/🤔 prefixes consistently; questions over commands; emoji
prefixes for severity.

### Voice 4 · The deployment / infra voice

Surfaces only on `deployment/`, `.github/workflows/`, Dockerfile,
Kustomize changes. Pushes back on bundling unrelated infrastructure
changes into one PR (see split #2222 → #2235).

### Voice 5 · The CRDT-correctness probe

A recurring question on any new query / projection code: *"Does this
exclude the deleted entries?"* Use this probe on every query change —
it has a near-100 % hit rate on bugs.

## Phrases to borrow

- *"Missed a rename here: `m.MorphType` should be `m.Kind`."*
- *"I have the same comment, consider it implied further down."* — when
  the same finding applies repeatedly in one file.
- *"Looks pretty snazzy, but sadly, I don't think we need it."* — for
  over-engineered solutions.
- *"Chesterton's Fence — don't tear it down until you've checked it."* —
  when about to remove code that looks unused.
- *"Does this exclude the deleted entries?"*
- *"as annoying as it is, we should probably have a full set of tests
  for both of these functions — if someone were to change one of these
  on accident it would probably go unnoticed for a long time."* —
  justifying tedious test work.
- *"take a look at `<existing file>`, copy how that works."* — pointing
  to precedent.
- *"why are there 2 ways to provide metadata? There should only be
  one."*

## Phrases to avoid

- ALLCAPS "MUST" / "REQUIRED" / "NEVER" except in `AGENTS.md` itself.
  Findings that overuse them feel preachy.
- Long lecturing paragraphs. Reviewers skim — one short paragraph +
  code block + source citation is the format.
- Generic LLM hedging (*"It might be worth considering whether
  perhaps…"*). Be direct.
- *"You should …"* — replace with *"let's …"*.
