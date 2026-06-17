---
name: crowdin-merge
description: Resolve the open Crowdin l10n_develop PR locally — merge `develop` in, reconcile catalogs to current code, review incoming translations for quality issues (brand-name decomposition, broken placeholders, MT failures like `Lexbox → "Sanduku la maneno"`), add translator context to new msgids, propose fixes, and on approval push translations to Crowdin and the branch. Use when the user references the Crowdin PR, says the l10n_develop PR has conflicts, or asks to land Crowdin translations.
---

# /crowdin-merge

Orchestrate the inbound Crowdin sync flow for languageforge-lexbox. The flow has three phases: deterministic (scripts), judgement (subagents), and publication (gated on user approval).

## Project context (load-bearing)

- **Crowdin's GitHub integration is export-only.** It pushes translations from Crowdin to GitHub on each sync but does NOT pull source files from GitHub. After the context-writer agent adds new `#.` comments, you MUST run `task -d crowdin crowdin -- push sources` for those comments to reach Crowdin's DB. Without that, context lives only in git and never protects future MT/AI runs. (Verified 2026-05-28 — past concern about "push sources confusing Crowdin" did not materialize this time; the operation only updates context metadata and is safe.)
- **All 7 target locales** (es, fr, id, ko, ms, sw, vi) are MT-covered by Crowdin (DeepL + Crowdin Translate + AI). Old "non-MT" classification is obsolete.
- **Export-only-approved is OFF** on the Crowdin project — so translations pushed via CLI (which enter as unapproved suggestions) DO appear in subsequent Crowdin → GitHub syncs.
- **MT can fail badly** on brand names and abbreviations (real example: `Lexbox → "Sanduku la maneno"`, `MB → "Mama/Baba"`, `FieldWorks → "Kazi za Uwanja"` — all Swahili). The review step exists to catch these.
- **Protected brand names**: `Lexbox`, `LexBox`, `FieldWorks`, `FwLite`, `SIL`. Must appear verbatim in every locale.
- More background in user memory `crowdin_workflow.md`.

## Flow

### 1. Pre-flight (deterministic)

```powershell
.\.claude\skills\crowdin-merge\scripts\merge-crowdin.ps1
```

