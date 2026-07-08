/**
 * A broad function an example serves. An example can serve several (see {@link ExamplePlugin.functions});
 * these double as the gallery's grouping sections and its cross-cutting filter chips.
 */
export type ExampleFunction =
  | 'collect'
  | 'enrich'
  | 'record'
  | 'explore'
  | 'review'
  | 'publish'
  | 'play';

/**
 * A capability worth surfacing as a badge before the plugin's HTML is loaded.
 * `internet` and `entry-menu` mirror the HTML's `<meta>` declarations (kept honest by
 * index.test.ts); `microphone`/`camera` can't be derived from stored HTML, so they live only here.
 */
export type ExampleCapability = 'internet' | 'microphone' | 'camera' | 'entry-menu';

export interface ExampleFunctionInfo {
  key: ExampleFunction;
  /** English display name (section heading and filter-chip label). */
  label: string;
}

/** Fixed display order of the gallery's sections and filter chips. */
export const exampleFunctions: ExampleFunctionInfo[] = [
  {key: 'collect', label: 'Collect words'},
  {key: 'enrich', label: 'Enrich & clean up'},
  {key: 'record', label: 'Record & illustrate'},
  {key: 'explore', label: 'Explore & analyze'},
  {key: 'review', label: 'Review & history'},
  {key: 'publish', label: 'Publish'},
  {key: 'play', label: 'Play & learn'},
];

export interface ExamplePlugin {
  /** Stable identifier, e.g. 'dictionary-stats'. */
  key: string;
  /** English display name (also used as the default plugin name). */
  name: string;
  /** One-sentence description (also the default plugin description). */
  description: string;
  /** Functions this example serves; `functions[0]` is the primary one that places it in a section. */
  functions: [ExampleFunction, ...ExampleFunction[]];
  /** Capabilities to surface as badges in the gallery. */
  capabilities?: ExampleCapability[];
  /** Loads the plugin HTML on demand so the raw files stay out of the main bundle. */
  loadHtml: () => Promise<string>;
}

