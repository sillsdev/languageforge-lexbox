import type { IEntry, WritingSystems } from './mini-lcm';

export const writingSystems: WritingSystems = {
  vernacular: [{
    id: 'Ipa',
    name: 'Ipa',
    abbreviation: 'Ipa',
    font: 'Ipa',
  }, {
    id: 'Tha',
    name: 'Tha',
    abbreviation: 'Tha',
    font: 'Tha',
  }],
  analysis: [{
    id: 'Sen',
    name: 'Sen',
    abbreviation: 'Sen',
    font: 'Sen',
  }, {
    id: 'Tha',
    name: 'Tha',
    abbreviation: 'Tha',
    font: 'Tha',
  }],
};

export const entries: IEntry[] = [
  {
    id: '1',
    lexemeForm: {
      Ipa: 'Ipsum lexorem',
      Tha: 'Tha 1',
    },
    citationForm: {
      Ipa: 'Citation 1',
      Tha: 'Citation 1',
    },
    literalMeaning: {
      Ipa: '',
      Tha: 'Literal 1',
    },
    note: {
    },
    senses: [
      {
        id: '1',
        definition: {
          Sen: '',
          Tha: 'A handful of tools that are used to prepare lexicons',
        },
        gloss: {
          Sen: '',
          Tha: 'Lexorsium',
        },
        partOfSpeech: 'Verb',
        semanticDomain: ['Food'],
        exampleSentences: [
          {
            id: '1',
            reference: 'A book',
            sentence: {
              Ipa: '',
              Tha: 'My ipsum lorexum proved useful at the last lexicography workshop.',
            },
            translation: {
              Sen: '',
              Tha: 'Translation 1',
            },
          },
        ],
      },
      {
        id: '2',
        definition: {
          Sen: '',
          Tha: 'Another definition for the term',
        },
        gloss: {
          Sen: '',
          Tha: 'Gloss 1',
        },
        partOfSpeech: 'Noun',
        semanticDomain: ['Food'],
        exampleSentences: [
          {
            id: '1',
            reference: '1',
            sentence: {
              Ipa: '',
              Tha: 'Another example sentence for the lexeme sense',
            },
            translation: {
              Sen: '',
              Tha: 'Translation 1',
            },
          },
          {
            id: '2',
            reference: '2',
            sentence: {
              Ipa: '',
              Tha: 'Yet, another example sentence for the lexeme sense',
            },
            translation: {
              Sen: '',
              Tha: 'Translation 1',
            },
          },
        ],
      },
      {
        id: '3',
        definition: {
          Sen: '',
          Tha: 'Another definition for the term',
        },
        gloss: {
          Sen: '',
          Tha: 'Gloss 1',
        },
        partOfSpeech: 'Verb',
        semanticDomain: ['Food'],
        exampleSentences: [
          {
            id: '1',
            reference: '1',
            sentence: {
              Ipa: '',
              Tha: 'Another example sentence for the lexeme sense',
            },
            translation: {
              Sen: '',
              Tha: 'Translation 1',
            },
          },
          {
            id: '2',
            reference: '2',
            sentence: {
              Ipa: '',
              Tha: 'Yet, another example sentence for the lexeme sense',
            },
            translation: {
              Sen: '',
              Tha: 'Translation 1',
            },
          },
        ],
      },
    ]
  },
  {
    id: '1',
    lexemeForm: {
      Ipa: 'Ipa 1',
      Tha: 'Tha 1',
    },
    citationForm: {
      Ipa: 'Citation 1',
      Tha: 'Citation 1',
    },
    literalMeaning: {
      Ipa: '',
      Tha: 'Literal 1',
    },
    note: {
    },
    senses: [
      {
        id: '1',
        definition: {
          Sen: 'Definition 1',
          Tha: 'Definition 1',
        },
        gloss: {
          Sen: 'Gloss 1',
          Tha: 'Gloss 1',
        },
        partOfSpeech: 'Verb',
        semanticDomain: ['Food'],
        exampleSentences: [
          {
            id: '1',
            reference: '1',
            sentence: {
              Ipa: '',
              Tha: 'Example 1',
            },
            translation: {
              Sen: 'Translation 1',
              Tha: 'Translation 1',
            },
          },
        ],
      }, {
        id: '2',
        definition: {
          Sen: '',
          Tha: 'Definition 1',
        },
        gloss: {
          Sen: 'Gloss 1',
          Tha: 'Gloss 1',
        },
        partOfSpeech: 'Verb',
        semanticDomain: ['Food'],
        exampleSentences: [
          {
            id: '1',
            reference: '1',
            sentence: {
              Ipa: '',
              Tha: 'Example 1',
            },
            translation: {
              Sen: 'Translation 1',
              Tha: 'Translation 1',
            },
          },
        ],
      }
    ]
  },
  {
    id: '1',
    lexemeForm: {
      Ipa: 'Ipa 1',
      Tha: 'Tha 1',
    },
    citationForm: {
      Ipa: 'Citation 1',
      Tha: 'Citation 1',
    },
    literalMeaning: {
      Ipa: '',
      Tha: 'Literal 1',
    },
    note: {
    },
    senses: [
      {
        id: '1',
        definition: {
          Sen: 'Definition 1',
          Tha: 'Definition 1',
        },
        gloss: {
          Sen: 'Gloss 1',
          Tha: 'Gloss 1',
        },
        partOfSpeech: 'Verb',
        semanticDomain: ['Food'],
        exampleSentences: [
          {
            id: '1',
            reference: '1',
            sentence: {
              Ipa: '',
              Tha: 'Example 1',
            },
            translation: {
              Sen: 'Translation 1',
              Tha: 'Translation 1',
            },
          },
        ],
      }
    ]
  }
];
