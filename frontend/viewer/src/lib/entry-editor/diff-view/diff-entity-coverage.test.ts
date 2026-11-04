import {describe, it, expect} from 'vitest';
import {readFileSync, readdirSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {dirname, join} from 'node:path';
import {objectDiffRows} from './diff-fields';

const generatedModels = join(dirname(fileURLToPath(import.meta.url)), '../../dotnet-types/generated-types/MiniLcm/Models');

// Diffed by their own components rather than objectDiffRows
const OWN_DIFF = ['Entry', 'Sense', 'ExampleSentence', 'ComplexFormComponent'];
// Comments have their own UI
const NOT_DIFFED = ['CommentThread', 'UserComment'];

describe('every model the backend can snapshot has a diff', () => {
  const models = readdirSync(generatedModels)
    .filter((file) => /extends IObjectWithId\b/.test(readFileSync(join(generatedModels, file), 'utf8')))
    .map((file) => file.replace(/^I|\.ts$/g, ''));
  const covered = [...Object.keys(objectDiffRows), ...OWN_DIFF, ...NOT_DIFFED];

  it('new models get a diff or an explicit opt-out', () => {
    expect(models.length).toBeGreaterThan(0);
    expect(models.filter((model) => !covered.includes(model))).toEqual([]);
  });

  it('lists no model that no longer exists', () => {
    expect(covered.filter((model) => !models.includes(model))).toEqual([]);
  });
});
