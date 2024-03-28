import type { FieldConfig, WellKnownFieldId } from './types';

const wellKnownFieldsI18n: Record<WellKnownFieldId, string> = {
  'lexemeForm': 'Lexeme form',
  'citationForm': 'Citation form',
  'literalMeaning': 'Literal meaning',
  'note': 'Note',
  'definition': 'Definition',
  'gloss': 'Gloss',
  'partOfSpeech': 'Part of speech',
  'semanticDomain': 'Semantic domain',
  'sentence': 'Sentence',
  'translation': 'Translation',
  'reference': 'Reference',
};

export function fieldName(fieldConfig: FieldConfig): string {
  return 'name' in fieldConfig ? fieldConfig.name : wellKnownFieldsI18n[fieldConfig.id as WellKnownFieldId];
}
