import type { FieldConfig, WellKnownFieldId } from './types';

type I18n = Record<WellKnownFieldId, string> & Record<Exclude<string, WellKnownFieldId>, string>;
type I18nKey = keyof typeof defaultI18n;
export type I18nType = keyof typeof i18nMap.other;

const defaultI18n = {
  'lexemeForm': 'Lexeme form',
  'citationForm': 'Citation form',
  'literalMeaning': 'Literal meaning',
  'note': 'Note',
  'definition': 'Definition',
  'gloss': 'Gloss',
  'partOfSpeech': 'Grammatical Info.',
  'semanticDomain': 'Semantic domain',
  'sentence': 'Sentence',
  'translation': 'Translation',
  'reference': 'Reference',
  'test': 'Test',
};

const weSayI18n = {
  'lexemeForm': 'Word',
  'gloss': 'Definition',
  'partOfSpeech': 'Part of speech',
};

const languageForgeI18n = {
  'lexemeForm': 'Word',
  'partOfSpeech': 'Part of speech',
};

const i18nMap = ({
  base: defaultI18n,
  other: {
    weSay: weSayI18n,
    languageForge: languageForgeI18n,
  },
} as const) satisfies {
  base: I18n,
  other: Record<string, Partial<I18n>>,
};

export function i18n(key: I18nKey, i18nType?: I18nType): string {
  const currI18n = i18nType ? {
    ...defaultI18n,
    ...i18nMap.other[i18nType],
  } : defaultI18n;
  return currI18n[key];
}

export function fieldName(fieldConfig: FieldConfig, i18nType?: I18nType): string {
  return 'name' in fieldConfig ? fieldConfig.name : i18n(fieldConfig.id as WellKnownFieldId, i18nType);
}
