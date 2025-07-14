import { IEntryService } from 'fw-lite-extension';

declare module 'fw-lite-extension' {
  export type IEntry = import('../../../viewer/src/lib/dotnet-types/index.js').IEntry;
  export type IProjectModel = import('../../../viewer/src/lib/dotnet-types/index.js').IProjectModel;

  /** Network event that informs subscribers when the command `fwLiteExtension.doStuff` is run */
  export type DoStuffEvent = {
    /** How many times the extension template has run the command `fwLiteExtension.doStuff` */
    count: number;
  };

  export type FindEntryEvent = {
    /** The entry to find */
    entry: string;
  };
  export type LaunchServerEvent = {
    baseUrl: string;
  };

  export type LocalProjectsEvent = {
    projects: IProjectModel[];
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
    'fwLiteExtension.localProjects': () => Promise<void>;
    'fwLiteExtension.openFWProjectSelector': () => Promise<{
      success: boolean;
    }>;
    'fwLiteExtension.openProject': (projectCode: string) => Promise<{
      success: boolean;
    }>;
    'fwLiteExtension.openFWLite': (webviewId: string) => {
      success: boolean;
    };
    'fwLiteExtension.findEntry': (
      webviewId: string,
      entry: string,
    ) => {
      success: boolean;
    };
    'fwLiteExtension.simpleFind': () => {
      success: boolean;
    };
    'fwLiteExtension.getBaseUrl': () => {
      baseUrl: string;
    };
  }
  export interface ProjectSettingTypes {
    'fw-lite-extension.fwProject': string;
  }
  export interface NetworkableObject {
    'fwLiteExtension.entryService': IEntryService;
  }
}
