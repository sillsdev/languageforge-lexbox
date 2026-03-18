import type {EntityFields, EntityType, EntryFieldId, ExampleFieldId, FieldId, SenseFieldId} from './entity-config';

import {ViewBase, type ICustomView, type IViewField} from '$lib/dotnet-types';
import type {Merge} from 'type-fest';

export interface TypedViewField<T extends FieldId> extends IViewField {
  fieldId: T;
  show: boolean;
}

export type View = Merge<Omit<ICustomView, 'deletedAt'>, {
  entryFields: TypedViewField<EntryFieldId>[];
  senseFields: TypedViewField<SenseFieldId>[];
  exampleFields: TypedViewField<ExampleFieldId>[];
}>;

export interface RootView extends View {
  parentView?: never
}

export interface CustomView extends View {
  parentView: RootView;
}

export function isCustomView(view: View): view is CustomView {
  return 'parentView' in view;
}

export type Overrides = Pick<Partial<View>, 'vernacular' | 'analysis'>;

/** Convert a field array to a record keyed by fieldId, for template lookups. */
export function fieldRecord<T extends FieldId>(fields: TypedViewField<T>[]): Partial<Record<T, TypedViewField<T>>> {
  return Object.fromEntries(fields.map(f => [f.fieldId, f])) as Partial<Record<T, TypedViewField<T>>>;
}

interface BuiltInFieldView {
  show: boolean;
  order: number;
}
type BuiltInEntityFields<T extends EntityType> = {[F in EntityFields<T>]: BuiltInFieldView};
type BuiltInFieldsView = {[T in EntityType]: BuiltInEntityFields<T>};

export const FW_LITE_VIEW: RootView = {
  id: ViewBase.FwLite,
  name: 'FieldWorks Lite',
  base: ViewBase.FwLite,
  ...builtInFieldsToViewFields({
    entry: {
      lexemeForm: {show: true, order: 1},
      citationForm: {show: true, order: 2},
      complexForms: {show: true, order: 3},
      complexFormTypes: {show: false, order: 4},
      components: {show: true, order: 5},
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
  }),
};

export const FW_CLASSIC_VIEW: RootView = {
  id: ViewBase.FieldWorks,
  name: 'FieldWorks Classic',
  base: ViewBase.FieldWorks,
  entryFields: FW_LITE_VIEW.entryFields.map(field => ({...field, show: true})),
  senseFields: FW_LITE_VIEW.senseFields.map(field => ({...field, show: true})),
  exampleFields: FW_LITE_VIEW.exampleFields.map(field => ({...field, show: true})),
};

export const BUILT_IN_VIEWS = [FW_LITE_VIEW, FW_CLASSIC_VIEW] as const;

function builtInFieldsToViewFields(fields: BuiltInFieldsView): Pick<View, 'entryFields' | 'senseFields' | 'exampleFields'> {
  return {
    entryFields: builtInEntityFieldsToViewFields(fields.entry),
    senseFields: builtInEntityFieldsToViewFields(fields.sense),
    exampleFields: builtInEntityFieldsToViewFields(fields.example),
  };
}

function builtInEntityFieldsToViewFields<T extends EntityType>(entityFields: BuiltInEntityFields<T>): TypedViewField<EntityFields<T> & FieldId>[] {
  return (Object.entries(entityFields) as [EntityFields<T>, BuiltInFieldView][])
    .toSorted(([, a], [, b]) => a.order - b.order)
    .map(([fieldId, config]) => ({fieldId: fieldId as EntityFields<T> & FieldId, show: config.show}));
}
