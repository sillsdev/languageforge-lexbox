---
agent: agent
---
# FwLite Full Translation Update Workflow

## Progress Checklist (Required in Output)

Always include a checklist that shows the current status of each step:

- [ ] 1) Extract new strings
- [ ] 2) Add context to new strings in en.po
- [ ] 3) Sync context to all locales
- [ ] 4) Translate only non-MT languages
- [ ] 5) Verify no empty `msgstr ""` in non-MT locales
- [ ] 6) Pause for user confirmation
- [ ] 7) Push sources and non-MT translations

Update the checklist as you progress. Only mark a step complete when it is fully done.

## Source of Truth: Non-MT Languages


Do NOT hard-code the non-MT languages in this prompt. Always read:
**[crowdin/README.md](../../crowdin/README.md)**
and use the **“Needs Local AI Translation (non-MT)”** list.

## Workflow (Required Order)

### 1) Extract new strings
- Run `task c:i18n` (or `task i18n` in `frontend/viewer`).
- This updates `en.po` and other locale files.

### 2) Add context to any strings that don't have context (English only)
- Update **only** [frontend/viewer/src/locales/en.po](../../frontend/viewer/src/locales/en.po).
- Add a `#.` comment block describing UI context, view (Lite/Classic), and intent.
- Preserve existing comments; only add context to strings that don't have it.

### 3) Sync context to all locales
- Run `task c:i18n` again to copy the new comments from `en.po` to all other `.po` files.

### 4) Translate only non-MT languages
- From [crowdin/README.md](../../crowdin/README.md), translate **only** those locales.
- Update their `.po` files under [frontend/viewer/src/locales/](../../frontend/viewer/src/locales/).

### 5) Validate: no empty strings in non-MT languages
- For each non-MT locale file, verify there are **no** empty `msgstr ""` values.
- The translation step is **not complete** until this check passes.

### 6) Pause for user verification
- Summarize what was changed and ask the user to confirm before pushing to Crowdin.
- Do **not** push without explicit confirmation.

### 7) If confirmed, push safely
- Run `task c:push-sources`.
- Run `task c:push-translations-non-mt`.

---

## Translation Practices (Required)

Use these rules from the review workflow to ensure quality:

### Spelling & Grammar
- Correct spelling and diacritics.
- Natural, idiomatic grammar.
- Correct punctuation and tone.

### Terminology Consistency
- Keep key terms consistent across the app.
- Maintain/extend a glossary for:
	- Entry (Classic view)
	- Word (Lite view)
	- Sense (Classic) / Meaning (Lite)
	- Definition, Example, Gloss
	- Component, Complex Form
	- Semantic domain, Writing System

### Context Appropriateness
- Use `#.` context comments to guide tone and UI usage.
- Button labels should be concise.
- Error messages should be clear and helpful.

### Safety Rules
- Preserve placeholders exactly (`{0}`, `{num}`, `{name}` etc.).
- Keep plural forms structurally correct.
- Do not change source `msgid` values.

### Completeness
- Fill any empty `msgstr ""` in non-MT languages unless context is genuinely unclear.
- If unclear, list it explicitly and ask for clarification.

---

## Reporting Requirements

After updates, provide:

### Changes Made
- List each updated entry with `msgid` and the new `msgstr`.

### Issues Found (Not Fixed)
- Any entries you couldn’t translate confidently, with reason.

### Terminology Glossary (This Run)
- English → target-language equivalents added or confirmed.

### Verification
- Confirm that each non-MT file has **no empty `msgstr ""`** remaining.

### Confirmation Gate
- Ask the user: **“Ready for me to push sources and non-MT translations to Crowdin?”**
- Only proceed with tasks after explicit confirmation.
