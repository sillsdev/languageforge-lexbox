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

/**
 * Fixed display order of the gallery's sections and filter chips. Display names live in
 * PluginsView (this module is imported by Playwright's node-side test loader, which can't
 * resolve the i18n runtime).
 */
export const exampleFunctions: ExampleFunction[] = ['collect', 'enrich', 'record', 'explore', 'review', 'publish', 'play'];

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
  /** False keeps the example out of the gallery (it still ships and still runs in tests). */
  listed?: boolean;
  /** Loads the plugin HTML on demand so the raw files stay out of the main bundle. */
  loadHtml: () => Promise<string>;
}

export const examplePlugins: ExamplePlugin[] = [
  // ---- Collect words ----
  {
    key: 'rapid-words',
    name: 'Rapid Word Collection',
    description: 'Work through the semantic domains one at a time, quickly adding words and glosses to each, and see how many domains have words.',
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
    key: 'sentence-sprint',
    name: 'Sentence Sprint',
    description: 'Add example sentences to senses that lack them, one at a time, with streaks that make progress satisfying.',
    functions: ['enrich', 'play'],
    loadHtml: async () => (await import('./sentence-sprint.html?raw')).default,
  },
  {
    key: 'orthography-machine',
    name: 'Orthography Change Machine',
    description: 'Define spelling-reform rules, preview every affected entry, then approve the changes in batches.',
    functions: ['enrich'],
    loadHtml: async () => (await import('./orthography-machine.html?raw')).default,
  },
  {
    key: 'bulk-editor',
    name: 'Bulk Editor',
    description: 'Assign a part of speech or semantic domain to many senses at once, previewing every change and approving them in batches.',
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

  // The sandbox can't use the microphone or camera (#2920); unlisted until it can.
  {
    key: 'pronunciation-recorder',
    name: 'Pronunciation Recorder',
    description: 'Record headwords, example sentences or glosses one after another: hold to record, it moves on by itself, and takes are saved in batches.',
    functions: ['record'],
    capabilities: ['microphone', 'entry-menu'],
    listed: false,
    loadHtml: async () => (await import('./pronunciation-recorder.html?raw')).default,
  },
  {
    key: 'photo-capture',
    name: 'Photo Capture',
    description: 'Photograph entries with your camera (or pick a file) and attach the picture to a sense.',
    functions: ['record'],
    capabilities: ['camera', 'entry-menu'],
    listed: false,
    loadHtml: async () => (await import('./photo-capture.html?raw')).default,
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
    description: 'Flag possible duplicate entries for review: same headword, nearly the same spelling, or the same gloss.',
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

  // ---- Review ----
  {
    key: 'change-review',
    name: 'Change Review',
    description: 'What the team changed recently, one card per entry with each field before and after. Pick up where you left off with "since my last review".',
    functions: ['review'],
    loadHtml: async () => (await import('./change-review.html?raw')).default,
  },
  {
    key: 'comments-feed',
    name: 'Comments Feed',
    description: "Every comment thread in the project, newest first, resolved ones included. From an entry's menu, just that entry's threads.",
    functions: ['review'],
    capabilities: ['entry-menu'],
    loadHtml: async () => (await import('./comments-feed.html?raw')).default,
  },

  // ---- Publish ----
  {
    key: 'dictionary-preview',
    name: 'Dictionary Preview',
    description: 'See your dictionary as book pages, one letter at a time, and print it or save it as a PDF.',
    functions: ['publish'],
    loadHtml: async () => (await import('./dictionary-preview.html?raw')).default,
  },
  {
    key: 'reversal-index',
    name: 'Reversal Index',
    description: 'Look words up by meaning: an index in an analysis language that points back to your headwords, one letter at a time or printed whole.',
    functions: ['publish', 'explore'],
    loadHtml: async () => (await import('./reversal-index.html?raw')).default,
  },

  // ---- Play & learn ----
  {
    key: 'speaking-isles',
    name: 'The Speaking Isles',
    description: 'A 3D island game grown from your dictionary: learn its words to lift the Hush, then add new words and example sentences along the way. Needs a device with WebGL.',
    functions: ['play', 'collect', 'explore'],
    loadHtml: async () => (await import('./speaking-isles.html?raw')).default,
  },
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
    description: 'Learn the words in your dictionary with flip cards; words you know come back less often.',
    functions: ['play'],
    loadHtml: async () => (await import('./flashcards.html?raw')).default,
  },
];

/** The examples the gallery offers. */
export const listedExamplePlugins = examplePlugins.filter((plugin) => plugin.listed !== false);

/**
 * Examples grouped by their PRIMARY function, in {@link exampleFunctions} order, skipping empty
 * sections. This is the gallery's default browse view (each example appears once).
 */

export function examplePluginsByPrimaryFunction(): {function: ExampleFunction; plugins: ExamplePlugin[]}[] {
  return exampleFunctions
    .map((key) => ({
      function: key,
      plugins: listedExamplePlugins.filter((plugin) => plugin.functions[0] === key),
    }))
    .filter((group) => group.plugins.length > 0);
}
