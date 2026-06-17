# Crowdin Translation Management

This directory manages the translation workflow for LexBox / FieldWorks Lite using the Crowdin CLI.

## Translation flow

**Inbound** (Crowdin → code): Crowdin auto-generates a PR from `l10n_develop` on each sync. Use the **`/crowdin-merge` skill** to resolve it: merge `develop` in, reconcile catalogs, review incoming translations for quality issues (brand-name decomposition, broken placeholders), apply fixes, optionally push corrections back to Crowdin, then push the branch.

**Outbound — sources** (code → Crowdin): Crowdin's GitHub integration is **export-only** — it does not pull sources from GitHub. To push updated source strings (including new `#.` context comments) into Crowdin's database, run `task crowdin -- push sources` (or `crowdin push sources` directly). This is the only mechanism. Run it after adding context comments to `en.po`. Verified safe — only updates string metadata, doesn't delete strings or invalidate translations. Note that context-only source updates don't trigger a new Crowdin → GitHub export PR; the new context only becomes visible in exports the next time a translation event causes a sync.

**Outbound — translations** (when we locally correct or fill in): the skill pushes via `crowdin push translations -l <locale>` for the specific locales it touched. Pushed translations enter Crowdin as unapproved suggestions; because the project's *Export only approved* setting is OFF, they still appear in subsequent Crowdin → GitHub syncs.

## Machine Translation coverage

All 7 target locales are MT-covered by Crowdin (Crowdin Translate + DeepL + AI Auto-Translate). The older "non-MT" classification (ms, sw) is obsolete.

Known failure modes the skill defends against:

- **Brand-name decomposition** — e.g. MT translating `Lexbox` → "Sanduku la maneno" (literal "word box") in Swahili. Add a project-level Glossary entry of type "Trademark" for `Lexbox`, `FieldWorks`, `FwLite`, `SIL` to make MT respect them.
- **Abbreviation decomposition** — e.g. `MB` → "Mama/Baba" in Swahili (M=Mama, B=Baba). The same glossary approach helps; the skill also catches these in review.
- **MT suggestions stay as suggestions, but are still exported** — Crowdin's MT engines produce unapproved suggestions by default. Because *Export only approved* is OFF on this project, suggestions still appear in subsequent GitHub syncs. Approval is only needed when you want to lock a translation against future MT re-runs.

## Resources

- [Crowdin CLI Configuration Reference](https://crowdin.github.io/crowdin-cli/configuration)
- [Official Configuration File Docs](https://support.crowdin.com/developer/configuration-file/)
- [Project Auto-Translate Settings](https://crowdin.com/project/language-depot/settings#project-pre-translate)
- [Machine Translation Engines Profile](https://crowdin.com/profile/tim_haasdyk/machine-translation)

## Files

- `crowdin.yml` — Crowdin CLI config (project_id, source/translation file patterns)
- `Taskfile.yml` — wrappers: `task c:crowdin -- <args>` and `task c:pull`
- `.crowdin-env.local` (gitignored) — your Crowdin API token
- `.claude/skills/crowdin-merge/` — the skill that automates the inbound flow
