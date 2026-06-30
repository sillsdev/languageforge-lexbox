import {describe, it, expect} from 'vitest';
import {readFileSync, readdirSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {dirname, join} from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const generatedModels = join(here, '../dotnet-types/generated-types/MiniLcm/Models');
const previewComponent = join(here, 'ActivityItemChangePreview.svelte');

/**
 * Every entity type the backend can emit as a change context. `IChangeContext.entityType` is set in
 * HistoryService.cs (`(Snapshot ?? PreviousSnapshot)?.GetType().Name`), so it's the C# class name of any
 * snapshot, i.e. every concrete `IObjectWithId` implementer. Those map 1:1 to the generated
 * `I<TypeName>.ts` interfaces that `extends IObjectWithId`, which we read here so a newly-added model type
 * is discovered automatically.
 */
function emittableEntityTypes(): string[] {
  return readdirSync(generatedModels)
    .filter((file) => file.endsWith('.ts'))
    .filter((file) => /extends IObjectWithId(\b|$)/m.test(readFileSync(join(generatedModels, file), 'utf8')))
    .map((file) => file.replace(/^I/, '').replace(/\.ts$/, ''));
}

/**
 * Entity types handled by a styled `entityType === '...'` branch in the preview. Every such literal in the
 * component is a styled branch — the raw-JSON fallback and the no-snapshot guard don't compare entityType —
 * so matching the literals tells us exactly which types render a real diff rather than falling through to JSON.
 */
function styledBranchTypes(): Set<string> {
  const source = readFileSync(previewComponent, 'utf8');
  return new Set([...source.matchAll(/entityType === '([^']+)'/g)].map((m) => m[1]));
}

// Types that intentionally fall through to the raw-JSON fallback (no styled diff). These are NOT IObjectWithId
// implementers today (RemoteResource isn't IObjectWithId, so it surfaces as entityType "Unknown"), so the
// derived set below won't contain them — the list documents intent and absorbs any future IObjectWithId type
// we deliberately choose not to preview. Adding a type here is a conscious opt-out, reviewed in the diff.
const FALLBACK_ONLY: string[] = [];

describe('activity preview covers every emittable entity type', () => {
  it('the model set we derive is the full vocab + lexical set (guards the derivation itself)', () => {
    // If this drifts, the derivation broke or the model set changed — re-check before trusting the test below.
    expect(emittableEntityTypes().sort()).toEqual(
      [
        'ComplexFormComponent',
        'ComplexFormType',
        'CustomView',
        'Entry',
        'ExampleSentence',
        'MorphType',
        'PartOfSpeech',
        'Publication',
        'SemanticDomain',
        'Sense',
        'WritingSystem',
      ].sort(),
    );
  });

  it('every emittable entity type has a styled preview branch or is an explicit fallback opt-out', () => {
    const styled = styledBranchTypes();
    const allowed = new Set(FALLBACK_ONLY);
    const unhandled = emittableEntityTypes().filter((type) => !styled.has(type) && !allowed.has(type));
    expect(unhandled).toEqual([]);
  });

  it('the vocab object types specifically render styled previews (mutation guard)', () => {
    // Named explicitly so removing any one branch fails here even if the derivation above were ever weakened.
    const styled = styledBranchTypes();
    for (const vocabType of ['PartOfSpeech', 'SemanticDomain', 'Publication', 'ComplexFormType', 'MorphType', 'WritingSystem', 'CustomView']) {
      expect(styled, `${vocabType} must have a styled entityType branch in ActivityItemChangePreview.svelte`).toContain(vocabType);
    }
  });
});
