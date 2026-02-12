/**
 * PROTOTYPE: Demonstrates LingUI inline translations for a platform.bible extension.
 *
 * This file shows how developers would write translatable strings inline in their
 * React components. The English text is co-located with the component code — no
 * need to go to a separate JSON file to see what a key means.
 *
 * At extraction time, `lingui extract` pulls these into PO files that CrowdIn
 * can manage. A build script then converts the translated PO files into the
 * `localizedStrings.json` format that platform.bible expects.
 *
 * NOTE: This file is for demonstration only. It won't actually run because we
 * don't have the full platform.bible runtime here. The purpose is to show that
 * LingUI's extraction can read these inline strings and produce PO output that
 * is compatible with CrowdIn and convertible to platform.bible's format.
 */

import { msg } from '@lingui/core/macro';

// ---------------------------------------------------------------------------
// 1. PLATFORM-FACING STRINGS (menus, settings, web view titles)
//
//    These need stable keys because platform.bible JSON (menus.json, etc.)
//    references them by key.  We use LingUI's explicit `id` to set the key,
//    while the `message` keeps the English text right here in the source.
// ---------------------------------------------------------------------------

export const platformStrings = {
  menuBrowseDictionary: msg({
    id: '%fwLiteExtension_menu_browseDictionary%',
    message: 'Browse FieldWorks dictionary',
  }),
  menuAddEntry: msg({
    id: '%fwLiteExtension_menu_addEntry%',
    message: 'Add to FieldWorks...',
  }),
  menuFindEntry: msg({
    id: '%fwLiteExtension_menu_findEntry%',
    message: 'Search in FieldWorks...',
  }),
  menuFindRelatedEntries: msg({
    id: '%fwLiteExtension_menu_findRelatedEntries%',
    message: 'Search for related words...',
  }),
  webViewTitleBrowse: msg({
    id: '%fwLiteExtension_webViewTitle_browseDictionary%',
    message: 'FieldWorks Lite',
  }),
  webViewTitleAddWord: msg({
    id: '%fwLiteExtension_webViewTitle_addWord%',
    message: 'Add to FieldWorks',
  }),
  webViewTitleFindWord: msg({
    id: '%fwLiteExtension_webViewTitle_findWord%',
    message: 'Search in FieldWorks',
  }),
  webViewTitleFindRelated: msg({
    id: '%fwLiteExtension_webViewTitle_findRelatedWords%',
    message: 'Find related words in FieldWorks',
  }),
  webViewTitleSelectDictionary: msg({
    id: '%fwLiteExtension_webViewTitle_selectDictionary%',
    message: 'Select FieldWorks dictionary',
  }),
  settingsAnalysisLanguage: msg({
    id: '%fwLiteExtension_projectSettings_analysisLanguage%',
    message: 'Analysis language',
  }),
  settingsDictionary: msg({
    id: '%fwLiteExtension_projectSettings_dictionary%',
    message: 'FieldWorks dictionary',
  }),
  settingsDictionaryDescription: msg({
    id: '%fwLiteExtension_projectSettings_dictionaryDescription%',
    message: 'The FieldWorks dictionary to use with this project',
  }),
  settingsTitle: msg({
    id: '%fwLiteExtension_projectSettings_title%',
    message: 'FieldWorks Lite settings',
  }),
};

// ---------------------------------------------------------------------------
// 2. WEBVIEW-INTERNAL STRINGS (UI labels, buttons, messages)
//
//    These are used only inside the extension's own React components.
//    Same pattern — explicit IDs with inline English text.
// ---------------------------------------------------------------------------

export const addWordStrings = {
  title: msg({
    id: '%fwLiteExtension_addWord_title%',
    message: 'Add entry to FieldWorks',
  }),
  buttonAdd: msg({
    id: '%fwLiteExtension_addWord_buttonAdd%',
    message: 'Add new entry',
  }),
  buttonSubmit: msg({
    id: '%fwLiteExtension_addWord_buttonSubmit%',
    message: 'Submit new entry',
  }),
};

export const commonStrings = {
  cancel: msg({
    id: '%fwLiteExtension_button_cancel%',
    message: 'Cancel',
  }),
  loading: msg({
    id: '%fwLiteExtension_dictionary_loading%',
    message: 'Loading...',
  }),
  noResults: msg({
    id: '%fwLiteExtension_dictionary_noResults%',
    message: 'No results',
  }),
  backToList: msg({
    id: '%fwLiteExtension_dictionary_backToList%',
    message: 'Back to list',
  }),
};

export const entryDisplayStrings = {
  headword: msg({
    id: '%fwLiteExtension_entryDisplay_headword%',
    message: 'Headword',
  }),
  gloss: msg({
    id: '%fwLiteExtension_entryDisplay_gloss%',
    message: 'Gloss',
  }),
  definition: msg({
    id: '%fwLiteExtension_entryDisplay_definition%',
    message: 'Definition',
  }),
  partOfSpeech: msg({
    id: '%fwLiteExtension_entryDisplay_partOfSpeech%',
    message: 'Part of speech',
  }),
  senses: msg({
    id: '%fwLiteExtension_entryDisplay_senses%',
    message: 'Senses',
  }),
};

export const dictionarySelectStrings = {
  select: msg({
    id: '%fwLiteExtension_dictionarySelect_select%',
    message: 'Select a dictionary',
  }),
  clear: msg({
    id: '%fwLiteExtension_dictionarySelect_clear%',
    message: 'Clear selection',
  }),
  confirm: msg({
    id: '%fwLiteExtension_dictionarySelect_confirm%',
    message: 'Confirm selection',
  }),
  loading: msg({
    id: '%fwLiteExtension_dictionarySelect_loading%',
    message: 'Loading dictionaries ...',
  }),
  noneFound: msg({
    id: '%fwLiteExtension_dictionarySelect_noneFound%',
    message: 'No dictionaries found',
  }),
  selected: msg({
    id: '%fwLiteExtension_dictionarySelect_selected%',
    message: 'Selected:',
  }),
  saved: msg({
    id: '%fwLiteExtension_dictionarySelect_saved%',
    message: 'Dictionary selection saved. You can close this window.',
  }),
  saveError: msg({
    id: '%fwLiteExtension_dictionarySelect_saveError%',
    message: 'Error saving dictionary selection:',
  }),
  saving: msg({
    id: '%fwLiteExtension_dictionarySelect_saving%',
    message: 'Saving dictionary selection',
  }),
};

export const errorStrings = {
  failedToAddEntry: msg({
    id: '%fwLiteExtension_error_failedToAddEntry%',
    message: 'Failed to add entry!',
  }),
  gettingNetworkObject: msg({
    id: '%fwLiteExtension_error_gettingNetworkObject%',
    message: 'Error getting network object:',
  }),
  missingParam: msg({
    id: '%fwLiteExtension_error_missingParam%',
    message: 'Missing required parameter: ',
  }),
};

export const findWordStrings = {
  textField: msg({
    id: '%fwLiteExtension_findWord_textField%',
    message: 'Find in dictionary...',
  }),
};

export const findRelatedWordStrings = {
  textField: msg({
    id: '%fwLiteExtension_findRelatedWord_textField%',
    message: 'Find related words in dictionary...',
  }),
  selectInstruction: msg({
    id: '%fwLiteExtension_findRelatedWord_selectInstruction%',
    message: 'Select a semantic domain for related words in that domain',
  }),
  noResultsInDomain: msg({
    id: '%fwLiteExtension_findRelatedWord_noResultsInDomain%',
    message: 'No entries in this semantic domain.',
  }),
};
