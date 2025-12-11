---
agent: agent
---
# Translation Review Workflow

## Getting Started

1. Navigate to `frontend/viewer/`
2. Run the script to get the first batch (replace `{LANGUAGE}` with language code like `es`, `fr`, `de`):
   ```bash
   node review-po.js {LANGUAGE} {BATCH_SIZE}
   ```
   
   **Batch sizes:**
   - `5` — Recommended for maximum thoroughness (default)
   - `10` — Good balance of thoroughness and pace
   - `15+` — Faster but less attention to detail
   
   Example:
   ```bash
   node review-po.js es 5
   ```

3. Copy the output and use the prompt below
4. After reviewing, run the script again for the next batch:
   ```bash
   node review-po.js {LANGUAGE} {BATCH_SIZE}
   ```
5. Repeat until the script shows "✅ REVIEW COMPLETE!"

---

## Prompt for AI Assistant

Use this prompt with each batch output from `review-po.js`:

---

### [PASTE BATCH OUTPUT HERE]

---

I need you to review and actively improve translations in the **FwLite app** (a dictionary editor for linguists).

**File:** `frontend/viewer/src/locales/{LANGUAGE}.po` (300+ reviewable entries)

**Goal:** Ensure accuracy, consistency, and natural phrasing in the target language. Fix sub-optimal translations and fill in missing translations with confidence.

See AGENTS.md for context on the app architecture and i18n setup.

### Review Checklist

For each translation entry in this batch:

#### 1. Spelling & Grammar
- [ ] Target language spelling is correct (including accents and diacritics)
- [ ] Grammar is natural and idiomatic
- [ ] Punctuation matches context
- [ ] No typos

#### 2. Terminology Consistency
- [ ] Key terms are used consistently with previous batches
- [ ] Same English word isn't translated multiple ways
- [ ] Establish/maintain glossary for key application terms:
  - Entry (Classic view) → [target language equivalent]
  - Word (Lite view) → [target language equivalent]
  - Sense (Classic) / Meaning (Lite) → [target language equivalents]
  - Definition → [target language equivalent]
  - Example → [target language equivalent]
  - Gloss → [target language equivalent]
  - Component → [target language equivalent]
  - Complex Form → [target language equivalent]
  - Semantic domain → [target language equivalent]
  - Writing System → [target language equivalent]

#### 3. Context Appropriateness
- [ ] Read the `#.` comment block to understand where/how this string appears in the app
- [ ] Read the `#:` source file reference if you need more context (see I18N_CONTEXT_GUIDE.md for how context comments are structured)
- [ ] Translation makes sense in that UI context (button, label, message, etc.)
- [ ] Phrasing is natural and professional (not literal word-for-word translation)
- [ ] Tone matches the original (instructional, confirmational, etc.)
- [ ] Technical terms are appropriate for the linguistics/dictionary domain

#### 4. Completeness & Fixes
- [ ] Empty translations (msgstr ""): **Provide your best translation** unless completely unsure about meaning
- [ ] Fuzzy entries (marked with `#, fuzzy` comments): **Fix or improve** them if you're confident
- [ ] Untranslated strings (msgstr = msgid): **Translate them** with confidence
- [ ] Placeholder variables (`{0}`, `{num}`, etc.) remain unchanged
- [ ] Sub-optimal translations: **Improve phrasing** if you see better alternatives in context

#### 5. Special Cases to Watch
- [ ] Plural forms: `{num, plural, one {...} other {...}}` are correctly translated
- [ ] Field labels and buttons are concise and clear
- [ ] Error messages are helpful and grammatically correct
- [ ] Status messages use appropriate verb tenses (gerunds for ongoing actions, past for completed)

### Workflow: Review → Update → Report

**CRITICAL: You MUST update the .po file with your changes as you review.**

1. Review each translation in the batch output
2. **IMMEDIATELY update `frontend/viewer/src/locales/{LANGUAGE}.po`** with any fixes/additions
3. Then provide a report of what you changed

### Report Format

After reviewing and updating the file, provide:

#### Changes Made
**For this batch**, list all translations you updated in the .po file:

**Fixed/Improved Translations:**
- **[Line/Entry]**: `msgid "..."` → `msgstr "[NEW_TRANSLATION]"` — [Reason]

**Added Translations (empty → filled):**
- **[Line/Entry]**: `msgid "..."` → `msgstr "[NEW_TRANSLATION]"` — [Reason]

Example:
- **Line 25**: `msgid "Definition"` → `msgstr "Definición"` — Was empty, added based on terminology glossary
- **Line 47**: `msgid "Meaning"` → `msgstr "Significado"` — Was fuzzy, improved for consistency with batch 1
- **Line 102**: `msgid "Add Entry"` → `msgstr "Añadir Entrada"` — Was "Agregar Registro", improved for consistency

#### Issues Found (Not Fixed)
List any problems you couldn't confidently fix (or "No unfixable issues"):
- **[Line/Entry]**: [Issue Type] — [Description] (reason for not fixing)

#### Terminology Glossary (This Batch)
All terms established or confirmed:
- English → [target language]

#### Notes
- Observations about this batch
- Patterns noticed
- Quality assessment
- Confidence level on added/fixed translations

#### Next Steps
Are you ready for the next batch?

---

## Example Feedback Format

```
## Proposed Changes

**Fixed/Improved Translations:**
- **Line 25**: `msgid "Definition"` → `msgstr "Definición"` — Was "defición" (accent error)
- **Line 102**: `msgid "Add"` → `msgstr "Añadir"` — Was "Abjdir" (typo)

**Added Translations (empty → filled):**
- **Line 47**: `msgid "Meaning"` → `msgstr "Significado"` — Was empty, using established glossary term

## Issues Found (Not Fixed)
- **Line 156**: Context unclear — "Acepción" or "Significado" for "Sense"? → Need clarification before fixing

## Terminology Glossary
- Definition → Definición
- Meaning → Significado
- Add → Añadir

## Notes
This batch covers mostly button labels and field names. Terminology consistent with batches 1-2. The plural form on line 65 is correctly translated. Added 3 missing translations with high confidence.

## Quality
✅ Strong — Fixed 2 errors, added 1 translation, 1 item needs clarification. 95% confident in changes.
```

---

---

## When Review is Complete

The script will show:
```
✅ REVIEW COMPLETE!
All strings in src/locales/{LANGUAGE}.po have been reviewed.
```

At this point, all reviewable translations have been reviewed. Collect any issues found and create a summary or file bugs as needed.

---

## Tips for Consistency Across Batches

1. **Save your glossary** from each batch
2. **Note terminology decisions** made in earlier batches
3. **Cross-reference** when you see similar terms
4. **Check plural forms** carefully — they follow a specific pattern in Spanish
5. **Preserve variable placeholders** (`{0}`, `{num}`) exactly as they appear
6. **Only skip translations if unsure** — For empty/fuzzy/bad translations, provide your best translation unless the meaning is genuinely unclear from context
7. **Note confidence level** — Be explicit about how confident you are in each fix/addition so reviewers can prioritize verification
