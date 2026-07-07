export interface ExamplePlugin {
  /** Stable identifier, e.g. 'dictionary-stats'. */
  key: string;
  /** English display name (also used as the default plugin name). */
  name: string;
  /** One-sentence description. */
  description: string;
  /** Loads the plugin HTML on demand so the raw files stay out of the main bundle. */
  loadHtml: () => Promise<string>;
}

export const examplePlugins: ExamplePlugin[] = [
  {
    key: 'dictionary-stats',
    name: 'Dictionary Stats',
    description: 'A read-only dashboard of entry, sense, part-of-speech and semantic domain statistics for the current project.',
    loadHtml: async () => (await import('./dictionary-stats.html?raw')).default,
  },
  {
    key: 'flashcards',
    name: 'Flashcards',
    description: 'Study vocabulary with flippable flashcards and spaced-repetition-style progress saved per entry.',
    loadHtml: async () => (await import('./flashcards.html?raw')).default,
  },
  {
    key: 'word-collector',
    name: 'Word Collector',
    description: 'Quickly add new words and glosses to a chosen semantic domain while brainstorming vocabulary.',
    loadHtml: async () => (await import('./word-collector.html?raw')).default,
  },
  {
    key: 'dictionary-preview',
    name: 'Dictionary Preview',
    description: 'See your dictionary as a typeset, printable book page — choose languages, filter, and print to PDF.',
    loadHtml: async () => (await import('./dictionary-preview.html?raw')).default,
  },
  {
    key: 'lexicon-galaxy',
    name: 'Lexicon Galaxy',
    description: 'Explore your whole dictionary as an interactive night sky where semantic domains form constellations.',
    loadHtml: async () => (await import('./lexicon-galaxy.html?raw')).default,
  },
  {
    key: 'orthography-machine',
    name: 'Orthography Change Machine',
    description: 'Define spelling-reform rules, preview every affected entry, then apply the changes one approval at a time.',
    loadHtml: async () => (await import('./orthography-machine.html?raw')).default,
  },
  {
    key: 'word-harvest-bingo',
    name: 'Word Harvest Bingo',
    description: 'A projector-friendly bingo card for word-collection workshops — squares stamp themselves as domains hit their targets.',
    loadHtml: async () => (await import('./word-harvest-bingo.html?raw')).default,
  },
  {
    key: 'sentence-sprint',
    name: 'Sentence Sprint',
    description: 'Fill example-sentence gaps one sense at a time, with streaks that make daily progress satisfying.',
    loadHtml: async () => (await import('./sentence-sprint.html?raw')).default,
  },
];
