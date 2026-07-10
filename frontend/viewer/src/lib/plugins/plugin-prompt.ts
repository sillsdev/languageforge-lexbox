import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';

/**
 * User-toggleable steering for the generated prompt. These shape the *instructions given to the AI*;
 * they do not restrict what a finished plugin can technically do (the app enforces that separately).
 */
export interface PluginPromptOptions {
  /** Plugin is only for this project, so it may reference its ids. Default true. */
  projectSpecific?: boolean;
  /** Should work well on small / mobile screens. Default true. */
  mobile?: boolean;
  /** Add broad steering to treat sacred/cultural/traditional content respectfully. Default false. */
  culturalSensitivity?: boolean;
  /** The plugin may modify the dictionary (every write is still user-confirmed). Default true. */
  allowEdits?: boolean;
  /** The plugin may use the internet. Default false (offline-first). */
  internet?: boolean;
}

/** Placeholder used when the user copies the prompt without describing their plugin first. */
const DESCRIPTION_PLACEHOLDER =
  'REPLACE THIS PARAGRAPH with a description of the plugin you want — what it shows or does, roughly how it should look, and anything it should save between sessions.';

/**
 * The closing section that tells the AI what to build. Kept separate from {@link buildPluginPrompt}
 * so the user's description can be spliced in with a cheap string op on every keystroke, without
 * re-running the project-data queries that build the body.
 */
export function pluginTaskSection(description: string): string {
  return `---

## What I want the plugin to do

${description.trim() || DESCRIPTION_PLACEHOLDER}`;
}

/**
 * Builds the project-aware body of the prompt users paste into an AI assistant (Claude, ChatGPT, …)
 * to have it write a plugin. Append {@link pluginTaskSection} to get the complete prompt. Kept in
 * English on purpose: it addresses the AI, not the user, and English prompts work best across models.
 * The embedded API documentation is the authoritative public reference for plugin API v1 — keep it in
 * sync with plugin-sdk.js and plugin-api-adapter.ts.
 */
