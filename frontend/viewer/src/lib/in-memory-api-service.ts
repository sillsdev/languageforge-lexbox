/* eslint-disable @typescript-eslint/naming-convention */

import {entries, projectName, writingSystems} from './entry-data';
import type {
  IEntry,
  IExampleSentence,
  ISense,
  JsonPatch,
  LexboxApiClient,
  LexboxApiFeatures,
  PartOfSpeech,
  QueryOptions,
  SemanticDomain,
  WritingSystemType,
  WritingSystems
} from './services/lexbox-api';

import {applyPatch} from 'fast-json-patch';
import {pickWs, type ComplexFormType, type WritingSystem} from './mini-lcm';
import {headword} from './utils';

const complexFormTypes = entries
  .flatMap(entry => entry.complexFormTypes)
  .filter((value, index, all) => all.findIndex(v2 => v2.id === value.id) === index);

function filterEntries(entries: IEntry[], query: string): IEntry[] {
  return entries.filter(entry =>
    [
      ...Object.values(entry.lexemeForm ?? {}),
      ...Object.values(entry.citationForm ?? {}),
      ...entry.senses.flatMap(sense => [
        ...Object.values(sense.gloss ?? {}),
      ]),
    ].some(value => value?.toLowerCase().includes(query.toLowerCase())));
}

export class InMemoryApiService implements LexboxApiClient {
  GetComplexFormTypes(): Promise<ComplexFormType[]> {
    return Promise.resolve(
      //*
      complexFormTypes
      /*/
      [
        {id: '13', name: {en: 'Compound'},},
        {id: '15', name: {en: 'Idiom'},}
      ]
      //*/
    );
  }

  GetPartsOfSpeech(): Promise<PartOfSpeech[]> {
    return Promise.resolve(
      [
        {id: '86ff66f6-0774-407a-a0dc-3eeaf873daf7', name: {en: 'Verb'},},
        {id: 'a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5', name: {en: 'Noun'},}
      ]
    );
  }

  GetSemanticDomains(): Promise<SemanticDomain[]> {
    return Promise.resolve([
      {id: '36e8f1df-1798-4ae6-904d-600ca6eb4145', name: {en: 'Fruit'}, code: '1'},
      {id: 'Animal', name: {en: 'Animal'}, code: '2'},
    ]);
  }

  SupportedFeatures(): LexboxApiFeatures {
    return {
      write: true,
    };
  }

  readonly projectName = projectName;

  private _entries = entries;

  private _Entries(): IEntry[] {
    return JSON.parse(JSON.stringify(this._entries)) as IEntry[];
  }

  GetEntries(options: QueryOptions | undefined): Promise<IEntry[]> {
    return Promise.resolve(this.ApplyQueryOptions(this._Entries(), options));
  }

  GetWritingSystems(): Promise<WritingSystems> {
    return Promise.resolve(writingSystems);
  }

  SearchEntries(query: string, options: QueryOptions | undefined): Promise<IEntry[]> {
    return Promise.resolve(this.ApplyQueryOptions(filterEntries(this._Entries(), query), options));
  }

  private ApplyQueryOptions(entries: IEntry[], options: QueryOptions | undefined): IEntry[] {
    if (!options) return entries;
    const defaultWs = writingSystems.vernacular[0].wsId;
    if (options.exemplar?.value) {
      const lowerExemplar = options.exemplar.value.toLowerCase();
      const exemplarWs = pickWs(options.exemplar.writingSystem, defaultWs);
      entries = entries.filter(entry =>
        (entry.citationForm[exemplarWs] ?? entry.lexemeForm[exemplarWs] ?? '')
          ?.toLocaleLowerCase()
          ?.startsWith(lowerExemplar));
    }

    const sortWs = pickWs(options.order.writingSystem, defaultWs);
    return entries
      .sort((e1, e2) => {
        const v1 = headword(e1, sortWs);
        const v2 = headword(e2, sortWs);
        if (!v2) return -1;
        if (!v1) return 1;
        const compare = v1.localeCompare(v2, sortWs);
        if (compare !== 0) return compare;
        return e1.id.localeCompare(e2.id);
      })
      .slice(options.offset, options.offset + options.count);
  }

  GetEntry(guid: string): Promise<IEntry> {
    return Promise.resolve(entries.find(e => e.id === guid)!);
  }

  CreateEntry(entry: IEntry): Promise<IEntry> {
    this._entries.push(entry);
    return Promise.resolve(entry);
  }

  UpdateEntry(_before: IEntry, after: IEntry): Promise<IEntry> {
    entries.splice(entries.findIndex(e => e.id === after.id), 1, after);
    return Promise.resolve(after);
  }

  CreateSense(entryGuid: string, sense: ISense): Promise<ISense> {
    this._entries.find(e => e.id === entryGuid)?.senses.push(sense);
    return Promise.resolve(sense);
  }

  UpdateSense(entryGuid: string, senseGuid: string, update: JsonPatch): Promise<ISense> {
    const entry = entries.find(e => e.id === entryGuid)!;
    const sense = entry.senses.find(s => s.id === senseGuid)!;
    applyPatch(sense, update);
    return Promise.resolve(sense);
  }

  CreateExampleSentence(entryGuid: string, senseGuid: string, exampleSentence: IExampleSentence): Promise<IExampleSentence> {
    this._entries.find(e => e.id === entryGuid)?.senses.find(s => s.id === senseGuid)?.exampleSentences.push(exampleSentence);
    return Promise.resolve(exampleSentence);
  }

  UpdateExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string, update: JsonPatch): Promise<IExampleSentence> {
    const entry = entries.find(e => e.id === entryGuid)!;
    const sense = entry.senses.find(s => s.id === senseGuid)!;
    const exampleSentence = sense.exampleSentences.find(es => es.id === exampleSentenceGuid)!;
    applyPatch(exampleSentence, update);
    return Promise.resolve(exampleSentence);
  }

  DeleteEntry(guid: string): Promise<void> {
    entries.slice(entries.findIndex(e => e.id === guid), 1);
    return Promise.resolve();
  }

  DeleteSense(entryGuid: string, senseGuid: string): Promise<void> {
    const entry = this._entries.find(e => e.id === entryGuid)!;
    entry.senses.slice(entry.senses.findIndex(s => s.id === senseGuid), 1);
    return Promise.resolve();
  }

  DeleteExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string): Promise<void> {
    const entry = this._entries.find(e => e.id === entryGuid)!;
    const sense = entry.senses.find(s => s.id === senseGuid)!;
    sense.exampleSentences.slice(sense.exampleSentences.findIndex(es => es.id === exampleSentenceGuid), 1);
    return Promise.resolve();
  }

  CreateWritingSystem(_type: WritingSystemType, _writingSystem: WritingSystem): Promise<void> {
    throw new Error('Method not implemented.');
  }

  UpdateWritingSystem(_wsId: string, _type: WritingSystemType, _update: JsonPatch): Promise<WritingSystem> {
    throw new Error('Method not implemented.');
  }
}
