import {describe, it, expect} from 'vitest';
import {readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {dirname, join} from 'node:path';
import {entityFieldIds, type EntityType} from './entity-config';

const generatedModels = join(dirname(fileURLToPath(import.meta.url)), '../dotnet-types/generated-types/MiniLcm/Models');

/** Property names declared on a generated model interface (tab-indented `name: Type;` lines, excluding imports). */
function modelFields(file: string): string[] {
  const source = readFileSync(join(generatedModels, file), 'utf8');
  return [...source.matchAll(/^\s+(\w+)\??:/gm)].map((m) => m[1]);
}

// Model fields that are intentionally NOT view-config fields: identity, soft-delete, parent links, sub-objects
// rendered on their own, and special-cased display fields. Adding a NEW model field without either a view-config
// entry or a line here fails this test — so activity summaries and diffs can't silently drop a new field.
const NOT_VIEW_FIELDS: Record<EntityType, string[]> = {
  entry: ['id', 'deletedAt', 'morphType', 'homographNumber', 'senses'],
  sense: ['id', 'deletedAt', 'entryId', 'partOfSpeech', 'exampleSentences', 'pictures'],
  example: ['id', 'deletedAt', 'senseId'],
};

const cases: {entity: EntityType; file: string}[] = [
  {entity: 'entry', file: 'IEntry.ts'},
  {entity: 'sense', file: 'ISense.ts'},
  {entity: 'example', file: 'IExampleSentence.ts'},
];

describe('view config covers every model field', () => {
  for (const {entity, file} of cases) {
    it(`every ${entity} model field is a view field or explicitly excluded`, () => {
      const configured = new Set<string>(entityFieldIds(entity) as string[]);
      const excluded = new Set(NOT_VIEW_FIELDS[entity]);
      const unhandled = modelFields(file).filter((f) => !configured.has(f) && !excluded.has(f));
      expect(unhandled).toEqual([]);
    });
  }
});
