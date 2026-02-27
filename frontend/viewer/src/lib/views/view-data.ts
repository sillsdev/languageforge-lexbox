import type {EntityFields, EntityType, FieldId} from './fields';

import type {PartialDeep} from 'type-fest';

export interface FieldView {
  show: boolean;
  order: number;
}

export type ViewFields = {[T in EntityType]: {[F in EntityFields<T>]: FieldView}};

export const allFields: ViewFields = {
  entry: {
    lexemeForm: {show: true, order: 1},
    citationForm: {show: true, order: 2},
    complexForms: {show: true, order: 3},
    components: {show: true, order: 4},
    complexFormTypes: {show: false, order: 5},
    literalMeaning: {show: false, order: 6},
    note: {show: true, order: 7},
    publishIn: {show: false, order: 8},
  },
  sense: {
    gloss: {show: true, order: 1},
    definition: {show: true, order: 2},
    partOfSpeechId: {show: true, order: 3},
    semanticDomains: {show: true, order: 4},
  },
  example: {
    sentence: {show: true, order: 1},
    translations: {show: true, order: 2},
    reference: {show: false, order: 3},
  },
};

export type EntityViewFields = ViewFields[EntityType];

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
  fields: {
    entry: showAllFields(allFields.entry, {complexFormTypes: {order: allFields.entry.components.order - 0.1}}),
    sense: showAllFields(allFields.sense),
    example: showAllFields(allFields.example),
  },
  alternateView: FW_LITE_VIEW,
};

const viewDefinitions: CustomViewDefinition[] = [
  // custom views
];

export const views: [RootView, RootView, ...CustomView[]] = [
  FW_LITE_VIEW,
  FW_CLASSIC_VIEW,
  ...viewDefinitions.map(view => ({
    ...FW_LITE_VIEW,
    ...view,
    fields: mergeViewFields(allFields, view.fieldOverrides),
  }))
];

function mergeFields<T>(base: T, overrides?: Partial<Record<FieldId, Partial<FieldView>>>): T {
  if (!overrides) return {...base};
  const result = {...base};
  for (const [id, override] of Object.entries(overrides) as [keyof T, Partial<FieldView>][]) {
    result[id] = {...result[id], ...override};
  }
  return result;
}

function mergeViewFields(base: ViewFields, overrides: CustomViewDefinition['fieldOverrides']): ViewFields {
  return {
    entry: mergeFields(base.entry, overrides.entry),
    sense: mergeFields(base.sense, overrides.sense),
    example: mergeFields(base.example, overrides.example),
  };
}

function showAllFields<T extends EntityViewFields>(fields: T, overrides?: PartialDeep<T>): T {
  const allShown = Object.fromEntries(
    Object.entries(fields).map(([id, field]) => [id, {...(field), show: true}])
  ) as T;
  return mergeFields(allShown, overrides);
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
  fieldOverrides: PartialDeep<ViewFields>;
  parentView: RootView;
}

interface ViewBase extends ViewDefinition {
  fields: ViewFields;
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
