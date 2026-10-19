import type {IEntry, ISense, IWritingSystem, IWritingSystems} from '$lib/dotnet-types';
import {describe, expect, it} from 'vitest';

import type {MorphTypesService} from './morph-types.svelte';
import type {ProjectContext} from '$project/project-context.svelte';
import type {View} from '$lib/views/view-data';
import {WritingSystemService} from './writing-system-service.svelte';

// The dedup only looks at wsId, so a minimal stub is enough (distinct object per call so we can
// assert *which* occurrence was kept).
function ws(wsId: string): IWritingSystem {
  return {wsId} as unknown as IWritingSystem;
}

function audioWs(wsId: string): IWritingSystem {
  return {wsId, isAudio: true} as unknown as IWritingSystem;
}

function viewWith({vernacular, analysis}: {vernacular?: string[]; analysis?: string[]} = {}): View {
  return {
    vernacular: vernacular?.map((wsId) => ({wsId})),
    analysis: analysis?.map((wsId) => ({wsId})),
  } as unknown as View;
}

function entryWith({lexemeForm = {}, citationForm = {}}: {lexemeForm?: Record<string, string>; citationForm?: Record<string, string>}): IEntry {
  return {lexemeForm, citationForm, senses: []} as unknown as IEntry;
}

function senseWith({gloss = {}, definition = {}}: {gloss?: Record<string, string>; definition?: Record<string, string>}): ISense {
  return {gloss, definition} as unknown as ISense;
}

// Decoration is the morph-type service's job; the identity stub keeps expectations readable.
const morphTypes = {decorate: (form: string | undefined) => form} as unknown as MorphTypesService;

function serviceWith(vernacular: IWritingSystem[], analysis: IWritingSystem[] = []): WritingSystemService {
  const writingSystems: IWritingSystems = {vernacular, analysis};
  const projectContext = {apiResource: () => ({current: writingSystems})} as unknown as ProjectContext;
  return new WritingSystemService(projectContext, morphTypes);
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

describe('viewVernacularNoAudio', () => {
  const service = serviceWith([ws('seh'), audioWs('seh-audio'), ws('ny')]);
  function wsIds(view: View) {
    return service.viewVernacularNoAudio(view).map((w) => w.wsId);
  }

  it("keeps the view's text writing systems and drops audio ones", () => {
    expect(wsIds(viewWith({vernacular: ['ny', 'seh-audio']}))).toEqual(['ny']);
  });

  it('uses every text vernacular when the view does not restrict writing systems', () => {
    expect(wsIds(viewWith())).toEqual(['seh', 'ny']);
  });

  it('falls back to every text vernacular when the view only shows audio', () => {
    expect(wsIds(viewWith({vernacular: ['seh-audio']}))).toEqual(['seh', 'ny']);
  });
});

describe('headwords', () => {
  // Project vernaculars in project order: seh, ny, en (+ an audio one, which never carries a headword)
  const service = serviceWith([ws('seh'), audioWs('seh-audio'), ws('ny'), ws('en')]);

  describe('headword (one writing system, no fallback)', () => {
    it('returns the form in that writing system', () => {
      const entry = entryWith({lexemeForm: {seh: 'casa', ny: 'nyumba'}});

      expect(service.headword(entry, 'ny')).toBe('nyumba');
    });

    it('prefers the citation form over the lexeme form', () => {
      const entry = entryWith({lexemeForm: {seh: 'casa'}, citationForm: {seh: 'casas'}});

      expect(service.headword(entry, 'seh')).toBe('casas');
    });

    it('is empty when the entry has no form there, even if other writing systems do', () => {
      const entry = entryWith({lexemeForm: {seh: 'casa'}});

      expect(service.headword(entry, 'ny')).toBe('');
    });
  });

  describe('firstHeadword (any text vernacular, project order)', () => {
    it('returns the first vernacular with a form, in project order', () => {
      const entry = entryWith({lexemeForm: {ny: 'nyumba', en: 'house'}});

      expect(service.firstHeadword(entry)).toBe('nyumba');
    });

    it('skips audio writing systems', () => {
      const entry = entryWith({lexemeForm: {'seh-audio': 'casa.wav', en: 'house'}});

      expect(service.firstHeadword(entry)).toBe('house');
    });

    it('is empty when the entry has no form anywhere', () => {
      expect(service.firstHeadword(entryWith({}))).toBe('');
    });
  });

  describe("viewBestHeadword (the given or the first of the view's vernaculars, otherwise any)", () => {
    it('prefers the given writing system', () => {
      const entry = entryWith({lexemeForm: {seh: 'casa', ny: 'nyumba'}});

      expect(service.viewBestHeadword(entry, viewWith({vernacular: ['seh', 'ny']}), 'ny')).toBe('nyumba');
    });

    it("prefers the view's default writing system when none is given", () => {
      const entry = entryWith({lexemeForm: {seh: 'casa', ny: 'nyumba'}});

      expect(service.viewBestHeadword(entry, viewWith({vernacular: ['ny', 'en']}))).toBe('nyumba');
    });

    it("then falls back to the view's other vernaculars, in project order, before any hidden one", () => {
      const entry = entryWith({lexemeForm: {seh: 'casa', en: 'house'}});

      expect(service.viewBestHeadword(entry, viewWith({vernacular: ['ny', 'en']}), 'ny')).toBe('house');
    });

    it('then falls back to a vernacular the view hides', () => {
      const entry = entryWith({lexemeForm: {seh: 'casa'}});

      expect(service.viewBestHeadword(entry, viewWith({vernacular: ['ny']}), 'ny')).toBe('casa');
    });

    it('is empty when the entry has no form anywhere', () => {
      expect(service.viewBestHeadword(entryWith({}), viewWith({vernacular: ['ny']}))).toBe('');
    });
  });

  describe('viewBestHeadwordIn (viewBestHeadword plus the writing system it came from)', () => {
    it('names the writing system the headword came from', () => {
      const entry = entryWith({lexemeForm: {seh: 'casa'}});

      const best = service.viewBestHeadwordIn(entry, viewWith({vernacular: ['ny']}), 'ny');

      expect(best?.value).toBe('casa');
      expect(best?.ws.wsId).toBe('seh');
    });

    it('is undefined when the entry has no form anywhere', () => {
      expect(service.viewBestHeadwordIn(entryWith({}), viewWith())).toBeUndefined();
    });
  });
});

describe('viewFirstDefOrGlossVal', () => {
  const service = serviceWith([], [ws('en'), ws('pt')]);

  it("prefers the view's analysis writing systems", () => {
    const sense = senseWith({gloss: {en: 'house', pt: 'casa'}});

    expect(service.viewFirstDefOrGlossVal(sense, viewWith({analysis: ['pt']}))).toBe('casa');
  });

  it('falls back to every analysis writing system when the view has no value', () => {
    const sense = senseWith({gloss: {en: 'house'}});

    expect(service.viewFirstDefOrGlossVal(sense, viewWith({analysis: ['pt']}))).toBe('house');
  });
});
