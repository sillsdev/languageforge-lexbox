import type {FieldIds} from './field-data';
import type {I18nType} from '../i18n';

interface FieldView {
  show: boolean;
}

export const allFields: Record<FieldIds, FieldView> = {
  lexemeForm: {show: true},
  citationForm: {show: true},
  literalMeaning: {show: true},
  note: {show: true},
  gloss: {show: true},
  definition: {show: true},
  partOfSpeechId: {show: true},
  semanticDomains: {show: true},
  sentence: {show: true},
  translation: {show: true},
  reference: {show: true},
};

const viewDefinitions: ViewDefinition[] = [
  {
    id: 'wesay',
    i18nKey: 'weSay',
    label: 'WeSay',
    fields: {
      citationForm: {show: false},
      literalMeaning: {show: false},
      note: {show: false},
      semanticDomains: {show: false},
      definition: {show: false},
      translation: {show: false},
      reference: {show: false},
    }
  },
  {
    id: 'languageforge',
    i18nKey: 'languageForge',
    label: 'Language Forge',
    fields: {
      citationForm: {show: false},
      literalMeaning: {show: false},
      note: {show: false},
      reference: {show: false},
    }
  }
];

export const views: View[] = [
  {
    id: 'everything',
    i18nKey: '',
    label: 'Everything (FieldWorks)',
    fields: allFields,
  },
  ...viewDefinitions.map(view => {
    return {
      ...view,
      fields: recursiveSpread(allFields, view.fields)
    };
  })
];

function recursiveSpread<T extends Record<string, unknown>>(obj1: T, obj2: Partial<T>): T {
  const result: Record<string, unknown> = {...obj1};
  for (const [key, value] of Object.entries(obj2)) {
    const currentValue = result[key];
    if (typeof currentValue === 'object' && currentValue !== null && typeof value === 'object' && value !== null) {
      result[key] = recursiveSpread(currentValue as Record<string, unknown>, value as Record<string, unknown>);
    } else {
      result[key] = value;
    }
  }
  return result as T;
}

interface ViewDefinition {
  id: string;
  i18nKey: I18nType;
  label: string;
  fields: Partial<Record<FieldIds, FieldView>>;
}

export interface View extends ViewDefinition {
  fields: Record<FieldIds, FieldView>;
}
