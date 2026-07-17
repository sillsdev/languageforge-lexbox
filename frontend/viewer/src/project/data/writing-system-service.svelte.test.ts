import {describe, expect, it} from 'vitest';
import type {IWritingSystem, IWritingSystems} from '$lib/dotnet-types';
import type {ProjectContext} from '$project/project-context.svelte';
import type {MorphTypesService} from './morph-types.svelte';
import {WritingSystemService} from './writing-system-service.svelte';

// The dedup only looks at wsId, so a minimal stub is enough (distinct object per call so we can
// assert *which* occurrence was kept).
function ws(wsId: string): IWritingSystem {
  return {wsId} as unknown as IWritingSystem;
}

function serviceWith(vernacular: IWritingSystem[], analysis: IWritingSystem[]): WritingSystemService {
  const writingSystems: IWritingSystems = {vernacular, analysis};
  // Only #wsResource.current is read on the writing-system-selection path; morphTypesService is unused there.
  const projectContext = {apiResource: () => ({current: writingSystems})} as unknown as ProjectContext;
  return new WritingSystemService(projectContext, {} as unknown as MorphTypesService);
}

describe('uniqueWritingSystems', () => {
  it('drops a writing system present in both lists, keeping the first (vernacular) occurrence', () => {
    const vernacularEn = ws('en');
    const service = serviceWith([vernacularEn, ws('fr')], [ws('en'), ws('es')]);

    const result = service.uniqueWritingSystems();

    expect(result.map((w) => w.wsId)).toEqual(['en', 'fr', 'es']);
    // The kept "en" is the vernacular one, not the analysis duplicate.
    expect(result[0]).toBe(vernacularEn);
  });

  it('keeps every writing system when there are no duplicates', () => {
    const service = serviceWith([ws('fr')], [ws('en')]);

    expect(service.uniqueWritingSystems().map((w) => w.wsId)).toEqual(['fr', 'en']);
  });

  it('de-duplicates against the requested order (analysis-vernacular keeps the analysis one)', () => {
    const analysisEn = ws('en');
    const service = serviceWith([ws('en'), ws('fr')], [analysisEn, ws('es')]);

    const result = service.uniqueWritingSystems('analysis-vernacular');

    expect(result.map((w) => w.wsId)).toEqual(['en', 'es', 'fr']);
    expect(result[0]).toBe(analysisEn);
  });
});
