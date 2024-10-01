import type { FieldConfig, WellKnownFieldId } from './config-types';

import type {FieldIds} from './entry-editor/field-data';

type I18n = Record<WellKnownFieldId, string> & Record<Exclude<string, WellKnownFieldId>, string>;
type I18nKey = FieldIds;
/**
 * I18n type is used to specify which i18n group to use for a field. If empty, the default i18n is used.
 */
export type I18nType = 'weSay' | 'languageForge' | '';

const defaultI18n: Record<FieldIds, string> = {
  'lexemeForm': 'Lexeme form',
  'citationForm': 'Citation form',
  'components': 'Components',
  'literalMeaning': 'Literal meaning',
  'note': 'Note',
  'definition': 'Definition',
  'gloss': 'Gloss',
  'partOfSpeechId': 'Grammatical Info.',
  'semanticDomains': 'Semantic domain',
  'sentence': 'Sentence',
  'translation': 'Translation',
  'reference': 'Reference',
  'test': 'Test',
};

const weSayI18n = {
  'lexemeForm': 'Word',
  'gloss': 'Definition',
  'partOfSpeechId': 'Part of speech',
};

const languageForgeI18n = {
  'lexemeForm': 'Word',
  'partOfSpeechId': 'Part of speech',
};

const i18nMap: Record<Exclude<I18nType, ''>, Partial<Record<FieldIds, string>>> = {
  weSay: weSayI18n,
  languageForge: languageForgeI18n,
};

export function i18n(key: I18nKey, i18nType?: I18nType): string {
  if (!i18nType) return defaultI18n[key];
  return i18nMap[i18nType][key] ?? defaultI18n[key];
}

export function fieldName(fieldConfig: {id: string, name?: string}, i18nType?: I18nType): string {
  return fieldConfig.name ?? i18n(fieldConfig.id as WellKnownFieldId, i18nType);
}
