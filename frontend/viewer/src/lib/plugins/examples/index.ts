import dictionaryStats from './dictionary-stats.html?raw';
import flashcards from './flashcards.html?raw';
import wordCollector from './word-collector.html?raw';

export interface ExamplePlugin {
  /** Stable identifier, e.g. 'dictionary-stats'. */
  key: string;
  /** English display name (also used as the default plugin name). */
  name: string;
  /** One-sentence description. */
  description: string;
  html: string;
}

export const examplePlugins: ExamplePlugin[] = [
  {
    key: 'dictionary-stats',
    name: 'Dictionary Stats',
    description: 'A read-only dashboard of entry, sense, part-of-speech and semantic domain statistics for the current project.',
    html: dictionaryStats,
  },
  {
    key: 'flashcards',
    name: 'Flashcards',
    description: 'Study vocabulary with flippable flashcards and spaced-repetition-style progress saved per entry.',
    html: flashcards,
  },
  {
    key: 'word-collector',
    name: 'Word Collector',
    description: 'Quickly add new words and glosses to a chosen semantic domain while brainstorming vocabulary.',
    html: wordCollector,
  },
];
