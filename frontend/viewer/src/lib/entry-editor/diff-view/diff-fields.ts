import {msg} from 'svelte-i18n-lingui';
import type {
  IComplexFormType,
  ICustomView,
  IMorphType,
  IPartOfSpeech,
  IPublication,
  ISemanticDomain,
  IWritingSystem,
} from '$lib/dotnet-types';
import type {EntityType} from '$lib/views/entity-config';

// 'text' is diffed character by character; the other scalars are replaced whole.
export type DiffRow = {label: string} & (
  | {kind: 'text' | 'multiString' | 'richMultiString' | 'boolean' | 'wsType' | 'viewBase' | 'wsList' | 'stringList'}
  | {kind: 'viewFields'; entity: EntityType}
);

/**
 * A row for every field of a generated model, or null for one a diff shouldn't show. Typed against the model,
 * so a new backend field fails type-checking until someone decides how its changes are shown. Entry, sense and
 * example fields are covered by entityConfig instead.
 */
export type DiffRows<T> = {[K in keyof T]-?: DiffRow | null};

const name = {label: msg`Name`, kind: 'multiString'} as const;
const abbreviation = {label: msg`Abbreviation`, kind: 'multiString'} as const;
const description = {label: msg`Description`, kind: 'richMultiString'} as const;

/** Rows for the objects without an editor layout, keyed by the entity type the backend reports for a snapshot. */
/* eslint-disable @typescript-eslint/naming-convention */ // C# type names
export const objectDiffRows: {
  PartOfSpeech: DiffRows<IPartOfSpeech>;
  SemanticDomain: DiffRows<ISemanticDomain>;
  Publication: DiffRows<IPublication>;
  ComplexFormType: DiffRows<IComplexFormType>;
  MorphType: DiffRows<IMorphType>;
  WritingSystem: DiffRows<IWritingSystem>;
  CustomView: DiffRows<ICustomView>;
} = {
  PartOfSpeech: {name, id: null, deletedAt: null, predefined: null},
  SemanticDomain: {
    name,
    abbreviation,
    code: {label: msg`Code`, kind: 'text'},
    description,
    ocmCodes: {label: msg`OCM codes`, kind: 'text'},
    louwNidaCodes: {label: msg`Louw-Nida codes`, kind: 'text'},
    id: null,
    deletedAt: null,
    predefined: null,
  },
  Publication: {name, isMain: {label: msg`Main publication`, kind: 'boolean'}, id: null, deletedAt: null},
  ComplexFormType: {name, id: null, deletedAt: null},
  MorphType: {
    name,
    abbreviation,
    description,
    prefix: {label: msg`Prefix`, kind: 'text'},
    postfix: {label: msg`Postfix`, kind: 'text'},
    // Fixed per project: a morph type's kind is its identity
    kind: null,
    secondaryOrder: null,
    id: null,
    deletedAt: null,
  },
  WritingSystem: {
    name: {label: msg`Name`, kind: 'text'},
    abbreviation: {label: msg`Abbreviation`, kind: 'text'},
    wsId: {label: msg`Writing system code`, kind: 'text'},
    type: {label: msg`Type`, kind: 'wsType'},
    isAudio: {label: msg`Audio`, kind: 'boolean'},
    font: {label: msg`Font`, kind: 'text'},
    exemplars: {label: msg`Exemplars`, kind: 'stringList'},
    systemCollationLocale: {label: msg`Sort language`, kind: 'text'},
    icuCollationRules: {label: msg`Sort rules`, kind: 'text'},
    id: null,
    deletedAt: null,
  },
  CustomView: {
    name: {label: msg`Name`, kind: 'text'},
    base: {label: msg`Base view`, kind: 'viewBase'},
    entryFields: {label: msg`Entry fields`, kind: 'viewFields', entity: 'entry'},
    senseFields: {label: msg`Sense fields`, kind: 'viewFields', entity: 'sense'},
    exampleFields: {label: msg`Example fields`, kind: 'viewFields', entity: 'example'},
    vernacular: {label: msg`Vernacular writing systems`, kind: 'wsList'},
    analysis: {label: msg`Analysis writing systems`, kind: 'wsList'},
    id: null,
    deletedAt: null,
  },
};
/* eslint-enable @typescript-eslint/naming-convention */

export type ObjectType = keyof typeof objectDiffRows;

export function isObjectType(entityType: string): entityType is ObjectType {
  return Object.hasOwn(objectDiffRows, entityType);
}
