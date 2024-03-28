import type { BaseEntityFieldConfig, CustomFieldConfig } from './types';
import type { IEntry, IExampleSentence, ISense } from './mini-lcm';

const allFieldConfigs = {
  entry: {
    lexemeForm: { id: 'lexemeForm', type: 'multi', ws: 'vernacular' },
    citationForm: { id: 'citationForm', type: 'multi', ws: 'vernacular' },
    literalMeaning: { id: 'literalMeaning', type: 'multi', ws: 'vernacular' },
    note: { id: 'note', type: 'multi', ws: 'analysis' },
  },
  customEntry: {
    custom1: { id: 'entry-custom-001', type: 'multi', ws: 'vernacular', name: 'Custom 1' },
  },
  sense: {
    gloss: { id: 'gloss', type: 'multi', ws: 'analysis' },
    definition: { id: 'definition', type: 'multi', ws: 'analysis' },
    partOfSpeech: { id: 'partOfSpeech', type: 'option', optionType: 'part-of-speech', ws: 'first-analysis' },
    semanticDomain: { id: 'semanticDomain', type: 'multi-option', optionType: 'semantic-domain', ws: 'first-analysis' },
  },
  customSense: {
    custom1: { id: 'sense-custom-001', type: 'multi', ws: 'first-analysis', name: 'Custom sense' },
  },
  example: {
    sentence: { id: 'sentence', type: 'multi', ws: 'vernacular' },
    translation: { id: 'translation', type: 'multi', ws: 'analysis' },
    reference: { id: 'reference', type: 'single', ws: 'first-analysis' },
  },
  customExample: {
  },
}

export const entryFieldConfigs: BaseEntityFieldConfig<IEntry>[] = [
  { id: 'lexemeForm', type: 'multi', ws: 'vernacular' },
  { id: 'citationForm', type: 'multi', ws: 'vernacular' },
  { id: 'literalMeaning', type: 'multi', ws: 'vernacular' },
  { id: 'note', type: 'multi', ws: 'analysis' },
];

export const customEntryFieldConfigs: CustomFieldConfig[] = [
  { id: 'entry-custom-001', type: 'multi', ws: 'vernacular', name: 'Custom 1' },
];

export const senseFieldConfigs: BaseEntityFieldConfig<ISense>[] = [
  { id: 'gloss', type: 'multi', ws: 'analysis' },
  { id: 'definition', type: 'multi', ws: 'analysis' },
  { id: 'partOfSpeech', type: 'option', optionType: 'part-of-speech', ws: 'first-analysis' },
  { id: 'semanticDomain', type: 'multi-option', optionType: 'semantic-domain', ws: 'first-analysis' },
];

export const customSenseFieldConfigs: CustomFieldConfig[] = [
  { id: 'sense-custom-001', type: 'multi', ws: 'first-analysis', name: 'Custom sense' },
];

export const exampleFieldConfigs: BaseEntityFieldConfig<IExampleSentence>[] = [
  { id: 'sentence', type: 'multi', ws: 'vernacular' },
  { id: 'translation', type: 'multi', ws: 'analysis' },
  { id: 'reference', type: 'single', ws: 'first-analysis' },
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

export const views = [
  {
    label: 'Everything',
    value: allFieldConfigs,
  },
  {
    label: 'WeSay',
    value: {
      entry: {
        lexemeForm: allFieldConfigs.entry.lexemeForm,
        note: allFieldConfigs.entry.note,
      },
      sense: {
        gloss: allFieldConfigs.sense.gloss,
        definition: allFieldConfigs.sense.definition,
        partOfSpeech: allFieldConfigs.sense.partOfSpeech,
        semanticDomain: allFieldConfigs.sense.semanticDomain,
      },
      example: {
        sentence: allFieldConfigs.example.sentence,
        translation: allFieldConfigs.example.translation,
      },
    },
  }];
