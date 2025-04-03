import type {FieldIds} from '$lib/entry-editor/field-data';
import type {I18nType} from '../i18n';

interface FieldView {
  show: boolean;
  order: number;
}

const defaultDef = Symbol('default spread values');

export const allFields: Record<FieldIds, FieldView> = {
  //entry
  lexemeForm: {show: true, order: 1},
  citationForm: {show: true, order: 2},
  complexForms: {show: false, order: 3},
  complexFormTypes: {show: false, order: 4},
  components: {show: false, order: 5},
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

export const FW_LITE_VIEW: RootView = {
  id: 'fwlite',
  type: 'fw-lite',
  i18nKey: '',
  label: 'FieldWorks Lite',
  fields: allFields,
  get alternateView() { return FW_CLASSIC_VIEW; }
};

export const FW_CLASSIC_VIEW: RootView = {
  id: 'fieldworks',
  type: 'fw-classic',
  i18nKey: 'fieldworks',
  label: 'FieldWorks',
  fields: recursiveSpread(allFields, {[defaultDef]: {show: true}}),
  alternateView: FW_LITE_VIEW,
};

const viewDefinitions: CustomViewDefinition[] = [
  // custom views
];

export const views: [RootView, RootView, ...CustomView[]] = [
  FW_LITE_VIEW,
  FW_CLASSIC_VIEW,
  ...viewDefinitions.map(view => {
    const fields: Record<FieldIds, FieldView> = recursiveSpread<typeof allFields>(allFields, view.fieldOverrides);
    return {
      ...FW_LITE_VIEW,
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
  type: 'fw-lite' | 'fw-classic';
  i18nKey: I18nType;
  label: string;
}

interface CustomViewDefinition extends ViewDefinition {
  fieldOverrides: Partial<Record<FieldIds, Partial<FieldView>>>;
  parentView: RootView;
}

interface ViewBase extends ViewDefinition {
  fields: Record<FieldIds, FieldView>;
}

interface RootView extends ViewBase {
  alternateView: RootView;
}

interface CustomView extends ViewBase {
  // Forcing this to be a RootView theoretically avoids potential cycles that might not include a RootView at all
  // A user can still start a new custom view based on an existing custom view, but they won't inherit from each other afterwards
  parentView: RootView;
}

export type View = (RootView | CustomView);
