import {describe, it, expect} from 'vitest';
import {readFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {HANDLED_VOCAB_FIELDS, IGNORED_VOCAB_FIELDS} from './vocab-diff-fields';

// The vocab models DiffVocabPrimitive renders. Each must have every property either handled (a row) or
// deliberately ignored — so a new backend field can't silently vanish from the diff preview (the bug that
// once dropped MorphType's `description`). Property names are parsed from the generated interface files.
const MODELS = ['IPartOfSpeech', 'ISemanticDomain', 'IPublication', 'IComplexFormType', 'IMorphType', 'IWritingSystem', 'ICustomView'];

function propertyNames(model: string): string[] {
  // vitest runs with cwd = frontend/viewer, so resolve the generated interface from there.
  const path = resolve(process.cwd(), 'src/lib/dotnet-types/generated-types/MiniLcm/Models', `${model}.ts`);
  const src = readFileSync(path, 'utf8');
  const body = src.slice(src.indexOf('{') + 1, src.lastIndexOf('}'));
  // Lines like `\tname: IMultiString;` or `\tprefix?: string;` — capture the identifier before `?:`/`:`.
  return [...body.matchAll(/^\s*([a-zA-Z_$][\w$]*)\??\s*:/gm)].map((m) => m[1]);
}

describe('DiffVocabPrimitive field coverage', () => {
  for (const model of MODELS) {
    it(`handles or ignores every property of ${model}`, () => {
      const props = propertyNames(model);
      expect(props.length).toBeGreaterThan(0); // guard against a parse that silently found nothing
      const uncovered = props.filter((p) => !HANDLED_VOCAB_FIELDS.has(p) && !IGNORED_VOCAB_FIELDS.has(p));
      expect(uncovered, `${model} has diff-uncovered fields — add a row to DiffVocabPrimitive or ignore them in vocab-diff-fields.ts`).toEqual([]);
    });
  }
});
