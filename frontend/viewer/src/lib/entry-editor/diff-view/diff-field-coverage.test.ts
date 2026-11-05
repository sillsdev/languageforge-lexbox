import {describe, it, expect} from 'vitest';
import {readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {dirname, join} from 'node:path';
import {entityFieldIds, type EntityType} from '$lib/views/entity-config';

const here = dirname(fileURLToPath(import.meta.url));
const generatedModels = join(here, '../../dotnet-types/generated-types/MiniLcm/Models');

function modelKeys(model: string): string[] {
  const source = readFileSync(join(generatedModels, `I${model}.ts`), 'utf8');
  return [...source.matchAll(/^\t(\w+)\??:/gm)].map((m) => m[1]);
}

function renderedFieldIds(file: string): string[] {
  const source = readFileSync(join(here, file), 'utf8');
  return [...source.matchAll(/fieldId="(\w+)"/g)].map((m) => m[1]);
}

// Links between entries, previewed by DiffEntryLinkList outside the entry primitive.
const NOT_DIFFED: Partial<Record<EntityType, string[]>> = {
  entry: ['complexForms', 'components'],
};

const cases: {entity: 'entry' | 'sense' | 'example'; model: string; file: string}[] = [
  {entity: 'entry', model: 'Entry', file: 'DiffEntryPrimitive.svelte'},
  {entity: 'sense', model: 'Sense', file: 'DiffSensePrimitive.svelte'},
  {entity: 'example', model: 'ExampleSentence', file: 'DiffExamplePrimitive.svelte'},
];

// Model keys a primitive shows outside the view's fields, so their changes count as visible.
const SHOWN_OUTSIDE_VIEW: Partial<Record<EntityType, string[]>> = {
  entry: ['morphType', 'homographNumber'],
};

// Model keys whose changes a primitive doesn't show: a change to only these reads "No visible changes".
const NOT_VISIBLE: Record<'entry' | 'sense' | 'example', string[]> = {
  entry: ['id', 'deletedAt', 'senses', ...NOT_DIFFED.entry ?? []],
  // partOfSpeechId is the source of truth
  sense: ['id', 'deletedAt', 'entryId', 'partOfSpeech', 'exampleSentences'],
  example: ['id', 'deletedAt', 'senseId'],
};

describe('diff primitives stay in sync with the editor fields', () => {
  for (const {entity, file} of cases) {
    it(`${file} renders exactly the diffable ${entity} fields`, () => {
      const allowedToSkip = new Set(NOT_DIFFED[entity] ?? []);
      const expected = entityFieldIds(entity).filter((id) => !allowedToSkip.has(id)).sort();
      const rendered = [...new Set(renderedFieldIds(file))].sort();
      expect(rendered).toEqual(expected);
    });
  }
});

describe('every model key is either diffed or explicitly not visible', () => {
  for (const {entity, model, file} of cases) {
    it(`${model}`, () => {
      const keys = modelKeys(model);
      expect(keys.length).toBeGreaterThan(0);
      const shown = [...entityFieldIds(entity), ...SHOWN_OUTSIDE_VIEW[entity] ?? []]
        .filter((id) => !(NOT_DIFFED[entity] ?? []).includes(id));
      const notVisible = NOT_VISIBLE[entity];
      expect(shown.filter((id) => notVisible.includes(id))).toEqual([]);
      expect([...shown, ...notVisible].sort()).toEqual([...keys].sort());
      const source = readFileSync(join(here, file), 'utf8');
      for (const id of SHOWN_OUTSIDE_VIEW[entity] ?? []) expect(source).toContain(`.${id}`);
    });
  }
});
