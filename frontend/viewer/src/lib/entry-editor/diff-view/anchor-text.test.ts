import {describe, expect, it} from 'vitest';
import type {IEntry, IExampleSentence, ISense, IWritingSystem, IWritingSystems} from '$lib/dotnet-types';
import type {MorphTypesService} from '$project/data/morph-types.svelte';
import type {ProjectContext} from '$project/project-context.svelte';
import type {View} from '$lib/views/view-data';
import {WritingSystemService} from '$project/data';
import {entryAnchor, exampleAnchor, objectAnchor, senseAnchor} from './anchor-text';

function ws(wsId: string): IWritingSystem {
  return {wsId} as unknown as IWritingSystem;
}
const morphTypes = {decorate: (form: string | undefined) => form} as unknown as MorphTypesService;
const writingSystems: IWritingSystems = {vernacular: [ws('seh')], analysis: [ws('en'), ws('pt')]};
const projectContext = {apiResource: () => ({current: writingSystems})} as unknown as ProjectContext;
const service = new WritingSystemService(projectContext, morphTypes);
const view = {} as unknown as View;

describe('entryAnchor', () => {
  it('names each side by its headword and homograph number', () => {
    const before = {lexemeForm: {seh: 'nyumba'}, citationForm: {}, homographNumber: 1} as unknown as IEntry;
    const after = {lexemeForm: {seh: 'nyumbo'}, citationForm: {}, homographNumber: 2} as unknown as IEntry;
    expect(entryAnchor(service, view, before, after)).toEqual({before: 'nyumba', after: 'nyumbo', beforeHomograph: 1, afterHomograph: 2});
    expect(entryAnchor(service, view, undefined, after).before).toBeUndefined();
  });
});

describe('senseAnchor', () => {
  it('prefers the gloss', () => {
    const before = {gloss: {en: 'house'}, definition: {en: 'a building'}} as unknown as ISense;
    const after = {gloss: {en: 'home'}, definition: {en: 'a building'}} as unknown as ISense;
    expect(senseAnchor(service, view, before, after)).toEqual({before: 'house', after: 'home'});
  });

  it('falls back to the definition when neither side has a gloss', () => {
    const sense = {gloss: {}, definition: {pt: {spans: [{text: 'casa', ws: 'pt'}]}}} as unknown as ISense;
    expect(senseAnchor(service, view, undefined, sense)).toEqual({before: undefined, after: 'casa'});
  });
});

describe('exampleAnchor', () => {
  it('names an example by its sentence', () => {
    const example = {sentence: {seh: {spans: [{text: 'Nyumba yanga', ws: 'seh'}]}}} as unknown as IExampleSentence;
    expect(exampleAnchor(service, view, example, undefined)).toEqual({before: 'Nyumba yanga', after: undefined});
  });
});

describe('objectAnchor', () => {
  it('names most objects by their name', () => {
    expect(objectAnchor(service, 'PartOfSpeech', {name: {en: 'Noun'}}, {name: {en: 'Common noun'}}))
      .toEqual({before: 'Noun', after: 'Common noun', detail: undefined});
    expect(objectAnchor(service, 'CustomView', undefined, {name: 'Quick gloss'}).after).toBe('Quick gloss');
  });

  it('names a semantic domain by code and name', () => {
    expect(objectAnchor(service, 'SemanticDomain', {code: '1.1', name: {en: 'Sky'}}, {code: '1.1', name: {en: 'Heaven'}}))
      .toEqual({before: '1.1 Sky', after: '1.1 Heaven', detail: undefined});
  });

  it('adds the code of a writing system', () => {
    expect(objectAnchor(service, 'WritingSystem', {name: 'Sena', wsId: 'seh'}, {name: 'Sena', wsId: 'seh'}))
      .toEqual({before: 'Sena', after: 'Sena', detail: 'seh'});
  });
});
