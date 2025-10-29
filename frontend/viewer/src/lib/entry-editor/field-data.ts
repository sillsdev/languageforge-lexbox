import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';

interface FieldData {
  helpId: string;
}

type PotentialFieldIds = keyof IEntry | keyof ISense | keyof IExampleSentence;

const privateFieldData = {
  //entry
  lexemeForm: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Lexeme_Form_field.htm' },
  citationForm: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Citation_Form_field.htm' },
  complexForms: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Complex_Forms.htm' },
  complexFormTypes: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Complex_Form_Type_field.htm' },
  components: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Components_field.htm' },
  literalMeaning: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Literal_Meaning_field.htm' },
  note: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Note_field.htm' },
  publishIn: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Publication_Settings_flds/Publish_In_(Publication_Settings).htm' },

  //sense
  gloss: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Gloss_field_Sense.htm' },
  definition: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/definition_field.htm' },
  partOfSpeechId: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Grammatical_Info_field.htm' },
  semanticDomains: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/semantic_domains_field.htm' },

  //example sentence
  sentence: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/example_field.htm' },
  translations: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Translation_field.htm' },
  reference: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Reference_field.htm' },
} satisfies Partial<Record<PotentialFieldIds, FieldData>>;
/**
 * This is a list of all standard fields with fixed data that is constant across all views.
 */
export type FieldId = keyof typeof privateFieldData;
export const fieldData: Record<FieldId, FieldData> = privateFieldData;

/**
 * @deprecated I think we want a type that's more type safe i.e. FieldId 👆
 */
export type FieldIds = string; // this was always just string, it just wasn't obvious 🙃
