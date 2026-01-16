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

**Needs Local AI Translation (non-MT):**
- Malay (ms)
- Swahili (sw)

The workflow of adding context and doing AI translation is documented in/handled by:
**[.github/prompts/fwlite-translations_full-update.prompt.md](../.github/prompts/fwlite-translations_full-update.prompt.md)**

## Resources

- [Crowdin CLI Configuration Reference](https://crowdin.github.io/crowdin-cli/configuration)
- [Official Configuration File Docs](https://support.crowdin.com/developer/configuration-file/)
- [Project MT Settings](https://crowdin.com/project/language-depot/settings#project-pre-translate)
- [Machine Translation Engines Profile](https://crowdin.com/profile/tim_haasdyk/machine-translation)
