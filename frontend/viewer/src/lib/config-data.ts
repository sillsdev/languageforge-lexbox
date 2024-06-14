import type { BaseEntityFieldConfig, CustomFieldConfig, FieldConfig, ViewConfigFieldProps } from './config-types';
import type { IEntry, IExampleSentence, ISense } from './mini-lcm';

import type { I18nType } from './i18n';

const allFieldConfigs = ({
  entry: {
    lexemeForm: { id: 'lexemeForm', type: 'multi', ws: 'vernacular', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Lexeme_Form_field.htm' },
    citationForm: { id: 'citationForm', type: 'multi', ws: 'vernacular', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Citation_Form_field.htm' },
    literalMeaning: { id: 'literalMeaning', type: 'multi', ws: 'vernacular', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Literal_Meaning_field.htm' },
    note: { id: 'note', type: 'multi', ws: 'analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Note_field.htm' },
  },
  customEntry: {
    custom1: { id: 'entry-custom-001', type: 'multi', ws: 'vernacular', name: 'Custom 1', custom: true },
  },
  sense: {
    gloss: { id: 'gloss', type: 'multi', ws: 'analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Gloss_field_Sense.htm' },
    definition: { id: 'definition', type: 'multi', ws: 'analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/definition_field.htm' },
    partOfSpeech: { id: 'partOfSpeech', type: 'option', optionType: 'part-of-speech', ws: 'first-analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Grammatical_Info_field.htm' },
    semanticDomain: { id: 'semanticDomain', type: 'multi-option', optionType: 'semantic-domain', ws: 'first-analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/semantic_domains_field.htm' }
  },
  customSense: {
    custom1: { id: 'sense-custom-001', type: 'multi', ws: 'first-analysis', name: 'Custom sense', custom: true },
  },
  example: {
    sentence: { id: 'sentence', type: 'multi', ws: 'vernacular', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/example_field.htm' },
    translation: { id: 'translation', type: 'multi', ws: 'analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Translation_field.htm' },
    reference: { id: 'reference', type: 'single', ws: 'first-analysis', helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Reference_field.htm' },
  },
  customExample: {
  },
} as const) satisfies {
  entry: Record<string, BaseEntityFieldConfig<IEntry>>,
  customEntry: Record<string, CustomFieldConfig>,
  sense: Record<string, BaseEntityFieldConfig<ISense>>,
  customSense: Record<string, CustomFieldConfig>,
  example: Record<string, BaseEntityFieldConfig<IExampleSentence>>,
  customExample: Record<string, CustomFieldConfig>,
};

export function allFields(viewConfig: ViewConfig): FieldConfig[] {
  return [
    ...Object.values(viewConfig.entry),
    ...Object.values(viewConfig.customEntry ?? {}),
    ...Object.values(viewConfig.sense),
    ...Object.values(viewConfig.customSense ?? {}),
    ...Object.values(viewConfig.example),
    ...Object.values<never>(viewConfig.customExample ?? {}),
  ];
}

type FieldsWithViewConfigProps<T extends Record<string, NonNullable<object>>> =
  { [K in keyof T]: FieldWithViewConfigProps<T[K]> };

type FieldWithViewConfigProps<T extends NonNullable<object>> =
  T & ViewConfigFieldProps;

interface ViewConfig {
  label: string,
  i18n?: I18nType,
  entry: Partial<FieldsWithViewConfigProps<typeof allFieldConfigs.entry>>,
  sense: Partial<typeof allFieldConfigs.sense>,
  example: Partial<typeof allFieldConfigs.example>,
  customEntry?: Partial<typeof allFieldConfigs.customEntry>,
  customSense?: Partial<typeof allFieldConfigs.customSense>,
  customExample?: Partial<typeof allFieldConfigs.customExample>,
}

function configure<T extends NonNullable<object>>(fieldConfig: T, props: ViewConfigFieldProps): FieldWithViewConfigProps<T> {
  return {...fieldConfig, ...props};
}

export const views: ViewConfig[] = [
  {
    label: 'Everything (FieldWorks)',
    ...allFieldConfigs,
    entry: {
      ...allFieldConfigs.entry,
      literalMeaning: configure(allFieldConfigs.entry.literalMeaning, { extra: true }),
    },
  },
  {
    label: 'WeSay',
    i18n: 'weSay',
    entry: {
      lexemeForm: allFieldConfigs.entry.lexemeForm,
      note: configure(allFieldConfigs.entry.note, { extra: true }),
    },
    sense: {
      gloss: allFieldConfigs.sense.gloss,
      partOfSpeech: allFieldConfigs.sense.partOfSpeech,
      semanticDomain: configure(allFieldConfigs.sense.semanticDomain, { extra: true }),
    },
    example: {
      sentence: allFieldConfigs.example.sentence,
    },
  },
  {
    label: 'Language Forge',
    i18n: 'languageForge',
    entry: {
      lexemeForm: allFieldConfigs.entry.lexemeForm,
      note: configure(allFieldConfigs.entry.note, { extra: true }),
      literalMeaning: configure(allFieldConfigs.entry.literalMeaning, { extra: true }),
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
      reference: configure(allFieldConfigs.example.reference, { extra: true }),
    },
  }] as const;
