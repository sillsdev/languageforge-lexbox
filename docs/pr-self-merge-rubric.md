# PR Self-Merge Rubric (languageforge-lexbox)

> **Interactive version:** an interactive, tick-through copy of this rubric is
> published as an artifact:
> <https://claude.ai/code/artifact/7792cc42-991f-4d2e-8990-7a582b29162c>
> (viewable once shared from the artifact's share menu).

Decide whether **your own** PR can be merged **without a second human review**.
Internal authors only (not outside contributors). Default-closed: **if anything
is uncertain, get a human.** You are not saving time by merging something that
needed review — you are moving the cost downstream and making it bigger.

---

## 0. Preconditions — both true, or stop here

- [ ] **CI is green.**
- [ ] **An AI review has run** (Devin / CodeRabbit / your own agent) and **you
      read its output** and addressed anything real.

These are assumed by everything below. This rubric only decides whether a
*second human* is still required on top of them.

---

## The rule

> **A PR is self-mergeable if — and only if — *every* changed file is GREEN.**

Judge each changed file by its **(path, change-type)** pair. Generated files
don't count as their own files — they inherit the classification of the source
edit that produced them (see reference).

A changed file is **GREEN** when either:

1. **Its change-type is universal-green** — *regardless of path*: docs /
   comment-only, formatting-or-whitespace-only, a **new** test (not a test
   edit), or i18n / new UI string additions. No further gates.
2. **Its path is not 🔴** *and* its change-type is **conditional-green**
   (everything else) *and* it clears the gates in §2 that apply to it.

If a file is neither → **not self-mergeable, get a human.** A **mixed PR** is
judged file-by-file: one non-green file sinks it — split the PR or request
review.

---

## 1. 🔴 Always-human — the hard stops

If the PR changes any of these, **get a human** — *unless every such change is a
universal-green type* (docs/comment, formatting, new test, i18n), which is
allowed anywhere.

- `backend/FwLite/FwLiteProjectSync/**` — CRDT↔FwData sync orchestration
- `backend/FwLite/LcmCrdt/**` — CRDT implementation
- `backend/FwLite/FwDataMiniLcmBridge/**` — FwData↔MiniLcm conversion
- `backend/FwLite/MiniLcm/**` model / `IMiniLcmApi` files — model-field fanout
- `backend/FwHeadless/**` · `backend/FixFwData/**` · `backend/LexCore/Sync/**` ·
  `backend/SyncReverseProxy/**` · `hgweb/**`
- `**/Migrations/**` — EF migrations
- Harmony package version bumps / MSBuild reference changes
- **Auth / JWT / secrets anywhere** (incl. `*/Auth/`, connection strings)
- **Breaking GraphQL schema changes** — removes/renames a field/type/enum-value,
  tightens nullability, adds a required arg, or changes an auth attribute on an
  existing resolver
- `deployment/**` — k8s / Kustomize / PVCs / secrets

*Why data loss / security / deployed-contract risk — see the repo's 🔴 CRITICAL
AGENTS.md flags and the sentinel review agents.*

---

## 2. Gates — apply to every *conditional-green* file

- **Size (always).** ≤ **150 net changed lines** of *reviewable, non-green* code
  — excluding generated files, universal-green files, lockfiles, and snapshots —
  **AND** it's *one coherent change you can hold in your head* (not 40 lines
  smeared across 15 unrelated files).
- **Test (only if the change alters behavior** — new behavior / bug fix /
  feature**).** New behavior → a new test; bug fix → a **regression test**. *If
  it can't reasonably be tested, get a human.* Non-behavior changes (refactor,
  config, dependency bump, test edit) need no new test — green CI covers them.
  Run the cheap targeted tests locally (per AGENTS.md tiers); rely on CI for
  stack-dependent suites.
- *(Verification is folded into the attestation below.)*

---

## 3. Final attestation — you are the reviewer of record

Applies to **every** PR. Vouch for this diff to **the same standard you'd hold a
teammate's PR to** (the repo's "Integrity is non-negotiable" ethos). Honest
**YES** to all four, or get a human:

- [ ] **Read** — I read every non-generated changed line, not skimmed.
- [ ] **Explain** — I can explain why each change exists, *without re-reading* —
      as if I wrote it.
- [ ] **Blast radius & ran it** — I checked what this could break (callers /
      consumers of any changed contract), and where it has a runtime surface a
      test can't fully capture, I exercised it and saw the intended behavior.
- [ ] **No mystery code** — there is nothing here I don't fully understand
      ("the agent probably knows what it's doing" is a NO).

---

## Tie-breaker

**Any "I'm not sure" on any line above → get a human.** That is the correct,
low-cost outcome, not a failure.

---

## Reference

### Change-type reach

*Reach* = how far a change-type green-lights across paths. **Universal** overrides
even a 🔴 path. **Conditional** is green only outside 🔴, and is subject to the §2
gates.

| Change-type | Reach | Test needed? / Notes |
|---|---|---|
| Docs / comment-only | 🟢 Universal | No runtime effect |
| Formatting / whitespace | 🟢 Universal | Auto-format, import reorder |
| **New** test (addition, not edit) | 🟢 Universal | Adds coverage, can't weaken a guard |
| i18n / new UI string additions | 🟢 Universal | Low-risk content |
| Test *modification* | 🟡 Conditional | No test; a weakened assertion in a 🔴 test → 🔴 = human |
| Dependency bump | 🟡 Conditional | No test; Harmony bump is 🔴 by path |
| Config change | 🟡 Conditional | No test; secret/auth/connection-string config is 🔴 by path |
| Pure refactor (no behavior change) | 🟡 Conditional | No test (no behavior change); leans on the §3 attestation |
| New behavior / bug fix / feature | 🟡 Conditional | **Needs a test** (§2); additive GraphQL lives here |

### Other

- **Generated files** (`frontend/viewer/src/lib/dotnet-types/generated-types/**`,
  `.po` catalogs) are *transparent*: classify the PR by the source edit; they're
  excluded from the size count.
- **Tool-ability.** The path lists are globs and the size rule is a computable
  line count on purpose — a future helper tool can implement this rubric
  verbatim.
- **On the two tiers.** An earlier draft split conditional-green into
  "zone-limited" and "gated." They had identical *reach* (green only outside 🔴,
  same size + attestation) and differed only by the test requirement — so
  they're merged, and "behavior change → test" is just a rule in §2.
