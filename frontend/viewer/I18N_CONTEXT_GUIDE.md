# i18n Context Addition Workflow

## Overview
After extracting new i18n strings using the i18n task, add translator context comments to the `en.po` file. This guide ensures translators receive all necessary information to translate accurately without verbose or misleading descriptions.

## Workflow

### 1. Extract Strings
```bash
cd frontend/viewer
pnpm run i18n:extract
```

This updates `src/locales/en.po` with any new `msgid` entries.

### 2. Identify New Strings
Review the git diff to find all NEW `msgid` entries added to `en.po`:
```bash
git diff src/locales/en.po | grep "^+msgid"
```

### 3. Determine View Applicability
**CRITICAL FIRST STEP:** Check the source file(s) to determine which view(s) the string applies to.

**How to identify which view(s) a string applies to:**
1. Look at source file location in `#:` comment
2. Search the source component for how the string is used:
   - **`pt($t`Classic text`, $t`Lite text`, ...)`** → String has both versions (see patterns below)
   - **`<ViewT ... classic={...} lite={...}>`** → String has both versions (see patterns below)
   - **`$t`Some text`` (no pt() or ViewT wrapper)** → String is **identical in both views**
3. If using Lite terminology (Word, Meaning, Part of, Component) → **Lite version**
4. If using Classic terminology (Entry, Sense, Complex Form) → **Classic version**

**See "Understanding i18n Extraction: pt() and ViewT" section below for detailed pattern explanations.**

### 4. Add Context Comments
For each new string, add a `#.` comment block above the `#:` source reference.

**When to add context:**
- ✅ **View difference** - String differs between Classic/Lite views
- ✅ **UI element unclear** - Is this a button? tooltip? field label? dialog title?
- ✅ **User action unclear** - What happens when user clicks/selects this?
- ✅ **Variable placeholders** - Explain what the placeholder contains
- ✅ **Related terms** - Cross-reference similar UI terms, terminology equivalents
- ✅ **Domain terminology** - Lexicography or linguistic concepts need brief explanation
- ❌ **Skip obvious strings** - "OK", "Cancel", "Save" (global UI conventions)

### 5. Context Comment Structure

**Standard format for view-specific strings (if Classic and Lite differ):**
```po
#. Relevant view: [Classic | Lite]
#. [Lite|Classic] view equivalent: "[text]"
#. [Description of UI element or behavior]
```

**For strings identical in both views, omit the view statement:**
```po
#. [Description of UI element or behavior]
```

**Essential elements to include:**
- **Where:** "Field label in sense editor", "Dialog footer", "Dropdown in Browse filter"
- **What:** "Meaning explanation for a sense", "Classification of word relationships"
- **How:** "Filters entries by...", "User action outcome: opens dialog..."
- **Related terms:** Cross-reference related UI fields or view equivalents

**Example - Good (Classic string with Lite equivalent):**
```po
#. Relevant view: Classic
#. Lite view equivalent: "Display as"
#. Field label: the standard form used as dictionary headword
#: src/lib/entry-editor/object-editors/EntryEditorPrimitive.svelte
msgid "Lexeme form"
msgstr "Lexeme form"
```

**Example - Good (Lite string with Classic equivalent):**
```po
#. Relevant view: Lite
#. Classic view equivalent: "Lexeme form"
#. Field label: vernacular meaning (the literal meaning in the source language)
#: src/lib/entry-editor/object-editors/EntryEditorPrimitive.svelte
msgid "Word"
msgstr "Word"
```

**Example - Good (shared between both views, no equivalents):**
```po
#. Button to add example sentence
#. Sentence/phrase showing how a word is used in context
#: src/lib/entry-editor/object-editors/EntryEditor.svelte
msgid "Add Example"
msgstr "Add Example"
```

**Example - Avoid (verbose/confusing):**
```po
#. Field for showing the basic core meaning before any metaphorical extensions
#. Like how "bank" basically means a river edge before financial institution
#: ...
```

## Understanding i18n Extraction: pt() and ViewT

The codebase has **two patterns** for view-specific strings:

### Pattern 1: pt() function (inline selection)
Used when you need the selected text inline in a template:
```javascript
pt($t`Delete Entry`, $t`Delete Word`, $currentView)
```

Both strings are extracted as separate `msgid` entries:
```po
msgid "Delete Entry"
msgid "Delete Word"
```

### Pattern 2: ViewT component (for complex content)
Used when you need HTML content or complex layouts that differ by view:
```svelte
<ViewT view={$currentView} classic={$t`Filter # entries`} lite={$t`Filter # words`}>
  <span class="font-bold">{formatNumber(stats.current.totalEntryCount)}</span>
