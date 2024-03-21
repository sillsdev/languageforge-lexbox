import type { IEntry, IExampleSentence, IMultiString, ISense } from "./mini-lcm";

import type { ConditionalKeys } from "type-fest";

export type WritingSystemType = "vernacular" | "analysis";
export type WritingSystemSelection = WritingSystemType | `first-${WritingSystemType}` | "vernacular-analysis" | "analysis-vernacular";
export type FieldType = 'multi' | 'single' | 'option' | 'multi-option';
export type WellKnownFieldId = Exclude<keyof (IEntry & ISense & IExampleSentence), 'id' | 'exampleSentences' | 'senses'>

type BaseFieldConfig = {
  type: FieldType;
  id: string;
  ws: WritingSystemSelection;
}

export type CustomFieldConfig = BaseFieldConfig & {
  name: string;
}

export type BaseEntityFieldConfig<T> = (({
  type: 'multi';
  id: ConditionalKeys<T, IMultiString>;
} | {
  type: 'single';
  id: ConditionalKeys<T, string>;
  ws: `first-${WritingSystemType}`;
} | {
  type: `option`;
  optionType: string;
  id: ConditionalKeys<T, string>;
  ws: `first-${WritingSystemType}`;
} | {
  type: `multi-option`;
  optionType: string;
  id: ConditionalKeys<T, string[]>;
  ws: `first-${WritingSystemType}`;
}) & BaseFieldConfig & { id: WellKnownFieldId});

export type EntityFieldConfig = BaseEntityFieldConfig<IEntry> | BaseEntityFieldConfig<ISense> | BaseEntityFieldConfig<IExampleSentence>;
export type FieldConfig = EntityFieldConfig | CustomFieldConfig;
