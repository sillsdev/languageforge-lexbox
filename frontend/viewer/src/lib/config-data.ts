import type { BaseEntityFieldConfig, CustomFieldConfig } from "./types";
import type { IEntry, IExampleSentence, ISense } from "./mini-lcm";

export const entryFieldConfigs: BaseEntityFieldConfig<IEntry>[] = [
  { id: "lexemeForm", type: "multi", ws: "vernacular" },
  { id: "citationForm", type: "multi", ws: "vernacular" },
  { id: "literalMeaning", type: "multi", ws: "vernacular" },
  { id: "note", type: "multi", ws: "analysis" },
];

export const customEntryFieldConfigs: CustomFieldConfig[] = [
  { id: "entry-custom-001", type: "multi", ws: "vernacular", name: "Custom 1" },
];

export const senseFieldConfigs: BaseEntityFieldConfig<ISense>[] = [
  { id: "gloss", type: "multi", ws: "analysis" },
  { id: "definition", type: "multi",  ws: "analysis" },
  { id: "partOfSpeech",type: "option", optionType: "part-of-speech",  ws: "first-analysis" },
  { id: "semanticDomain", type: "multi-option", optionType: "semantic-domain", ws: "first-analysis" },
];

export const customSenseFieldConfigs: CustomFieldConfig[] = [
  { id: "sense-custom-001", type: "multi", ws: "first-analysis", name: "Custom sense" },
];

export const exampleFieldConfigs: BaseEntityFieldConfig<IExampleSentence>[] = [
  { id: "sentence", type: "multi", ws: "vernacular" },
  { id: "translation", type: "multi", ws: "analysis" },
  { id: "reference", type: "single", ws: "first-analysis" },
];

export const customExampleFieldConfigs: CustomFieldConfig[] = [
];

export const allFields = [
  ...entryFieldConfigs,
  ...customEntryFieldConfigs,
  ...senseFieldConfigs,
  ...customSenseFieldConfigs,
  ...exampleFieldConfigs,
  ...customExampleFieldConfigs,
];
