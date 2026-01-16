# Crowdin Translation Management

This directory manages the translation workflow for LexBox/FieldWorks Lite using the Crowdin CLI.

## Machine Translation (MT) Coverage

Crowdin is configured to automatically pre-translate certain languages using MT engines (e.g., Crowdin Translate, DeepL).

**Supported by Crowdin MT:**
- French (fr)
- Indonesian (id)
- Korean (ko)
- Spanish (es)
- Vietnamese (vi)

**Needs Local AI Translation:**
Any language supported by LexBox/FieldWorks Lite that is *not* in the list above must be translated locally (e.g., using AI-assisted scripts) before being pushed to Crowdin or committed.

## Workflow

1.  **Extract strings:** Run `task i18n` in the `frontend/viewer` directory.
2.  **Local AI Translation:** (If needed) Translate new strings for non-MT languages.
3.  **Push to Crowdin:**
    - **Sources:** `task c:crowdin -- push sources` (to upload new strings from `en.po`)
    - **Translations:** `task c:crowdin -- push translations -l <lang_id>` (to upload local AI translations)
    *Note: `push translations` only supports one language at a time.*
4.  **Pull from Crowdin:** `task c:crowdin -- pull` (to get MT or human-reviewed translations)

## Dry Run
To see what would happen for both sources and a specific translation (defaults to Swahili):
```bash
task c:dryrun
```
To dry run a different language:
```bash
task c:dryrun LOCALE=<lang_id>
```

## Resources

- [Crowdin CLI Configuration Reference](https://crowdin.github.io/crowdin-cli/configuration)
- [Official Configuration File Docs](https://support.crowdin.com/developer/configuration-file/)
- [Project MT Settings](https://crowdin.com/project/language-depot/settings#project-pre-translate)
- [Machine Translation Engines Profile](https://crowdin.com/profile/tim_haasdyk/machine-translation)
