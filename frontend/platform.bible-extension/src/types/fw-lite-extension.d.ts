declare module 'fw-lite-extension' {
  export type IEntry = import('../../../viewer/src/lib/dotnet-types/index.js').IEntry;
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
    projects: FwProject[];
  };

  export type FwProject = {
    name: string;
    code: string;
    crdt: boolean;
    fwdata: boolean;
    lexbox: boolean;
    role: number;
    id: string;
  };
}

declare module 'papi-shared-types' {
  export interface CommandHandlers {
    'fwLiteExtension.localProjects': () => Promise<void>;
    'fwLiteExtension.openFWLite': (webviewId: string) => {
      success: boolean;
    };
    'fwLiteExtension.findEntry': (entry: string) => {
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