export async function buildPluginPrompt(
  api: IMiniLcmJsInvokable,
  project: {projectName: string; projectCode: string},
  options: PluginPromptOptions = {},
): Promise<string> {
  const projectSpecific = options.projectSpecific ?? true;
  const mobile = options.mobile ?? true;
  const culturalSensitivity = options.culturalSensitivity ?? false;
  const readOnly = !(options.allowEdits ?? true);
  const internet = options.internet ?? false;
  const [writingSystems, partsOfSpeech, semanticDomains, entryCount] = await Promise.all([
    api.getWritingSystems(),
    api.getPartsOfSpeech(),
    api.getSemanticDomains(),
    api.countEntries(undefined, {}),
  ]);

  function describeWs(ws: {wsId: string; name: string; isAudio: boolean}, index: number): string {
    return `  - \`${ws.wsId}\` — ${ws.name}${index === 0 ? ' (default)' : ''}${ws.isAudio ? ' (audio-only, skip for text)' : ''}`;
  }
  const vernacular = writingSystems.vernacular.map(describeWs).join('\n') || '  - (none)';
  const analysis = writingSystems.analysis.map(describeWs).join('\n') || '  - (none)';

  function firstName(name: Record<string, string>): string {
    return Object.values(name).find(value => value) ?? '(unnamed)';
  }
  const posList = partsOfSpeech.slice(0, 40)
    .map(pos => `  - ${firstName(pos.name)} (\`${pos.id}\`)`)
    .join('\n') || '  - (none defined yet)';
  const domainSample = semanticDomains.slice(0, 15)
    .map(domain => `\`${domain.code}\` ${firstName(domain.name)}`)
    .join(', ');

  const networkRequirement = internet
    ? `4. **Internet is allowed.** This plugin may fetch external resources/APIs. Add \`<meta name="fwlite-plugin-permissions" content="internet">\` in \`<head>\` so the app lifts the network block (it shows the user an "internet" badge). Still don't send dictionary data anywhere the task doesn't require.`
    : `4. **No network access.** A Content-Security-Policy blocks all fetches and external resources — keep everything inline and offline. (If a task genuinely needed the internet it would be enabled explicitly; assume it is not.)`;

  const writesRequirement = readOnly
    ? `6. **Read-only plugin.** Do not modify the dictionary: never call \`createEntry\`, \`updateEntry\`, \`applyChanges\`, or \`saveFile\`. Read, display, and analyze only.`
    : `6. **Writes are user-approved**: every \`createEntry\`/\`updateEntry\`/\`applyChanges\` call pops up a confirmation dialog in the app showing exactly what will change. If the user declines, the promise rejects with \`error.code === 'permission-denied'\` — handle that as a normal outcome (show "not saved"), never as a crash.`;

  const writingSection = readOnly ? '' : `### Writing (each call = one user confirmation dialog)

\`\`\`ts
fwlite.createEntry(entry): Promise<Entry>
// Provide only the fields you have; ids and empty collections are filled in for you:
// {lexemeForm: {ws: 'word'}, senses: [{gloss: {ws: 'meaning'}, semanticDomains: [domainObj], partOfSpeechId?: id}]}
// semanticDomains items must be objects obtained from getSemanticDomains(). partOfSpeechId is a POS id (read back as sense.partOfSpeech).

fwlite.updateEntry(before, after): Promise<Entry>
// 'before' = the entry exactly as you fetched it; 'after' = a modified deep copy.
// Only the differences are applied, so concurrent edits to other fields survive.

fwlite.applyChanges(operations): Promise<Entry[]>
// Several changes behind ONE confirmation dialog (instead of one dialog per write). Use this when
// you have a batch — e.g. a contribution game submitting multiple entries at once.
// operations: Array<{type:'createEntry', entry} | {type:'updateEntry', before, after}>
// Resolves to the resulting entries, in order.
\`\`\`

`;

  const recordingSection = readOnly ? '' : `### Recording audio / capturing pictures

You may record audio (or capture from the camera) and save it — e.g. to fill in missing pronunciation. Use the normal browser APIs (\`navigator.mediaDevices.getUserMedia\`, \`MediaRecorder\`); **the browser prompts the user** for microphone/camera access, so you don't declare any permission. Then persist the bytes with \`fwlite.saveFile\` and attach the returned \`mediaUri\` to an entry:

\`\`\`ts
fwlite.saveFile(bytes, {filename, mimeType}): Promise<{result: string, mediaUri?: string, errorMessage?: string}>
// bytes: ArrayBuffer | Uint8Array | Blob. Then write mediaUri into an audio writing-system field (or a
// sense picture) via updateEntry — that edit is user-approved as usual.
\`\`\`

`;

  const projectSection = projectSpecific
    ? `## This project

This plugin will run inside **${project.projectName}** (code \`${project.projectCode}\`). You may reference its writing systems, part-of-speech ids, and semantic domain codes below — but **don't hard-code them unless it genuinely simplifies the code or adds real value**. Prefer reading them at runtime (\`getWritingSystems\`, \`getPartsOfSpeech\`, \`getSemanticDomains\`); equally, don't force awkward abstraction where a direct reference is clearly simpler. Use good judgement.

- Entries: ~${entryCount}
- Vernacular writing systems (the language being documented):
${vernacular}
- Analysis writing systems (meanings/translations):
${analysis}
- Parts of speech:
${posList}
- Semantic domains: ${semanticDomains.length} defined${domainSample ? ` — e.g. ${domainSample}` : ''}`
    : `## Make it portable

This plugin should work in **any** project, so **don't hard-code** writing-system codes, semantic-domain codes, or part-of-speech ids. Discover them at runtime (\`getWritingSystems\`, \`getPartsOfSpeech\`, \`getSemanticDomains\`) and handle the case where the ones you expect don't exist. (For reference only, the current project has ~${entryCount} entries and ${writingSystems.vernacular.length} vernacular writing system(s) — don't rely on those specifics.)`;

  const responsiveGuidance = mobile
    ? `- **Responsive**: must work from a 360px phone to a desktop; no horizontal page scrolling. Test your layout mentally at both sizes.`
    : `- **Layout**: target desktop/tablet. You don't need to fully support small phones — support them where it's cheap, but it's fine to require a larger screen if mobile would add significant complexity. Still avoid gratuitous horizontal scrolling.`;

  const culturalSection = culturalSensitivity ? `## Cultural sensitivity

Some vernacular content may be sacred, sensitive, or culturally significant to the language community. Treat all language data with respect: don't trivialize, gamify, or expose it carelessly, and present it plainly when in doubt. When something feels sensitive, err toward restraint.

` : '';

  return `# Write a plugin for FieldWorks Lite (FW Lite)

You are writing a **plugin** for FW Lite, a dictionary-editing app used by language communities and linguists. A plugin is **one self-contained HTML file** that FW Lite runs in a sandboxed iframe and connects to the open dictionary project through a small JavaScript API.

## Hard requirements

1. **Output exactly one complete HTML file** (\`<!DOCTYPE html>\` through \`</html>\`). The user will copy-paste it as-is.
2. **Fully self-contained**: all CSS and JavaScript inline. No external scripts, stylesheets, fonts, or images (embed small images as data: URIs if needed). No build step, no modules, no frameworks.
3. **Sandboxed environment**: the plugin runs in an opaque-origin iframe. \`localStorage\`, \`sessionStorage\`, and cookies are **unavailable** — persist data with \`fwlite.storage\` instead. \`alert\`/\`confirm\` work; popups don't.
${networkRequirement}
5. A global **\`fwlite\`** API object is injected before your code runs. Data becomes available once \`fwlite.ready\` resolves — structure your code as \`fwlite.ready.then(init)\`.
${writesRequirement}

## What FW Lite already provides — don't rebuild these

Your plugin runs inside app chrome that already gives you the following. Build on these instead of duplicating them — it keeps plugins small and consistent, and avoids bugs (a hand-rolled fullscreen button, for instance, usually just breaks):

- **Fullscreen** — a fullscreen button in the plugin toolbar; once fullscreen the app shows its own exit control (and Esc exits). Do **not** add your own fullscreen button.
- **Reload** — a reload button in the toolbar. Do **not** add your own.
- **Back** — a button to leave the plugin and return to the list.
- **Theme** — light/dark via \`fwlite.theme\`, which follows the app. Drive your colors from it; don't build a theme switcher.
- **Toasts** — \`fwlite.notify(message)\` shows an app notification. Use it instead of building your own.
- **Persistent storage** — \`fwlite.storage\` for saving data between sessions.
- **Viewing & editing entries** — \`fwlite.openEntry(id, {mode})\` opens the app's own view/edit dialog over your plugin. Don't rebuild an entry editor.
- **Change approval** — every write shows the user exactly what will change, so you never build confirmation UI.
- **Downloading media** — \`fwlite.getMedia\` fetches audio/picture files for you (downloading on demand).

## The \`fwlite\` API (v1)

All methods return Promises.

### Reading

\`\`\`ts
fwlite.ready: Promise<{apiVersion: 1, project: {projectName, projectCode}, theme: 'light'|'dark', permissions: string[], capabilities: {openEntryModes: string[]}}>
fwlite.project      // {projectName, projectCode} — available after ready
fwlite.theme        // 'light' | 'dark' — the app's current theme; also respect prefers-color-scheme
fwlite.capabilities // {openEntryModes} — what this app build supports; feature-detect before using optional modes
fwlite.context      // {entryId?: string} — launch context; entryId is set only when opened from an entry

fwlite.getWritingSystems(): Promise<{vernacular: WritingSystem[], analysis: WritingSystem[]}>
// WritingSystem: {wsId: string, name: string, abbreviation: string, font: string, isAudio: boolean, exemplars: string[]}
// The first item in each array is the default (the primary orthography). See "Writing systems & headwords" below:
// isAudio writing systems hold audio-file references, NOT text — never display their values; skip them for text.

fwlite.getEntries(query?): Promise<Entry[]>
// query: {
//   search?: string,                 // full-text search
//   limit?: number,                  // default 100, max 1000
//   offset?: number,                 // for paging
//   filter?: {
//     semanticDomainCode?: string,   // entries with a sense in this semantic domain
//     partOfSpeechId?: string,       // entries with a sense of this part of speech
//     missingGlossWs?: string,       // entries with no sense glossed in this writing system
//     missingExampleWs?: string,     // entries where no sense has an example sentence in this writing system
//     missingPartOfSpeech?: boolean, // entries having a sense with no part of speech
//   },                               // all filter conditions compose with AND
//   sort?: {writingSystem?: string, ascending?: boolean},  // sorts by headword
// }
// Filtering runs on the real backend; against the in-browser demo project filters may return
// unfiltered results, so also verify entries client-side if correctness matters.
fwlite.countEntries(query?): Promise<number>   // query: {search?, filter?}
fwlite.getEntry(id): Promise<Entry | null>
fwlite.getPartsOfSpeech(): Promise<{id: string, name: MultiString}[]>
fwlite.getSemanticDomains(): Promise<{id: string, name: MultiString, code: string}[]>

fwlite.getMedia(mediaUri): Promise<{data: ArrayBuffer, fileName?: string, mimeType?: string} | null>
// Fetches an audio recording or picture. Pass an audio writing-system value (see below) or a
// sense picture's \`mediaUri\`. The app downloads the file automatically on first use, so this can
// take a moment and returns null when offline or not found — always handle null.
\`\`\`

### Audio & pictures

Recordings and images aren't inlined in the entry data — you get a **reference** and fetch the bytes with \`fwlite.getMedia(ref)\`:
- **Audio**: an audio writing system (\`isAudio: true\`) stores a media reference as its value in a multistring (e.g. \`entry.lexemeForm['seh-Zxxx-x-audio']\`, or a sense/example audio field). Pass that value to \`getMedia\`.
- **Pictures**: each sense has \`sense.pictures: {id, mediaUri, caption}[]\`. Pass \`picture.mediaUri\` to \`getMedia\`.

\`getMedia\` may download on first use and can fail offline (returns \`null\`) — show a loading state and degrade gracefully. Build something usable inside your iframe from the bytes:

\`\`\`js
const media = await fwlite.getMedia(ref);
if (media) {
  const url = URL.createObjectURL(new Blob([media.data], {type: media.mimeType || ''}));
  audioEl.src = url;   // or imgEl.src = url;  (revoke with URL.revokeObjectURL when done)
}
\`\`\`

${recordingSection}### Comments (read-only)

\`\`\`ts
fwlite.getCommentThreads({subjectType, subjectId, includeComments?}): Promise<CommentThread[]>
fwlite.getCommentThread(threadId): Promise<CommentThread | null>
fwlite.getUserComments(threadId): Promise<Comment[]>
fwlite.getUnreadComments({threadId?}): Promise<Comment[]>          // omit threadId for all unread across the project
fwlite.getUnreadCommentsForSubject({subjectType, subjectId}): Promise<Comment[]>
fwlite.countUnreadComments({threadId?}): Promise<number>
// subjectType: 'Entry' | 'Sense' | 'ExampleSentence'. A thread groups comments about one subject.
// CommentThread: {id, subjectType, subjectId, status: 'Open'|'Closed', authorName?, createdAt, updatedAt, comments?}
// Comment: {id, commentThreadId, text, authorName?, createdAt, updatedAt}
// Read/unread state is per-device (local), not shared across the team.
\`\`\`

### Activity & history (read-only)

\`\`\`ts
fwlite.getActivity({skip?, take?, authorFilterKeys?, changeTypeKeys?, sort?}): Promise<Activity[]>  // project-wide change feed
fwlite.getEntityHistory(entityId): Promise<HistoryItem[]>          // one entry's timeline
fwlite.getChangeContext({commitId, changeIndex}): Promise<ChangeContext>
fwlite.getObjectAtCommit({commitId, entityId}): Promise<object>    // entity state at a point in time
fwlite.listActivityAuthors(): Promise<{authorId?, authorName?, commitCount}[]>
fwlite.listActivityChangeTypes(): Promise<{key, label, commitCount}[]>
// Activity: {commitId, timestamp, changeName, changeTypes, metadata: {authorName?, authorId?}}
// Not available for every project type — these reject with code 'not-supported' when there's no history.
\`\`\`

There is no built-in "reviewed" flag on changes. If you're building a review tool, track review state yourself in \`fwlite.storage\` keyed by \`commitId\` — the app doesn't persist it for you (this is fine for a single reviewer on one device).

${writingSection}### Utilities

\`\`\`ts
fwlite.openEntry(entryId, options?): Promise<void>  // see "Opening entries" below; default opens a read-only dialog
fwlite.notify(message): Promise<void>          // shows a toast in the app
fwlite.storage.get(key): Promise<any>          // per-plugin persistent storage (JSON values, ~256KB total)
fwlite.storage.set(key, value): Promise<void>
fwlite.storage.remove(key): Promise<void>
fwlite.headword(entry): string                 // the entry's headword the way the app shows it — prefer this (see below)
fwlite.asText(richStringOrString): string      // flattens rich text ({spans:[{text}]}) to plain text
fwlite.firstValue(multiString, ['seh','en']): string  // first non-empty value, preferring given writing systems
\`\`\`

### Opening entries

\`\`\`ts
fwlite.openEntry(entryId, {mode})  // mode: 'view' (default) | 'edit' | 'window' | 'navigate'
\`\`\`

- \`'view'\` — opens the entry in a **read-only dialog** over your plugin. Your plugin stays running underneath, so no state is lost. This is the default: seeing an entry shouldn't risk changing it (the dialog has an "Edit" button if the user does want to change it).
- \`'edit'\` — same dialog, opened ready to edit.
- \`'window'\` — opens the entry in a separate window. Not available on every platform (e.g. Android); where unsupported the app falls back to \`'view'\`. Check \`fwlite.capabilities.openEntryModes.includes('window')\` if you want to hide the option.
- \`'navigate'\` — leaves your plugin and goes to the entry in the app. **This unmounts your plugin and loses its state** — only use it to hand the user off for good.

Prefer \`'view'\`/\`'edit'\` in a running game or tool so the player keeps their place.

### Launch context

By default a plugin is launched on its own from the plugins list. If your plugin is meant to act on a
**single entry**, declare it so the app offers it in an entry's context menu:

\`\`\`html
<meta name="fwlite-plugin-contexts" content="entry">
\`\`\`

Only declare \`entry\` if your plugin genuinely does something with one entry — otherwise leave it out so
you don't clutter every entry's menu. When the user launches your plugin from an entry, \`fwlite.context.entryId\`
is set to that entry's id — start by loading it with \`fwlite.getEntry(fwlite.context.entryId)\`. Otherwise
\`context\` is \`{}\` (no \`entryId\`), so treat the entry-focused flow as optional and fall back to a normal view.

### Data model

\`\`\`ts
type MultiString = {[writingSystemId: string]: string};           // e.g. {en: 'dog', fr: 'chien'}
type RichMultiString = {[writingSystemId: string]: RichString};   // rich text; flatten with fwlite.asText
type RichString = {spans: {text: string, ws: string}[]};

type Entry = {
  id: string,
  headword: string,              // computed, read-only: how the app displays this entry (see below). Use fwlite.headword(entry).
  lexemeForm: MultiString,       // the word as first entered (vernacular)
  citationForm: MultiString,     // dictionary form; overrides lexemeForm for display when present
  note: RichMultiString,
  senses: Sense[],
};
type Sense = {
  id: string,
  gloss: MultiString,            // short meaning (analysis language)
  definition: RichMultiString,   // longer meaning
  partOfSpeech?: {id: string, name: MultiString},
  semanticDomains: {id: string, name: MultiString, code: string}[],
  exampleSentences: {id: string, sentence: RichMultiString, translations: {id: string, text: RichMultiString}[]}[],
  pictures: {id: string, mediaUri: string, caption: RichMultiString}[],  // load image bytes with fwlite.getMedia(picture.mediaUri)
};
\`\`\`

Errors reject with an \`Error\` that has a \`code\`: \`unknown-method\`, \`invalid-args\`, \`permission-denied\`, \`not-supported\`, \`storage-full\`, or \`internal\`.

## Writing systems & headwords

A word can be written in several **writing systems** (\`wsId\`s). They come in kinds:
- The **default** (first) vernacular writing system is the primary orthography — what you normally show.
- **Audio** writing systems (\`isAudio: true\`) store audio-file references, **not text**. Never render their values as text (doing so prints a file id, not a word) — skip them.
- Other variants (IPA/phonetic like \`seh-fonipa\`, dialects, etc.) are real text but are *alternates*, not the main form. Don't use them as a fallback for the headword.

The **headword** is how the app names an entry: the citation form if set, otherwise the lexeme form decorated with the word's morph-type affix markers (e.g. an affix shows as \`-s\` or \`un-\`). Getting this right by hand is fiddly, so **every entry you receive already has a computed \`entry.headword\`** — use \`fwlite.headword(entry)\` (or read \`entry.headword\`) instead of piecing together \`citationForm\`/\`lexemeForm\` yourself. It's read-only; you don't send it back when writing.

${projectSection}

${culturalSection}## Design guidance

${responsiveGuidance}
- **Theme & palette**: define a small set of CSS custom properties on \`:root\` (\`--bg\`, \`--surface\`, \`--border\`, \`--text\`, \`--text-muted\`, \`--accent\`, \`--radius\`) and drive every color through them. Provide dark values under \`html[data-theme="dark"]\`, then set \`document.documentElement.dataset.theme = fwlite.theme\` after ready (respecting \`prefers-color-scheme\` as a fallback). Never hard-code raw colors on elements.
- Use the system font stack; the vernacular writing systems may specify a \`font\` name you can add to a font-family list.
- Always render **loading**, **empty** ("no entries yet") and **error** states for async data.
- Escape dictionary data before inserting it into the DOM (\`textContent\`, not \`innerHTML\`) — it can contain any characters.
- Users of this plugin are dictionary editors, not developers — keep the UI friendly and obvious.

## Capabilities & limits inside the sandbox

- **Available offline**: Canvas 2D, WebGL, Web Audio, and WebAssembly all work — no network needed. Use them freely for games and visualizations.
- **No external resources**: everything must be inline (see Hard requirements). Embed any binary art/audio as \`data:\` URIs, or generate it procedurally at runtime.
- **CSS variables don't reach WebGL/Canvas.** If you drive a renderer, read the resolved colors with \`getComputedStyle(document.documentElement).getPropertyValue('--accent')\` (etc.) and pass them in; re-read them when \`fwlite.theme\` changes.
- **No random-sampling query**: to pick random entries, load the set once (\`getEntries({limit: ...})\`) and sample in memory — there are only ~${entryCount} entries.

## Skeleton

\`\`\`html
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>My plugin</title>
<style>
  :root { --bg: #f5f6f8; --surface: #fff; --border: #e2e4e9; --text: #1c1e21; --text-muted: #6b7280; --accent: #4f46e5; --radius: 12px; }
  html[data-theme="dark"] { --bg: #16171b; --surface: #1f2126; --border: #33353c; --text: #edeef1; --text-muted: #9aa0ab; --accent: #818cf8; }
  body { margin: 0; padding: 16px; background: var(--bg); color: var(--text); font-family: system-ui, sans-serif; }
  /* your styles — always reference the variables above */
</style>
</head>
<body>
<main id="app">Loading…</main>
<script>
fwlite.ready.then(async () => {
  document.documentElement.dataset.theme = fwlite.theme;
  const entries = await fwlite.getEntries({limit: 500});
  // build your UI…
});
</script>
</body>
</html>
\`\`\`
`;
}
