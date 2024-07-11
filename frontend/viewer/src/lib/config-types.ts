import type { IEntry, IExampleSentence, IMultiString, ISense, SemanticDomain } from './mini-lcm';

import type { ConditionalKeys } from 'type-fest';
import type { LexboxApiFeatures } from './services/lexbox-api';
import type { views } from './config-data';

export type WritingSystemType = 'vernacular' | 'analysis';
export type WritingSystemSelection = WritingSystemType | `first-${WritingSystemType}` | 'vernacular-analysis' | 'analysis-vernacular';
export type FieldType = 'multi' | 'single' | 'option' | 'multi-option';
export type WellKnownFieldId = Exclude<keyof (IEntry & ISense & IExampleSentence), 'id' | 'exampleSentences' | 'senses'>

type BaseFieldConfig = {
  type: FieldType;
  id: string;
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

export type OptionFieldValue = {id: string};

export type BaseEntityFieldConfig<T> = (({
  type: 'multi';
  id: ConditionalKeys<T, IMultiString>;
} | {
  type: 'single';
  id: ConditionalKeys<T, string>;
  ws: `first-${WritingSystemType}`;
} | (OptionFieldConfig & {id: ConditionalKeys<T, string>}) | {
  type: `multi-option`;
  optionType: string;
  id: ConditionalKeys<T, string[] | SemanticDomain[]>;
  ws: `first-${WritingSystemType}`;
}) & BaseFieldConfig & {
  id: WellKnownFieldId,
  helpId: string,
});

export type EntityFieldConfig = BaseEntityFieldConfig<IEntry> | BaseEntityFieldConfig<ISense> | BaseEntityFieldConfig<IExampleSentence>;
export type FieldConfig = (EntityFieldConfig & ViewConfigFieldProps) | CustomFieldConfig;

export type ViewConfigFieldProps = {
  extra?: true,
};

export type LexboxFeatures = LexboxApiFeatures;

export type LexboxPermissions = {
  write: boolean,
  comment: boolean,
}

export type ViewOptions = {
  generateExternalChanges: boolean,
  showExtraFields: boolean,
  hideEmptyFields: boolean,
  activeView: typeof views[number],
}

export type ViewConfig = ViewOptions & {
  readonly?: boolean,
}
