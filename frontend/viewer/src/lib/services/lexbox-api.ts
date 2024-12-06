/* eslint-disable */

import type {ComplexFormType, IEntry, IExampleSentence, ISense, PartOfSpeech, QueryOptions, SemanticDomain, WritingSystem, WritingSystems} from '../mini-lcm';

import type {Operation} from 'fast-json-patch';
import type {Readable} from 'svelte/store';

export type {ComplexFormType, IEntry, IExampleSentence, ISense, PartOfSpeech, QueryOptions, SemanticDomain, WritingSystem, WritingSystems} from '../mini-lcm';

export type JsonPatch = Operation[];

export enum WritingSystemType {
  Vernacular = 0,
  Analysis = 1,
}

export interface LexboxApiFeatures {
  history?: boolean;
  write?: boolean;
  openWithFlex?: boolean;
  feedback?: boolean;
  sync?: boolean;
  about?: Readable<string>;
}

export interface LexboxApi {
  GetWritingSystems(): Promise<WritingSystems>;
  CreateWritingSystem(type: WritingSystemType, writingSystem: WritingSystem): Promise<void>;
  UpdateWritingSystem(wsId: string, type: WritingSystemType, update: JsonPatch): Promise<WritingSystem>;

  GetPartsOfSpeech(): Promise<PartOfSpeech[]>;
  GetSemanticDomains(): Promise<SemanticDomain[]>;
  GetComplexFormTypes(): Promise<ComplexFormType[]>;

  GetEntries(options: QueryOptions | undefined): Promise<IEntry[]>;
  SearchEntries(query: string, options: QueryOptions | undefined): Promise<IEntry[]>;
  GetEntry(guid: string): Promise<IEntry>;

  CreateEntry(entry: IEntry): Promise<IEntry>;
  UpdateEntry(before: IEntry, after: IEntry): Promise<IEntry>;
  DeleteEntry(guid: string): Promise<void>;

  CreateSense(entryGuid: string, sense: ISense): Promise<ISense>;
  UpdateSense(entryGuid: string, senseGuid: string, update: JsonPatch): Promise<ISense>;
  DeleteSense(entryGuid: string, senseGuid: string): Promise<void>;

  CreateExampleSentence(entryGuid: string, senseGuid: string, exampleSentence: IExampleSentence): Promise<IExampleSentence>;
  UpdateExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string, update: JsonPatch): Promise<IExampleSentence>;
  DeleteExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string): Promise<void>;
}

export interface LexboxApiMetadata {
  SupportedFeatures(): Promise<LexboxApiFeatures>;
}

export type LexboxApiClient = LexboxApi & LexboxApiMetadata;
