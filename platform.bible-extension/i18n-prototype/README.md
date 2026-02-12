# i18n Prototype: LingUI + CrowdIn + platform.bible

This prototype demonstrates that LingUI can be used for inline translation
authoring in a platform.bible extension, with PO files managed by CrowdIn, and
a build step that generates the `localizedStrings.json` format platform.bible
requires.

## The problem

platform.bible extensions declare translatable strings in a specific JSON format
(`contributions/localizedStrings.json`), where each string is identified by a
`%key%`-wrapped identifier. Today, developers maintain this JSON file manually —
editing a separate file for every string change and having no co-located English
text in the component source.

The FwLite Svelte viewer already uses **LingUI**, which lets you write English
text inline in the component source (`{$t\`Cancel\`}`). We want the same DX for
the platform.bible React extension while remaining compatible with:

1. **platform.bible's `localizedStrings.json`** format (required for menus,
   settings, web view titles, and in-component strings)
2. **CrowdIn** for translation management (already used for the viewer)

## The solution

```
┌─────────────────────────────┐
│  React components (.tsx)    │  Developer writes inline English text
│                             │  using LingUI's msg() macro with
│  msg({                      │  explicit IDs matching platform.bible's
│    id: '%key%',             │  %key% convention.
│    message: 'English text'  │
│  })                         │
└─────────────┬───────────────┘
              │ lingui extract
              ▼
┌─────────────────────────────┐
│  PO files (per locale)      │  Standard gettext format.
│                             │  CrowdIn natively supports these.
│  msgid "%key%"              │
│  msgstr "English text"      │  ← en.po has English values
│                             │  ← es.po has Spanish values (from CrowdIn)
└─────────────┬───────────────┘
              │ po-to-platform-bible.mjs
              ▼
┌─────────────────────────────┐
│  localizedStrings.json      │  Exact format platform.bible expects.
│                             │
│  { "localizedStrings": {    │
│      "en": { "%key%": ... },│
│      "es": { "%key%": ... } │
│    }                        │
│  }                          │
└─────────────────────────────┘
```

## What this prototype proves

### 1. LingUI extraction works with explicit IDs

The `msg()` macro accepts an explicit `id` parameter. When the ID follows
platform.bible's `%key%` convention, `lingui extract` produces PO files where:

- `msgid` = the platform.bible key (e.g., `%fwLiteExtension_button_cancel%`)
- `msgstr` = the English source text (e.g., `Cancel`)

This means the English text lives **inline in the source code**, not in a
separate JSON file.

### 2. The PO files are CrowdIn-compatible

The generated PO files are standard gettext format — the same format the FwLite
viewer already uses with CrowdIn. The CrowdIn workflow would be:

1. Upload `en.po` as the source file
2. CrowdIn shows translators the English `msgstr` values
3. Translators fill in `msgstr` values in other locale PO files
4. Download translated PO files

### 3. PO → localizedStrings.json conversion produces valid output

The `scripts/po-to-platform-bible.mjs` script reads all locale PO files and
generates a `localizedStrings.json` that:

- Has the exact structure platform.bible expects (`metadata` + `localizedStrings`)
- Contains all 41 keys from the current extension
- **All English values are identical** to the current hand-maintained
  `contributions/localizedStrings.json`

### 4. The LOCALIZED_STRING_KEYS array is auto-generated

The conversion script also generates `localized-string-keys.ts`, eliminating the
need to manually maintain the key list.

## Running the prototype

```bash
cd platform.bible-extension/i18n-prototype
npm install
npm run extract    # LingUI extracts strings from src/ → PO files
npm run generate   # Converts PO files → localizedStrings.json
npm run pipeline   # Both steps in sequence
```

## Developer experience comparison

### Before (current)

To add a new translatable string, the developer must:

1. Choose a key name (e.g., `%fwLiteExtension_newFeature_label%`)
2. Add it to `contributions/localizedStrings.json` with the English text
3. Add it to `src/types/localized-string-keys.ts`
4. Reference it in the component: `localizedStrings['%fwLiteExtension_newFeature_label%']`

The English text is only visible in the JSON file, not at the point of use.

### After (with LingUI)

1. Write the string inline in the component:
   ```tsx
   const strings = {
     label: msg({
       id: '%fwLiteExtension_newFeature_label%',
       message: 'My new feature',
     }),
   };
   ```
2. Run `lingui extract` → PO files updated automatically
3. Run the conversion script → `localizedStrings.json` and key list regenerated
4. Use in the component: `localizedStrings[strings.label.id]`

The English text is **co-located with the component code**. The JSON file and
key list are generated, not hand-maintained.

## Files

| File | Purpose |
|------|---------|
| `lingui.config.ts` | LingUI configuration (locales, paths) |
| `src/sample-component.tsx` | All 41 extension strings defined with inline English text |
| `src/locales/en.po` | **Generated** — English PO catalog (CrowdIn source) |
| `src/locales/es.po` | **Generated** — Spanish PO catalog (with sample translations) |
| `src/locales/fr.po` | **Generated** — French PO catalog (empty, ready for CrowdIn) |
| `scripts/po-to-platform-bible.mjs` | Converts PO catalogs → `localizedStrings.json` |
| `output/localizedStrings.json` | **Generated** — platform.bible format (matches current) |
| `output/localized-string-keys.ts` | **Generated** — TypeScript key list |

## CrowdIn integration

The existing CrowdIn configuration (`crowdin/crowdin.yml`) already manages PO
files for the FwLite viewer. Adding the extension's PO file would be a single
line:

```yaml
files:
  - source: "frontend/viewer/src/locales/en.po"
    translation: "frontend/viewer/src/locales/%two_letters_code%.po"
  # Add this:
  - source: "platform.bible-extension/i18n-prototype/src/locales/en.po"
    translation: "platform.bible-extension/i18n-prototype/src/locales/%two_letters_code%.po"
```

## Tradeoffs and considerations

### You still need explicit IDs

Unlike the FwLite viewer where LingUI auto-generates hash IDs from the source
text (`{$t\`Cancel\`}` → hash), platform.bible **requires** stable, namespaced
`%key%` identifiers because:

- `menus.json` references keys by name (e.g., `"label": "%fwLiteExtension_menu_browseDictionary%"`)
- The platform's `useLocalizedStrings` hook looks up strings by key
- Keys must be globally unique across all extensions

So the DX improvement is **co-location of English text** (you see what the key
means right where you use it), not elimination of keys entirely.

### Build step required

The PO → JSON conversion must run as a build step. This could be integrated
into the existing webpack build or as a pre-build npm script.

### Runtime unchanged

Components still consume strings via `useLocalizedStrings` — the runtime API
doesn't change. The improvement is entirely in the **authoring and translation
management** workflow.
