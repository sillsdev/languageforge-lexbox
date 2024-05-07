import type {
  IEntry,
  IExampleSentence,
  ISense,
  JsonPatch,
  LexboxApi,
  QueryOptions,
  WritingSystemType,
  WritingSystems
} from './services/lexbox-api';
import {entries, writingSystems} from './entry-data';

import { ExampleSentence, type WritingSystem } from './mini-lcm';
import { filterEntries, firstVal } from './utils';

export class InMemoryApiService implements LexboxApi {
  GetEntries(options: QueryOptions | undefined): Promise<IEntry[]> {
    return Promise.resolve(this.ApplyQueryOptions(entries, options));
  }

  GetWritingSystems(): Promise<WritingSystems> {
    return Promise.resolve(writingSystems);
  }

  SearchEntries(query: string, options: QueryOptions | undefined): Promise<IEntry[]> {
    return Promise.resolve(this.ApplyQueryOptions(filterEntries(entries, query), options));
  }

  private ApplyQueryOptions(entries: IEntry[], options: QueryOptions | undefined): IEntry[] {
    if (!options) return entries;
    let sortWs = options.order.writingSystem;
    if (sortWs === 'default') sortWs = writingSystems.vernacular[0].id;

    return entries
      .sort((e1, e2) => {
        const v1 = firstVal(e1.citationForm) ?? firstVal(e1.lexemeForm);
        const v2 = firstVal(e2.citationForm) ?? firstVal(e2.lexemeForm);
        if (!v2) return -1;
        if (!v1) return 1;
        let compare = v1.localeCompare(v2, sortWs);
        if (compare !== 0) return compare;
        return e1.id.localeCompare(e2.id);
      })
      .slice(options.offset, options.offset + options.count);
  }

  GetEntriesForExemplar(exemplar: string, options: QueryOptions | undefined): Promise<IEntry[]> {
    const lowerExemplar = exemplar.toLowerCase();
    return Promise.resolve(this.ApplyQueryOptions(entries.filter(entry =>
      (firstVal(entry.citationForm) ?? firstVal(entry.lexemeForm))
        ?.toLocaleLowerCase()
        ?.startsWith(lowerExemplar)),
      options));
  }

  GetEntry(guid: string): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  CreateEntry(entry: IEntry): Promise<IEntry> {
    entries.push(entry);
    return Promise.resolve(entry);
  }

  UpdateEntry(guid: string, update: JsonPatch): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  CreateSense(entryGuid: string, sense: ISense): Promise<ISense> {
    entries.find(e => e.id === entryGuid)?.senses.push(sense);
    return Promise.resolve(sense);
  }

  UpdateSense(entryGuid: string, senseGuid: string, update: JsonPatch): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  CreateExampleSentence(entryGuid: string, senseGuid: string, exampleSentence: IExampleSentence): Promise<IExampleSentence> {
    entries.find(e => e.id === entryGuid)?.senses.find(s => s.id === senseGuid)?.exampleSentences.push(exampleSentence);
    return Promise.resolve(exampleSentence);
  }

  UpdateExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string, update: JsonPatch): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  DeleteEntry(guid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  DeleteSense(entryGuid: string, senseGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  DeleteExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  CreateWritingSystem(type: WritingSystemType, writingSystem: WritingSystem): Promise<void> {
    throw new Error('Method not implemented.');
  }

  UpdateWritingSystem(wsId: string, type: WritingSystemType, update: JsonPatch): Promise<WritingSystem> {
    throw new Error('Method not implemented.');
  }
}
