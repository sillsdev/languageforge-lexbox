import type {I18nType} from '../i18n';
import type {FieldIds} from '$lib/entry-editor/field-data';

interface FieldView {
  show: boolean;
  order: number;
}

const defaultDef = Symbol('default spread values');

export const allFields: Record<FieldIds, FieldView> = {
  //entry
  lexemeForm: {show: true, order: 1},
  citationForm: {show: true, order: 2},
  complexForms: {show: true, order: 3},
  complexFormTypes: {show: false, order: 4},
  components: {show: true, order: 5},
  literalMeaning: {show: false, order: 6},
  note: {show: true, order: 7},

  //sense
  gloss: {show: true, order: 1},
  definition: {show: true, order: 2},
  partOfSpeechId: {show: true, order: 3},
  semanticDomains: {show: true, order: 4},

  //example sentence
  sentence: {show: true, order: 1},
  translation: {show: true, order: 2},
  reference: {show: false, order: 3},
};

const viewDefinitions: ViewDefinition[] = [
  {
    id: 'fieldworks',
    i18nKey: 'fieldworks',
    label: 'FieldWorks',
    fields: {[defaultDef]: {show: true}}
  },
  {
    id: 'wesay',
    i18nKey: 'weSay',
    label: 'WeSay',
    fields: {
      [defaultDef]: {show: false},
      lexemeForm: {show: true},

      //sense
      gloss: {show: true},
      partOfSpeechId: {show: true},

      //example sentence
      sentence: {show: true},
    }
  },
  {
    id: 'languageforge',
    i18nKey: 'languageForge',
    label: 'Language Forge',
    fields: {
      [defaultDef]: {show: false},
      lexemeForm: {show: true},

      //sense
      gloss: {show: true, order: 2},
      definition: {show: true, order: 1},
      partOfSpeechId: {show: true},
      semanticDomains: {show: true},

      //example sentence
      sentence: {show: true},
      translation: {show: true},
    }
  }
];
const everythingView: View = {
  id: 'fwlite',
  i18nKey: '',
  label: 'FieldWorks Lite',
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

function recursiveSpread<T extends Record<string | symbol, unknown>>(obj1: T, obj2: { [P in keyof T]?: Partial<T[P]> }): T {
  const result: Record<string, unknown> = {...obj1};
  const defaultValues = obj2[defaultDef];
  if (defaultValues) {
    for (const [key, value] of Object.entries(result)) {
      if (typeof value === 'object' && value !== null) {
        result[key] = {...value, ...defaultValues};
      } else {
        result[key] = defaultValues;
      }
    }
  }
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
