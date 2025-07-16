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
    entry: string;
  };
  export type FwDictionariesEvent = {
    dictionaries: IProjectModel[];
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
  import { IEntryService, IProjectModel, SuccessHolder, UrlHolder } from 'fw-lite-extension';

  export interface CommandHandlers {
    'fwLiteExtension.browseDictionary': (webViewId: string) => Promise<SuccessHolder>;
    'fwLiteExtension.selectDictionary': (
      projectId: string,
      dictionaryCode: string,
    ) => Promise<SuccessHolder>;
    'fwLiteExtension.fwDictionaries': () => Promise<IProjectModel[] | undefined>;
    'fwLiteExtension.openFWLite': (webViewId: string) => Promise<SuccessHolder>;
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
