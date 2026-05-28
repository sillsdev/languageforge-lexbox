import {msg} from 'svelte-i18n-lingui';
import {vt, type ViewText} from './view-text';

interface FieldData {
  helpId: string;
  label: ViewText;
}

interface EntityData {
  $label: ViewText;
  [key: string]: FieldData | ViewText;
}

export const entityConfig = {
  entry: {
    $label: vt(msg`Entry`, msg`Word`),
    lexemeForm: {
      label: vt(msg`Lexeme form`, msg`Word`),
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Lexeme_Form_field.htm',
    },
    citationForm: {
      label: vt(msg`Citation form`, msg`Display as`),
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Citation_Form_field.htm',
    },
    complexForms: {
      label: vt(msg`Complex forms`, msg`Part of`),
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Complex_Forms.htm',
    },
    complexFormTypes: {
      label: vt(msg`Complex form types`, msg`Uses components as`),
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Complex_Form_Type_field.htm',
    },
    components: {
      label: msg`Components`,
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Components_field.htm',
    },
    literalMeaning: {
      label: msg`Literal meaning`,
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Literal_Meaning_field.htm',
    },
    note: {
      label: msg`Note`,
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Note_field.htm',
    },
    publishIn: {
      label: vt(msg`Publish Entry in`, msg`Publish Word in`),
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Publication_Settings_flds/Publish_In_(Publication_Settings).htm',
    },
  },
  sense: {
    $label: msg`Sense`,
    gloss: {
      label: msg`Gloss`,
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Gloss_field_Sense.htm',
    },
    definition: {
      label: msg`Definition`,
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/definition_field.htm',
    },
    partOfSpeechId: {
      label: vt(msg`Grammatical info.`, msg`Part of speech`),
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Grammatical_Info_field.htm',
    },
    semanticDomains: {
      label: msg`Semantic domains`,
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/semantic_domains_field.htm',
    },
  },
  example: {
    $label: msg`Example`,
    sentence: {
      label: msg`Sentence`,
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/example_field.htm',
    },
    translations: {
      label: msg`Translation`,
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Translation_field.htm',
    },
    reference: {
      label: msg`Reference`,
      helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Reference_field.htm',
    },
  },
} as const satisfies Record<string, EntityData>;

export type EntityType = keyof typeof entityConfig;
export type EntityFields<T extends EntityType> = Exclude<keyof (typeof entityConfig[T]), '$label'> & string;
export type EntityConfig<T extends EntityType> = {$label: ViewText} & Record<EntityFields<T>, FieldData> & EntityData;
export type EntryFieldId = EntityFields<'entry'>;
export type SenseFieldId = EntityFields<'sense'>;
export type ExampleFieldId = EntityFields<'example'>;
export type FieldId = EntryFieldId | SenseFieldId | ExampleFieldId;

export function getEntityConfig<T extends EntityType>(entityType: T): EntityConfig<T> {
  return entityConfig[entityType] as EntityConfig<T>;
}

export function entityFieldIds<T extends EntityType>(entityType: T): EntityFields<T>[] {
  return Object.keys(entityConfig[entityType]).filter((k): k is EntityFields<T> => k !== '$label');
}
