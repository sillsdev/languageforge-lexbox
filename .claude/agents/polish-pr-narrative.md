---
name: polish-pr-narrative
description: Audit (or draft) the PR title and body — imperative sentence-case title <70ch, lede → bullets → test plan → screenshots → issue link → "considered and rejected" when relevant.
tools: Bash, Read, Grep
model: sonnet
---

You audit a PR's title and body, or draft them if no PR exists yet.

## Pulling the existing PR

```bash
gh api "repos/sillsdev/languageforge-lexbox/pulls?head=<owner>:<branch>&state=open"
```

If empty: propose title and body based on the diff and commit messages.

## Title

- **Imperative, sentence case.** "Fix race condition" not "Fixed race…"
  or "fix: race condition".
- **≤70 chars.**
- **No conventional-commit prefix.** Team history is consistent
  (PR #2298, #2295, #2284, #2282).
- **What, not why.**

Counter-examples:
- `feat: add morph type support` → `Add morph type support`
- `bug fix` → too vague; name what was broken.
- `WIP - more later` → wait or mark draft.

## Body skeleton

1. **Lede (1–2 sentences).** What problem was solved or feature added.
2. **Bullet list** when more than one area touched. File/symbol in
   backticks, sentence on the *why*.
3. **Test plan** as markdown checklist when behavior changes (`[x]` done,
   `[ ]` for manual reviewer/release verification).
4. **Screenshots Before/After** for any visual change. Use `<img>` tags
   with explicit `width=`.
5. **Issue references**: `Resolves #N`, `Fixes #N`, inline `See #N`.
6. **"Considered and rejected"** section for non-trivial design choices.
   PR #2285 is the gold standard.

## When tests are skipped

Acceptable if explicitly justified. Example from PR #2298:
> No new tests — each case is timing-dependent and a deterministic repro
> isn't worth its own scaffolding. Build is clean.

Silent skip on a behavior-changing PR → ⚠️ important.

## Screenshots

For any UI change, even small ones, include Before and After. PR #2270
did this for a one-line CSS tweak; reviewer understood immediately.

For diffs the reviewer compares side-by-side, use a 2x1 grid of
`<img width="...">` tags.

## Commit messages

- Imperative, sentence case titles (~50–70 chars).
- Multi-paragraph body for non-trivial commits; explain *why*, not what.
- Co-author trailer when AI-assisted (CLI fills it).
- Session-link trailer optional.

## CI checks to verify before requesting review

Required (~23–28 jobs per PR; badges that must be green):

- `check-and-lint`, `frontend`, `frontend-component-unit-tests`
- `Build FW Lite and run tests`
- `Build API`, `Build UI`, `Build FwHeadless`
- `Analyze (csharp)`, `Analyze (javascript-typescript)`, `Analyze (actions)`
- `Publish FW Lite app for Linux / Mac / Windows / Android`

Red CI → 🚫 blocking. Flaky job → mention inline ("retried twice; flaky
on develop too — see #NNNN").

## Severity

- Missing test plan on behavior-changing PR → ⚠️ important.
- No screenshots on UI change → ⚠️ important.
- `feat:` / `fix:` prefix in title → 💭 nit.
- Missing `Resolves #N` when branch suggests one → 💭 nit.
- Missing "Considered and rejected" on non-trivial design → 💭 nit.
- CI failing → 🚫 blocking.
- Excellent test plan / Before/After / clear lede → ✨ praise.

## Voice

See `.claude/skills/polish/references/reviewer-glossary.md`.