This script:
1. Asserts clean working tree
2. Fetches `origin/l10n_develop` and `origin/develop`; verifies the open PR exists
3. Runs `audit-po.mjs` to check no translations are being silently deleted (existing approved translations going `→ ""`). Aborts on data loss.
4. Checks out `l10n_develop` and **aligns local to origin**: if local has commits not on origin, classifies them — if all are `New translations ...` auto-syncs (Crowdin force-pushed previously), resets local to origin; if any look human-authored, aborts and asks you to rescue them first. Crowdin owns this branch on origin, so this kind of divergence is normal and resetting is correct.
5. Merges `origin/develop`, resolves `.po` and `description-*.md` conflicts with `--ours` (Crowdin's side wins for translation data)
6. Runs `pnpm i18n:extract` (in `frontend/viewer/`) twice; second run must produce no diff (stability check)
7. Commits `"Merge develop into l10n_develop and reconcile catalogs"`

If the script exits non-zero, STOP and surface the error. The most common abort cause is data loss in the audit — see `scripts/audit-po.mjs` output.

### 2. Judgement — parallel subagents

Two agents run independently. Launch in the same tool-call message for parallelism.

**Context agent** — `Agent` tool, `subagent_type: i18n-context-writer`:

```
input: output of `node .claude/skills/crowdin-merge/scripts/list-new-msgids.mjs`
job: add `#.` translator-context comments to new msgids in en.po, decide skip-vs-add per msgid
output: writes en.po; emits JSON decision log to stdout
```

After it returns, run:

```powershell
# Sync context comments en → all locales, and assert completeness
cd frontend/viewer ; pnpm i18n:extract
node .claude/skills/crowdin-merge/scripts/check-completeness.mjs context <agent-decisions.json>
```

If completeness fails, re-prompt the agent with the gap list. Then commit:
`"Add translator context for new strings"`.

**After committing context additions, push sources to Crowdin so the new context lands in Crowdin's DB:**

```powershell
task -d crowdin crowdin -- push sources
```

Skip this step if the context agent added zero comments (decision log shows all `skipped-obvious`). The push is safe: only updates string metadata, doesn't touch translations.

**Reviewer agent** — `Agent` tool, `subagent_type: i18n-translation-reviewer`. One invocation per locale that has incoming translations:

```
input: filtered JSON from `node list-incoming-translations.mjs`, one locale's array per agent
job: per-translation verdict (ok / fix / flag) with reason
output: JSON array, one entry per input
```

After all reviewer agents return, run completeness check:

```powershell
node .claude/skills/crowdin-merge/scripts/check-completeness.mjs review <combined-verdicts.json>
```

### 3. Apply fixes + report

Run `apply-fixes.mjs` to apply every `verdict: "fix"` to the relevant locale .po file in one pass:

```powershell
node .claude/skills/crowdin-merge/scripts/apply-fixes.mjs
```

The script reads `$TEMP/verdicts-<locale>.json` files, edits the msgstr line under the matching msgid, and prints per-locale counts. It refuses to apply if msgid spans multiple lines or isn't found exactly — surfaces those for manual handling.

Commit: `"Apply translation fixes from review pass"`.

Build the report — concise, ASCII-table style:

```
Crowdin PR #<num> — Review summary

Merge       reconciled <N> catalog files, <M> new msgids added by develop
Context     added comments to <X> of <Y> new msgids
Review      <total> incoming translations across <locale-count> locales
              ✓ ok      <count>
              ⚠ flagged <count>  (no action — listed below)
              ✗ fixed   <count>  (applied — listed below)

Fixed (each with reason from reviewer agent):

  sw  "Lexbox"
        was:    "Sanduku la maneno"
        now:    "Lexbox"
        reason: brand-name decomposed; "Lexbox" must appear verbatim

  sw  "{0} (FieldWorks)"
        was:    "{0} (Kazi za Uwanja)"
        now:    "{0} (FieldWorks)"
        reason: brand "FieldWorks" decomposed to "Field jobs"

  sw  "{0} MB"
        was:    "{0} Mama/Baba"
        now:    "{0} MB"
        reason: abbreviation "MB" decomposed (M=Mama, B=Baba); units must stay verbatim

Flagged (for human review — not auto-fixed):

  ms  "Preview not available"
        crowdin: "Praperintian tidak tersedia"
        reason:  "Praperintian" is not standard Malay; consider "Pratonton tidak tersedia"

[Glossary reminder — show ONLY if fixes include brand-name protections:]
ℹ️  This run fixed N brand-name decomposition cases. To prevent recurrence,
    add to Crowdin Glossary (Settings → Glossaries) as type "Trademark":
      Lexbox, FieldWorks, FwLite, SIL
    Once glossary is set, MT engines respect these and won't decompose them.

Ready to push?
  1.  task -d crowdin crowdin -- push translations -l sw    (repeat for each touched locale)
  2.  git push origin l10n_develop

Approve to run both, or just 1 / just 2 / cancel.
```

Use the actual numbers and findings, not the placeholders above. Show the glossary block only when the fixes contain at least one brand-name protection (suggested msgstr exactly equals msgid AND that msgid contains "Lexbox" / "FieldWorks" / "FwLite" / "SIL").

**For the "Fixed" and "Flagged" sections**: pull `msgid`, `msgstr` (now / was-on-l10n_develop), `suggested` (for fixes), and `reason` directly from the reviewer agent's verdict JSON files at `$TEMP/verdicts-<locale>.json`. Every fix and every flag has a `reason` field — surface it so the user can sanity-check each call without having to open the JSON.

### 4. Push (gated)

On explicit user approval:

```powershell
.\.claude\skills\crowdin-merge\scripts\push-translations.ps1 -Locales <only-locales-with-fixes>
```

The script pushes Crowdin first (retryable if it fails), then pushes the branch. On success it tells the user the PR is mergeable.

**If user declines or asks for changes**: don't push. Show updated report after any changes; re-ask. Never push without explicit "yes / approve / push".

## Failure modes to anticipate

- **Audit detects deletions** — almost certainly means past locally-pushed translations weren't pushed to Crowdin. Surface the specific msgids; ask the user whether to push them up before continuing, or whether the deletion is intentional.
- **Stability check fails** — `i18n:extract` produced different output on the second run. Don't proceed; investigate the extractor first.
- **Conflict on a non-i18n file** — `merge-crowdin.ps1` will abort. Resolve manually and rerun.
- **Reviewer agent flags but doesn't fix** — for `verdict: "flag"` entries, list them in the report under "Flagged"; the user decides whether to fix manually in Crowdin's UI or override.
- **No incoming translations** — Crowdin PR exists but every locale.po is unchanged vs develop. Possible after a stale Crowdin sync. Report it and stop after the merge commit; nothing to review.

## Files this skill writes

- Commits on `l10n_develop`: merge commit, context commit, fixes commit (each only if there's content)
- Modifies: `frontend/viewer/src/locales/*.po`, possibly `platform.bible-extension/assets/descriptions/description-*.md`
- Never touches: develop branch directly, any non-i18n source file

## Companion tasks (Taskfile.yml in `crowdin/`)

- `task c:crowdin -- <crowdin-cli-args>` — wrapped Crowdin CLI invocation (uses `-b develop`)
- `task c:pull` — pulls translations (rarely needed; the skill handles this)
- `task c:i18n` — runs `pnpm i18n:extract` from `frontend/viewer/`

The skill calls `task -d crowdin crowdin -- push translations -l <locale>` directly per touched locale (or the equivalent via `push-translations.ps1`).
