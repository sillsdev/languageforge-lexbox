import {describe, it, expect} from 'vitest';
import {readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {dirname, join} from 'node:path';
import {entityFieldIds, type EntityType} from '$lib/views/entity-config';

const here = dirname(fileURLToPath(import.meta.url));

function renderedFieldIds(file: string): string[] {
  const source = readFileSync(join(here, file), 'utf8');
  return [...source.matchAll(/fieldId="(\w+)"/g)].map((m) => m[1]);
}

// Relation-list fields that are intentionally not rendered as inline field diffs (they're shown via
// the dedicated complex-form preview path instead). Adding a NEW field to entity-config without a diff
// leaf — or without listing it here — makes this test fail, which is the point.
const NOT_DIFFED: Partial<Record<EntityType, string[]>> = {
  entry: ['complexForms', 'components'],
};

const cases: {entity: EntityType; file: string}[] = [
  {entity: 'entry', file: 'DiffEntryPrimitive.svelte'},
  {entity: 'sense', file: 'DiffSensePrimitive.svelte'},
  {entity: 'example', file: 'DiffExamplePrimitive.svelte'},
];

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
