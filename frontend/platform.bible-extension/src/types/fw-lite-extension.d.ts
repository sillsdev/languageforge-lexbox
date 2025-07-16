import { IEntryService } from 'fw-lite-extension';

declare module 'fw-lite-extension' {
  export type IEntry = import('../../../viewer/src/lib/dotnet-types/index.js').IEntry;
  export type IProjectModel = import('../../../viewer/src/lib/dotnet-types/index.js').IProjectModel;

  export interface SuccessHolder {
    success: boolean;
  }
  export interface UrlHolder {
    baseUrl: string;
    dictionaryUrl: string;
  }

  export type OpenFwLiteEvent = {
    dictionaryCode: string;
  };
  export type FindEntryEvent = {
    /** The entry to find */
    entry: string;
  };
  export type LaunchServerEvent = {
    baseUrl: string;
  };

  export type FwDictionariesEvent = {
    dictionaries: IProjectModel[];
  };
  export type OpenProjectEvent = {
    projectCode: string;
  };

  export interface IEntryQuery {
    readonly projectId: string;
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
}

declare module 'papi-shared-types' {
  export interface CommandHandlers {
    'fwLiteExtension.browseDictionary': (webViewId: string) => Promise<SuccessHolder>;
    'fwLiteExtension.selectDictionary': (
      projectId: string,
      dictionaryCode: string,
    ) => Promise<SuccessHolder>;
    'fwLiteExtension.fwDictionaries': () => Promise<void>;
    'fwLiteExtension.openFWLite': (webViewId: string) => SuccessHolder;
    'fwLiteExtension.findEntry': (webViewId: string, entry: string) => SuccessHolder;
    'fwLiteExtension.simpleFind': () => SuccessHolder;
    'fwLiteExtension.getBaseUrl': () => UrlHolder;
  }
  export interface ProjectSettingTypes {
    'fw-lite-extension.fwProject': string;
  }
  export interface NetworkableObject {
    'fwLiteExtension.entryService': IEntryService;
  }
}
