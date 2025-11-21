import type {FieldId} from '$lib/entry-editor/field-data';

export interface FieldView {
  show: boolean;
  order: number;
}

const defaultDef = Symbol('default spread values');

export const allFields: Record<FieldId, FieldView> = {
  //entry
  lexemeForm: {show: true, order: 1},
  citationForm: {show: true, order: 2},
  complexForms: {show: true, order: 3},
  components: {show: true, order: 4},
  complexFormTypes: {show: false, order: 5},
  literalMeaning: {show: false, order: 6},
  note: {show: true, order: 7},
  publishIn: {show: false, order: 8},

  //sense
  gloss: {show: true, order: 1},
  definition: {show: true, order: 2},
  partOfSpeechId: {show: true, order: 3},
  semanticDomains: {show: true, order: 4},

  //example sentence
  sentence: {show: true, order: 1},
  translations: {show: true, order: 2},
  reference: {show: false, order: 3},
};

export const FW_LITE_VIEW: RootView = {
  id: 'fwlite',
  type: 'fw-lite',
  label: 'FieldWorks Lite',
  fields: allFields,
  get alternateView() { return FW_CLASSIC_VIEW; }
};

export const FW_CLASSIC_VIEW: RootView = {
  id: 'fieldworks',
  type: 'fw-classic',
  label: 'FieldWorks Classic',
  fields: recursiveSpread(allFields, {
    complexFormTypes: {order: allFields.components.order - 0.1},
    [defaultDef]: {show: true}
  }),
  alternateView: FW_LITE_VIEW,
};

const viewDefinitions: CustomViewDefinition[] = [
  // custom views
];

export const views: [RootView, RootView, ...CustomView[]] = [
  FW_LITE_VIEW,
  FW_CLASSIC_VIEW,
  ...viewDefinitions.map(view => {
    const fields: Record<FieldId, FieldView> = recursiveSpread<typeof allFields>(allFields, view.fieldOverrides);
    return {
      ...FW_LITE_VIEW,
      ...view,
      fields: fields
    };
  })
];

function recursiveSpread<T extends Record<string | symbol, unknown>>(obj1: T, obj2: { [P in keyof T]?: Partial<T[P]> } & { [defaultDef]?: Partial<T[keyof T]> }): T {
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

export type ViewType = 'fw-lite' | 'fw-classic';

interface ViewDefinition {
  id: string;
  type: ViewType;
  label: string;
}

export type Overrides = {
  analysisWritingSystems?: string[];
  vernacularWritingSystems?: string[];
};

interface CustomViewDefinition extends ViewDefinition {
  fieldOverrides: Partial<Record<FieldId, Partial<FieldView>>>;
  parentView: RootView;
}

interface ViewBase extends ViewDefinition {
  fields: Record<FieldId, FieldView>;
  overrides?: Overrides
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
