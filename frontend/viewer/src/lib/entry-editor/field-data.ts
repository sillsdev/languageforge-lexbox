
interface FieldData {
  helpId: string;
}

const privateFieldData = {
  //entry
  lexemeForm: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Lexeme_Form_field.htm' },
  citationForm: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Citation_Form_field.htm' },
  components: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Components_field.htm' },
  literalMeaning: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Literal_Meaning_field.htm' },
  note: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Entry_level_fields/Note_field.htm' },

  //sense
  gloss: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Gloss_field_Sense.htm' },
  definition: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/definition_field.htm' },
  partOfSpeechId: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Grammatical_Info_field.htm' },
  semanticDomains: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/semantic_domains_field.htm' },

  //example sentence
  sentence: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/example_field.htm' },
  translation: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Translation_field.htm' },
  reference: { helpId: 'User_Interface/Field_Descriptions/Lexicon/Lexicon_Edit_fields/Sense_level_fields/Reference_field.htm' },
} satisfies Record<string, FieldData>;
/**
 * This is a list of all standard fields with fixed data that is constant across all views.
 */
export const fieldData: Record<string, FieldData> = privateFieldData;
export type FieldIds = keyof typeof fieldData;