export const examplePlugins: ExamplePlugin[] = [
  // ---- Collect words ----
  {
    key: 'rapid-words',
    name: 'Rapid Word Collection',
    description: 'Work through the semantic-domain framework, rapidly adding new words and glosses to each domain.',
    functions: ['collect'],
    loadHtml: async () => (await import('./rapid-words.html?raw')).default,
  },
  {
    key: 'wordlist',
    name: 'Wordlist',
    description: 'Elicit words for a comparative wordlist (Swadesh or your own), creating an entry for each meaning.',
    functions: ['collect'],
    loadHtml: async () => (await import('./wordlist.html?raw')).default,
  },

  // ---- Enrich & clean up ----
  {
    key: 'gap-finder',
    name: 'Gap Finder',
    description: 'Find entries missing glosses, parts of speech, examples or domains, and fill them in from a worklist.',
    functions: ['enrich', 'explore'],
    loadHtml: async () => (await import('./gap-finder.html?raw')).default,
  },
  {
    key: 'sentence-sprint',
    name: 'Sentence Sprint',
    description: 'Add example sentences to senses that lack them, one at a time, with streaks that make progress satisfying.',
    functions: ['enrich', 'play'],
    loadHtml: async () => (await import('./sentence-sprint.html?raw')).default,
  },
  {
    key: 'orthography-machine',
    name: 'Orthography Change Machine',
    description: 'Define spelling-reform rules, preview every affected entry, then apply the changes one approval at a time.',
    functions: ['enrich'],
    loadHtml: async () => (await import('./orthography-machine.html?raw')).default,
  },
  {
    key: 'bulk-editor',
    name: 'Bulk Editor',
    description: 'Assign a part of speech or semantic domain to many senses at once, previewing and approving each change.',
    functions: ['enrich'],
    loadHtml: async () => (await import('./bulk-editor.html?raw')).default,
  },
  {
    key: 'character-inspector',
    name: 'Character Inspector',
    description: 'Audit the characters used in a writing system, flag out-of-alphabet or stray characters, and tidy whitespace.',
    functions: ['enrich', 'explore'],
    loadHtml: async () => (await import('./character-inspector.html?raw')).default,
  },

  // ---- Record & illustrate ----
  {
    key: 'pronunciation-recorder',
    name: 'Pronunciation Recorder',
    description: 'Walk entries that lack a recording and capture pronunciation audio straight onto each one.',
    functions: ['record'],
    capabilities: ['microphone', 'entry-menu'],
    loadHtml: async () => (await import('./pronunciation-recorder.html?raw')).default,
  },
  {
    key: 'photo-capture',
    name: 'Photo Capture',
    description: 'Photograph entries with your camera (or pick a file) and attach the picture to a sense.',
    functions: ['record'],
    capabilities: ['camera', 'entry-menu'],
    loadHtml: async () => (await import('./photo-capture.html?raw')).default,
  },
  {
    key: 'audio-dictionary',
    name: 'Audio Dictionary',
    description: 'Play back pronunciation recordings, track coverage, and drill listening — see which entries still need audio.',
    functions: ['record', 'play', 'explore'],
    loadHtml: async () => (await import('./audio-dictionary.html?raw')).default,
  },
  {
    key: 'picture-dictionary',
    name: 'Picture Dictionary',
    description: 'Browse every illustrated entry as a filterable image gallery with a lightbox and audio playback.',
    functions: ['record', 'explore'],
    loadHtml: async () => (await import('./picture-dictionary.html?raw')).default,
  },

  // ---- Explore & analyze ----
  {
    key: 'browse-grid',
    name: 'Browse Grid',
    description: 'See the whole lexicon as a sortable, filterable spreadsheet with configurable columns and CSV export.',
    functions: ['explore'],
    loadHtml: async () => (await import('./browse-grid.html?raw')).default,
  },
  {
    key: 'concordance',
    name: 'Concordance',
    description: 'Search all text fields and read the results keyword-in-context, aligned on your search term.',
    functions: ['explore'],
    loadHtml: async () => (await import('./concordance.html?raw')).default,
  },
  {
    key: 'duplicate-finder',
    name: 'Duplicate Finder',
    description: 'Flag possible duplicate entries — identical, near-identical, or sharing a gloss — for review.',
    functions: ['explore'],
    loadHtml: async () => (await import('./duplicate-finder.html?raw')).default,
  },
  {
    key: 'minimal-pairs',
    name: 'Minimal Pairs',
    description: 'Find words that differ by a single sound, grouped by contrast, as evidence for phonemic analysis.',
    functions: ['explore'],
    loadHtml: async () => (await import('./minimal-pairs.html?raw')).default,
  },
  {
    key: 'domain-coverage',
    name: 'Domain Coverage',
    description: "See how much of the semantic-domain framework your project covers, and drill into what's still empty.",
    functions: ['explore', 'collect'],
    loadHtml: async () => (await import('./domain-coverage.html?raw')).default,
  },
  {
    key: 'dictionary-stats',
    name: 'Dictionary Stats',
    description: 'A dashboard of entry, sense, part-of-speech and semantic-domain statistics for the project.',
    functions: ['explore'],
    loadHtml: async () => (await import('./dictionary-stats.html?raw')).default,
  },
  {
    key: 'lexicon-galaxy',
    name: 'Lexicon Galaxy',
    description: 'Explore your whole dictionary as an interactive night sky where semantic domains form constellations.',
    functions: ['explore', 'play'],
    loadHtml: async () => (await import('./lexicon-galaxy.html?raw')).default,
  },

  // ---- Review & history ----
  {
    key: 'change-review',
    name: 'Change Review',
    description: "Work through the project's recent change history one commit at a time, checking each off as reviewed.",
    functions: ['review'],
    loadHtml: async () => (await import('./change-review.html?raw')).default,
  },
  {
    key: 'entry-time-machine',
    name: 'Entry Time Machine',
    description: "See one entry's full edit history on a timeline and compare any past version with today's.",
    functions: ['review'],
    capabilities: ['entry-menu'],
    loadHtml: async () => (await import('./entry-time-machine.html?raw')).default,
  },
  {
    key: 'comments-inbox',
    name: 'Comments Inbox',
    description: "Triage your team's comment threads across the dictionary, with unread tracking and per-entry context.",
    functions: ['review'],
    capabilities: ['entry-menu'],
    loadHtml: async () => (await import('./comments-inbox.html?raw')).default,
  },

  // ---- Publish ----
  {
    key: 'dictionary-preview',
    name: 'Dictionary Preview',
    description: 'Typeset your dictionary as printable, book-style pages, ready to print or save as PDF.',
    functions: ['publish'],
    loadHtml: async () => (await import('./dictionary-preview.html?raw')).default,
  },
  {
    key: 'reversal-index',
    name: 'Reversal Index',
    description: 'Build a reversal index — meanings in an analysis language pointing back to headwords — to browse or print.',
    functions: ['publish', 'explore'],
    loadHtml: async () => (await import('./reversal-index.html?raw')).default,
  },

  // ---- Play & learn ----
  {
    key: 'crossword',
    name: 'Crossword',
    description: 'Generate a playable crossword from your words, clued by their meanings.',
    functions: ['play'],
    capabilities: ['entry-menu'],
    loadHtml: async () => (await import('./crossword.html?raw')).default,
  },
  {
    key: 'flashcards',
    name: 'Flashcards',
    description: 'Study vocabulary with flip cards and spaced-repetition-style progress saved per entry.',
    functions: ['play'],
    loadHtml: async () => (await import('./flashcards.html?raw')).default,
  },
  {
    key: 'listening-quiz',
    name: 'Listening Quiz',
    description: 'Play a recording and pick the right meaning (or word) — a multiple-choice listening game.',
    functions: ['play'],
    loadHtml: async () => (await import('./listening-quiz.html?raw')).default,
  },
  {
    key: 'word-harvest-bingo',
    name: 'Word Harvest Bingo',
    description: 'A projector-friendly bingo board that stamps squares as domains fill up during a word-collection event.',
    functions: ['play', 'collect'],
    loadHtml: async () => (await import('./word-harvest-bingo.html?raw')).default,
  },
];

/** English label for a function key. */
export function exampleFunctionLabel(key: ExampleFunction): string {
  return exampleFunctions.find((f) => f.key === key)?.label ?? key;
}

/**
 * Examples grouped by their PRIMARY function, in {@link exampleFunctions} order, skipping empty
 * sections. This is the gallery's default browse view (each example appears once).
 */
export function examplePluginsByPrimaryFunction(): {function: ExampleFunctionInfo; plugins: ExamplePlugin[]}[] {
  return exampleFunctions
    .map((info) => ({
      function: info,
      plugins: examplePlugins.filter((plugin) => plugin.functions[0] === info.key),
    }))
    .filter((group) => group.plugins.length > 0);
}