</ViewT>
```

Both strings are extracted:
```po
msgid "Filter # entries"
msgid "Filter # words"
```

**Why two patterns?**
- `pt()` is simpler for plain text
- `ViewT` component allows wrapping child elements that need to stay the same (like the number formatting above)

**Context comments needed for both:**
- For the Classic version: note that Lite equivalent exists
- For the Lite version: note that Classic equivalent exists
- Mention which UI element contains the variable content (if any)

## Key Principles

### Conciseness with Completeness
- 2-3 short lines max
- Include all crucial details without filler
- One detail per line for clarity

### Context Over Assumptions
- Never assume translator knows the UI flow
- Explicitly state what happens when user clicks/selects
- Name the UI element type (button, dropdown, field label, etc.)

### Lite/Classic Terminology Mapping
Keep consistent translations of parallel terms:
- Lite: "Word", "Meaning", "Part of", "Component"
- Classic: "Entry", "Sense", "Complex Form", "Component"

Note: The terminology in the string itself indicates which view it's for. If you see "Word" or "Add new word", it's the Lite version.

### Domain Knowledge
- Explain lexicography concepts briefly
- Link to system behavior (e.g., "user-configurable" for types)
- Distinguish linguistic phenomena (inflection ≠ compounding)

## Accuracy Check - Critical!

**Before finalizing your context comments, verify:**

1. **Terminology accuracy**
   - Use correct linguistic/lexicography terms (not made-up)
   - Don't mix inflection with compounding (e.g., "walk→walking" is NOT a complex form)
   - Components are entries, not morphemes

2. **Related term references**
   - Cross-reference related UI terms (e.g., "Entry Only" ↔ "Sense")
   - Link Lite/Classic view equivalents ("Word" Lite = "Lexeme form" Classic)
   - Keep relationships consistent across all comments

3. **Consult domain experts**
   - For lexicography terms: check FwLite source code or FieldWorks documentation
   - For UI behavior: verify in actual component or ask team
   - For relationship fields: trace Entry.cs or ComplexFormComponent.cs logic

**Red flags:**
- ❌ "walk→walking" for complex forms (that's inflection, not compounds)
- ❌ Calling components "morphemes" (they're entries)
- ❌ "singular/plural" as complex form types (user-configurable, not fixed)
- ❌ Vague descriptions like "Show all entries matching criteria" for toggles/buttons

## View-Specific Terminology Reference

Below are the terms that are likely to indicate a string is for a specific view. Use them when reviewing and creating context comments.

If a string contains any of these Classic terms → it's a Classic string:
- Entry, Sense, Complex Form, Lexeme form, Citation form, Grammatical info.

If a string contains these Lite terms → it's a Lite string:
- Word, Meaning, Part of, Display as, Part of speech

## Files & References

**Source files with related context:**
- `src/lib/entry-editor/field-data.ts` - field labels & help
- `src/lib/entry-editor/object-editors/EntryEditorPrimitive.svelte` - entry editor fields
- `src/project/browse/filter/FieldSelect.svelte` - browse filters
- `backend/FwLite/MiniLcm/Models/Entry.cs` - entry data model
- `backend/FwLite/MiniLcm/Models/ComplexFormComponent.cs` - complex form relationships

**When in doubt:**
1. Check the .svelte component where string is used
2. Read the surrounding code/comments
3. Look up FieldWorks documentation for linguistic terms
4. Ask the team on Slack #lex-box-dev

## Checklist

- [ ] Run `pnpm run i18n:extract` in `frontend/viewer/`
- [ ] Identify all new `msgid` entries
- [ ] For each new string, decide: needs context?
- [ ] Write context comments (1-3 lines, specific)
- [ ] **Verify terminology accuracy** (critical!)
- [ ] Check cross-references are consistent
- [ ] Verify examples are correct (not from wrong phenomena)
- [ ] Remove/correct any misleading comments
- [ ] Stage changes to `src/locales/en.po`
- [ ] DO NOT COMMIT (let user decide)

## Example Workflow Output

```bash
# After i18n:extract, you see:
git diff src/locales/en.po | grep "^+msgid"
  +msgid "Bind to morpheme"
  +msgid "Show pronunciation"
  +msgid "Link to another entry"

# Evaluate each:
# "Bind to morpheme" - unclear UI context, needs comment
# "Show pronunciation" - obvious, maybe skip
# "Link to another entry" - vague, needs clarification

# Add context, verify accuracy, done!
```

---

**Last updated:** December 2025  
**For questions:** Check `.github/copilot-instructions.md` or ask in #dev
