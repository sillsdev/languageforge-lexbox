import { OpenWebViewOptions } from '@papi/core';
import { IEntryService, IProjectModel, SuccessHolder, UrlHolder } from 'fw-lite-extension';

declare module 'fw-lite-extension' {
  export type IEntry = import('../../../viewer/src/lib/dotnet-types/index.js').IEntry;
  export type IProjectModel = import('../../../viewer/src/lib/dotnet-types/index.js').IProjectModel;
  export type IWritingSystems =
    import('../../../viewer/src/lib/dotnet-types/index.js').IWritingSystems;

  export interface SuccessHolder {
    success: boolean;
  }
  export interface UrlHolder {
    baseUrl: string;
    dictionaryUrl: string;
  }

  export type FindEntryEvent = {
    entry: string;
  };

  export interface IEntryQuery {
    readonly surfaceForm?: string;
    readonly exactMatch?: boolean;
    readonly partOfSpeech?: string;
    readonly semanticDomain?: string;
  }

  export interface IEntryService {
    getEntries(projectId: string, query: IEntryQuery): Promise<IEntry[] | undefined>;
    addEntry(projectId: string, reference: IEntry): Promise<void>;
    updateEntry(projectId: string, reference: IEntry): Promise<void>;
    deleteEntry(projectId: string, id: string): Promise<void>;
  }

  interface OpenWebViewOptionsWithProjectId extends OpenWebViewOptions {
    projectId?: string;
  }
  interface FindWebViewOptions extends OpenWebViewOptionsWithProjectId {
    word?: string;
  }
}

declare module 'papi-shared-types' {
  export interface CommandHandlers {
    'fwLiteExtension.browseDictionary': (webViewId: string) => Promise<SuccessHolder>;
    'fwLiteExtension.selectDictionary': (
      projectId: string,
      dictionaryCode: string,
    ) => Promise<SuccessHolder>;
    'fwLiteExtension.fwDictionaries': () => Promise<IProjectModel[] | undefined>;
    'fwLiteExtension.openFWLite': (webViewId: string) => Promise<SuccessHolder>;
    'fwLiteExtension.findEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'fwLiteExtension.getBaseUrl': () => UrlHolder;
  }
  export interface ProjectSettingTypes {
    'fw-lite-extension.fwDictionaryCode': string;
  }
  export interface NetworkableObject {
    'fwLiteExtension.entryService': IEntryService;
  }
}
