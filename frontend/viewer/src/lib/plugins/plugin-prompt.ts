import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';

/**
 * Builds the project-aware prompt users paste into an AI assistant (Claude, ChatGPT, …) to have
 * it write a plugin. Kept in English on purpose: it addresses the AI, not the user, and English
 * prompts work best across models. The embedded API documentation is the authoritative public
 * reference for plugin API v1 — keep it in sync with plugin-sdk.js and plugin-api-adapter.ts.
 */
export async function buildPluginPrompt(
  api: IMiniLcmJsInvokable,
  project: {projectName: string; projectCode: string},
): Promise<string> {
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

  return `# Write a plugin for FieldWorks Lite (FW Lite)

You are writing a **plugin** for FW Lite, a dictionary-editing app used by language communities and linguists. A plugin is **one self-contained HTML file** that FW Lite runs in a sandboxed iframe and connects to the open dictionary project through a small JavaScript API.

## Hard requirements

1. **Output exactly one complete HTML file** (\`<!DOCTYPE html>\` through \`</html>\`). The user will copy-paste it as-is.
2. **Fully self-contained**: all CSS and JavaScript inline. No external scripts, stylesheets, fonts, or images (embed small images as data: URIs if needed). No build step, no modules, no frameworks.
3. **Sandboxed environment**: the plugin runs in an opaque-origin iframe. \`localStorage\`, \`sessionStorage\`, and cookies are **unavailable** — persist data with \`fwlite.storage\` instead. \`alert\`/\`confirm\` work; popups don't.
4. **No network access by default.** A Content-Security-Policy blocks all fetches and external resources. Only if the task genuinely requires internet access, declare it by adding \`<meta name="fwlite-plugin-permissions" content="internet">\` in \`<head>\` — the app then lifts the network block and shows the user an "internet" badge. Don't declare it unless it's needed.
5. A global **\`fwlite\`** API object is injected before your code runs. Data becomes available once \`fwlite.ready\` resolves — structure your code as \`fwlite.ready.then(init)\`.
6. **Writes are user-approved**: every \`createEntry\`/\`updateEntry\` call pops up a confirmation dialog in the app showing exactly what will change. If the user declines, the promise rejects with \`error.code === 'permission-denied'\` — handle that as a normal outcome (show "not saved"), never as a crash.

## The \`fwlite\` API (v1)

All methods return Promises.

### Reading

\`\`\`ts
fwlite.ready: Promise<{apiVersion: 1, project: {projectName, projectCode}, theme: 'light'|'dark', permissions: string[]}>
fwlite.project // {projectName, projectCode} — available after ready
fwlite.theme   // 'light' | 'dark' — the app's current theme; also respect prefers-color-scheme

fwlite.getWritingSystems(): Promise<{vernacular: WritingSystem[], analysis: WritingSystem[]}>
// WritingSystem: {wsId: string, name: string, abbreviation: string, font: string, isAudio: boolean, exemplars: string[]}
// The first item in each array is the default. isAudio ones hold audio file refs, not text — usually skip them.

fwlite.getEntries(query?): Promise<Entry[]>
// query: {
//   search?: string,                 // full-text search
//   limit?: number,                  // default 100, max 1000
//   offset?: number,                 // for paging
//   filter?: {semanticDomainCode?: string, partOfSpeechId?: string},
//   sort?: {writingSystem?: string, ascending?: boolean},  // sorts by headword
// }
fwlite.countEntries(query?): Promise<number>   // query: {search?, filter?}
fwlite.getEntry(id): Promise<Entry | null>
fwlite.getPartsOfSpeech(): Promise<{id: string, name: MultiString}[]>
fwlite.getSemanticDomains(): Promise<{id: string, name: MultiString, code: string}[]>
\`\`\`

### Writing (each call = one user confirmation dialog)

\`\`\`ts
fwlite.createEntry(entry): Promise<Entry>
// Provide only the fields you have; ids and empty collections are filled in for you:
// {lexemeForm: {ws: 'word'}, senses: [{gloss: {ws: 'meaning'}, semanticDomains: [domainObj], partOfSpeechId?: id}]}
// semanticDomains items must be objects obtained from getSemanticDomains().

fwlite.updateEntry(before, after): Promise<Entry>
// 'before' = the entry exactly as you fetched it; 'after' = a modified deep copy.
// Only the differences are applied, so concurrent edits to other fields survive.
\`\`\`

### Utilities

\`\`\`ts
fwlite.openEntry(entryId): Promise<void>       // navigates the app to that entry
fwlite.notify(message): Promise<void>          // shows a toast in the app
fwlite.storage.get(key): Promise<any>          // per-plugin persistent storage (JSON values, ~256KB total)
fwlite.storage.set(key, value): Promise<void>
fwlite.storage.remove(key): Promise<void>
fwlite.asText(richStringOrString): string      // flattens rich text ({spans:[{text}]}) to plain text
fwlite.firstValue(multiString, ['seh','en']): string  // first non-empty value, preferring given writing systems
\`\`\`

### Data model

\`\`\`ts
type MultiString = {[writingSystemId: string]: string};           // e.g. {en: 'dog', fr: 'chien'}
type RichMultiString = {[writingSystemId: string]: RichString};   // rich text; flatten with fwlite.asText
type RichString = {spans: {text: string, ws: string}[]};

type Entry = {
  id: string,
  lexemeForm: MultiString,       // the word (vernacular)
  citationForm: MultiString,     // dictionary form; headword = citationForm || lexemeForm
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
};
\`\`\`

Errors reject with an \`Error\` that has a \`code\`: \`unknown-method\`, \`invalid-args\`, \`permission-denied\`, \`storage-full\`, or \`internal\`.

## This project

The plugin will run **only** inside this project, so hard-coding its writing systems, part-of-speech ids, and semantic domain codes is fine and keeps the code simple.

- Project: **${project.projectName}** (code \`${project.projectCode}\`)
- Entries: ~${entryCount}
- Vernacular writing systems (the language being documented):
${vernacular}
- Analysis writing systems (meanings/translations):
${analysis}
- Parts of speech:
${posList}
- Semantic domains: ${semanticDomains.length} defined${domainSample ? ` — e.g. ${domainSample}` : ''}

## Design guidance

- **Responsive**: must work from a 360px phone to a desktop; no horizontal page scrolling. Test your layout mentally at both sizes.
- **Theme**: support light and dark mode. Read \`fwlite.theme\` after ready and respect \`prefers-color-scheme\`; drive colors through CSS custom properties.
- Use the system font stack; the vernacular writing systems may specify a \`font\` name you can add to a font-family list.
- Always render **loading**, **empty** ("no entries yet") and **error** states for async data.
- Escape dictionary data before inserting it into the DOM (\`textContent\`, not \`innerHTML\`) — it can contain any characters.
- Users of this plugin are dictionary editors, not developers — keep the UI friendly and obvious.

## Skeleton

\`\`\`html
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>My plugin</title>
<style>/* your styles */</style>
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

---

## What I want the plugin to do

REPLACE THIS PARAGRAPH with a description of the plugin you want — what it shows or does, roughly how it should look, and anything it should save between sessions.
`;
}
