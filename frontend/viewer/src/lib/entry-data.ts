import type { IEntry, WritingSystems } from "./mini-lcm";

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
    id: "1",
    lexemeForm: {
      values: {
        Ipa: "Ipa 1",
        Tha: "Tha 1",
      },
    },
    citationForm: {
      values: {
        Ipa: "Citation 1",
        Tha: "Citation 1",
      },
    },
    literalMeaning: {
      values: {
        Ipa: "",
        Tha: "Literal 1",
      },
    },
    note: {
      values: {

      },
    },
    senses: [
      {
        id: "1",
        definition: {
          values: {
            Sen: "",
            Tha: "Definition 1",
          },
        },
        gloss: {
          values: {
            Sen: "",
            Tha: "Gloss 1",
          },
        },
        partOfSpeech: "Verb",
        semanticDomain: ["Food"],
        exampleSentences: [
          {
            id: "1",
            reference: "A book",
            sentence: {
              values: {
                Ipa: "test",
                Tha: "Example 1asd",
              },
            },
            translation: {
              values: {
                Sen: "",
                Tha: "Translation 1",
              },
            },
          },
        ],
      },
      {
        id: "2",
        definition: {
          values: {
            Sen: "",
            Tha: "Definition 1",
          },
        },
        gloss: {
          values: {
            Sen: "",
            Tha: "Gloss 1",
          },
        },
        partOfSpeech: "Verb",
        semanticDomain: ["Food"],
        exampleSentences: [
          {
            id: "1",
            reference: "1",
            sentence: {
              values: {
                Ipa: "",
                Tha: "Example 1",
              },
            },
            translation: {
              values: {
                Sen: "",
                Tha: "Translation 1",
              },
            },
          },
        ],
      }
    ]
  },
  {
    id: "1",
    lexemeForm: {
      values: {
        Ipa: "Ipa 1",
        Tha: "Tha 1",
      },
    },
    citationForm: {
      values: {
        Ipa: "Citation 1",
        Tha: "Citation 1",
      },
    },
    literalMeaning: {
      values: {
        Ipa: "",
        Tha: "Literal 1",
      },
    },
    note: {
      values: {

      },
    },
    senses: [
      {
        id: "1",
        definition: {
          values: {
            Sen: "Definition 1",
            Tha: "Definition 1",
          },
        },
        gloss: {
          values: {
            Sen: "Gloss 1",
            Tha: "Gloss 1",
          },
        },
        partOfSpeech: "Verb",
        semanticDomain: ["Food"],
        exampleSentences: [
          {
            id: "1",
            reference: "1",
            sentence: {
              values: {
                Ipa: "",
                Tha: "Example 1",
              },
            },
            translation: {
              values: {
                Sen: "Translation 1",
                Tha: "Translation 1",
              },
            },
          },
        ],
      }, {
        id: "2",
        definition: {
          values: {
            Sen: "",
            Tha: "Definition 1",
          },
        },
        gloss: {
          values: {
            Sen: "Gloss 1",
            Tha: "Gloss 1",
          },
        },
        partOfSpeech: "Verb",
        semanticDomain: ["Food"],
        exampleSentences: [
          {
            id: "1",
            reference: "1",
            sentence: {
              values: {
                Ipa: "",
                Tha: "Example 1",
              },
            },
            translation: {
              values: {
                Sen: "Translation 1",
                Tha: "Translation 1",
              },
            },
          },
        ],
      }
    ]
  },
  {
    id: "1",
    lexemeForm: {
      values: {
        Ipa: "Ipa 1",
        Tha: "Tha 1",
      },
    },
    citationForm: {
      values: {
        Ipa: "Citation 1",
        Tha: "Citation 1",
      },
    },
    literalMeaning: {
      values: {
        Ipa: "",
        Tha: "Literal 1",
      },
    },
    note: {
      values: {

      },
    },
    senses: [
      {
        id: "1",
        definition: {
          values: {
            Sen: "Definition 1",
            Tha: "Definition 1",
          },
        },
        gloss: {
          values: {
            Sen: "Gloss 1",
            Tha: "Gloss 1",
          },
        },
        partOfSpeech: "Verb",
        semanticDomain: ["Food"],
        exampleSentences: [
          {
            id: "1",
            reference: "1",
            sentence: {
              values: {
                Ipa: "",
                Tha: "Example 1",
              },
            },
            translation: {
              values: {
                Sen: "Translation 1",
                Tha: "Translation 1",
              },
            },
          },
        ],
      }
    ]
  }
];
