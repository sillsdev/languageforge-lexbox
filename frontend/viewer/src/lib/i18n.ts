import type {FieldIds} from './entry-editor/field-data';
import type {WellKnownFieldId} from './config-types';

// type I18n = Record<WellKnownFieldId, string> & Record<Exclude<string, WellKnownFieldId>, string>;
type I18nKey = FieldIds;
/**
 * I18n type is used to specify which i18n group to use for a field. If empty, the default i18n is used.
 */
export type I18nType = 'fieldworks' | '';

const defaultI18n: Record<FieldIds, string> = {
  // entry
  'lexemeForm': 'Word',
  'citationForm': 'Display as',
  'complexForms': 'Part of',
  'complexFormTypes': 'Complex form types',
  'components': 'Made of',
  'literalMeaning': 'Literal meaning',
  'note': 'Note',

  // sense
  'definition': 'Definition',
  'gloss': 'Translation',
  'partOfSpeechId': 'Part of speech',
  'semanticDomains': 'Semantic domain',

  // example sentence
  'sentence': 'Sentence',
  'translation': 'Translation',
  'reference': 'Reference',

  'test': 'Test',
  'sense': 'Definition',
  'entry': 'Word',
  'entries': 'Words'
};

const fieldWorksI18n: Record<FieldIds, string> = {
  'lexemeForm': 'Lexeme form',
  'citationForm': 'Citation form',
  'complexForms': 'Complex forms',
  'complexFormTypes': 'Complex form types',
  'components': 'Components',
  'literalMeaning': 'Literal meaning',
  'note': 'Note',
  'definition': 'Definition',
  'gloss': 'Gloss',
  'partOfSpeechId': 'Grammatical info.',
  'semanticDomains': 'Semantic domain',
  'sentence': 'Sentence',
  'translation': 'Translation',
  'reference': 'Reference',
  'test': 'Test',
  'sense': 'Sense',
  'entry': 'Entry',
  'entries': 'Entries'
};

const i18nMap: Record<Exclude<I18nType, ''>, Partial<Record<FieldIds, string>>> = {
  fieldworks: fieldWorksI18n,
};

export function i18n(key: I18nKey, i18nType?: I18nType): string {
  if (!i18nType) return defaultI18n[key];
  return i18nMap[i18nType][key] ?? defaultI18n[key];
}

export function fieldName(fieldConfig: {id: string, name?: string}, i18nType?: I18nType): string {
  return fieldConfig.name ?? i18n(fieldConfig.id as WellKnownFieldId, i18nType);
}
