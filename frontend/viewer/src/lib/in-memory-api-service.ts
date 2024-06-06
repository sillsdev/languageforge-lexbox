import type {
  LexboxApiClient,
  IEntry,
  IExampleSentence,
  ISense,
  JsonPatch,
  LexboxApiFeatures,
  QueryOptions,
  WritingSystemType,
  WritingSystems
} from './services/lexbox-api';
import {entries, projectName, writingSystems} from './entry-data';

import { type WritingSystem } from './mini-lcm';
import { headword } from './utils';
import { applyPatch } from 'fast-json-patch';

function filterEntries(entries: IEntry[], query: string) {
  return entries.filter(entry =>
    [
      ...Object.values(entry.lexemeForm ?? {}),
      ...Object.values(entry.citationForm ?? {}),
      ...Object.values(entry.literalMeaning ?? {}),
      ...entry.senses.flatMap(sense => [
        ...Object.values(sense.gloss ?? {}),
      ]),
    ].some(value => value?.toLowerCase().includes(query.toLowerCase())))
}

export class InMemoryApiService implements LexboxApiClient {

  SupportedFeatures(): LexboxApiFeatures {
    return {};
  }

  readonly projectName = projectName;

  private _entries = entries;
  private _Entries(): IEntry[] {
    return JSON.parse(JSON.stringify(this._entries));
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
    let sortWs = options.order.writingSystem;
    const defaultWs = writingSystems.vernacular[0].id;
    if (sortWs === 'default') sortWs = defaultWs;
    if (options.exemplar?.value) {
      const lowerExemplar = options.exemplar?.value.toLowerCase();
      let ws = options.exemplar?.writingSystem;
      if (ws === 'default') ws = defaultWs;
      entries = entries.filter(entry =>
        (entry.citationForm[ws] ?? entry.lexemeForm[ws] ?? '')
          ?.toLocaleLowerCase()
          ?.startsWith(lowerExemplar));
    }

    return entries
      .sort((e1, e2) => {
        const v1 = headword(e1, sortWs);
        const v2 = headword(e2, sortWs);
        if (!v2) return -1;
        if (!v1) return 1;
        let compare = v1.localeCompare(v2, sortWs);
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

  UpdateEntry(guid: string, update: JsonPatch): Promise<IEntry> {
    const entry = entries.find(e => e.id === guid)!;
    applyPatch(entry, update);
    return Promise.resolve(entry);
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

  CreateWritingSystem(type: WritingSystemType, writingSystem: WritingSystem): Promise<void> {
    throw new Error('Method not implemented.');
  }

  UpdateWritingSystem(wsId: string, type: WritingSystemType, update: JsonPatch): Promise<WritingSystem> {
    throw new Error('Method not implemented.');
  }
}
