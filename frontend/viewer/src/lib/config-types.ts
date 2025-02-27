import type {
  IEntry,
  IExampleSentence,
  IMultiString,
  ISemanticDomain,
  ISense
} from '$lib/dotnet-types';

import type {ConditionalKeys} from 'type-fest';

export type WritingSystemType = 'vernacular' | 'analysis';
export type WritingSystemSelection = WritingSystemType | `first-${WritingSystemType}` | 'vernacular-analysis' | 'analysis-vernacular';
export type FieldType = 'multi' | 'single' | 'option' | 'multi-option';
export type WellKnownFieldId = Exclude<keyof (IEntry & ISense & IExampleSentence), 'id' | 'exampleSentences' | 'senses'>

export type BaseFieldConfig = {
  type: FieldType;
  id: string;
  name?: string;
  extra?: boolean;
  helpId?: string;
  ws: WritingSystemSelection;
  readonly?: true;
}

export type CustomFieldConfig = BaseFieldConfig & {
  name: string;
  custom: true;
}

export type OptionFieldConfig = {
  type: `option`;
  optionType: string;
  ws: `first-${WritingSystemType}`;
}

export type BaseEntityFieldConfig<T> = (({
  type: 'multi';
  id: ConditionalKeys<T, IMultiString>;
} | {
  type: 'single';
  id: ConditionalKeys<T, string | undefined>;
  ws: `first-${WritingSystemType}`;
} | {
  type: 'option',
  optionType: string,
  id: ConditionalKeys<T, string | undefined>,
  ws: `first-${WritingSystemType}`
} | {
  type: `multi-option`;
  optionType: string;
  id: ConditionalKeys<T, string[] | ISemanticDomain[]>;
  ws: `first-${WritingSystemType}`;
}) & BaseFieldConfig & {
  id: WellKnownFieldId
});

export type EntityFieldConfig = BaseEntityFieldConfig<IEntry> | BaseEntityFieldConfig<ISense> | BaseEntityFieldConfig<IExampleSentence>;
export type FieldConfig = EntityFieldConfig | CustomFieldConfig;

export type LexboxPermissions = {
  write: boolean,
  comment: boolean,
}
