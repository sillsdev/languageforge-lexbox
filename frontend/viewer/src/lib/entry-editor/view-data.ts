import type {FieldIds} from './field-data';
import type {I18nType} from '../i18n';

interface FieldView {
  show: boolean;
  order: number;
}

export const allFields: Record<FieldIds, FieldView> = {
  //entry
  lexemeForm: {show: true, order: 1},
  citationForm: {show: true, order: 2},
  components: {show: true, order: 3},
  literalMeaning: {show: true, order: 4},
  note: {show: true, order: 5},

  //sense
  gloss: {show: true, order: 1},
  definition: {show: true, order: 2},
  partOfSpeechId: {show: true, order: 3},
  semanticDomains: {show: true, order: 4},

  //example sentence
  sentence: {show: true, order: 1},
  translation: {show: true, order: 2},
  reference: {show: true, order: 3},
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

      //sense
      gloss: {order: 2},
      definition: {order: 1},

      reference: {show: false},
    }
  }
];
const everythingView: View = {
  id: 'everything',
  i18nKey: '',
  label: 'Everything (FieldWorks)',
  fields: allFields,
};
export const views: View[] = [
  everythingView,
  ...viewDefinitions.map(view => {
    const fields: Record<FieldIds, FieldView> = recursiveSpread<typeof allFields>(allFields, view.fields);
    return {
      ...everythingView,
      ...view,
      fields: fields
    };
  })
];

function recursiveSpread<T extends Record<string, unknown>>(obj1: T, obj2: { [P in keyof T]?: Partial<T[P]> }): T {
  const result: Record<string, unknown> = {...obj1};
  for (const [key, value] of Object.entries(obj2)) {
    const currentValue = result[key];
    if (typeof currentValue === 'object' && currentValue !== null && typeof value === 'object' && value !== null) {
      result[key] = recursiveSpread(currentValue as Record<string, unknown>, value as Record<string, Partial<unknown>>);
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
  fields: Partial<Record<FieldIds, Partial<FieldView>>>;
}

export interface View extends ViewDefinition {
  fields: Record<FieldIds, FieldView>;
}
