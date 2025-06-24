declare module 'fw-lite-extension' {
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
    'fwLiteExtension.openFWLite': () => {
      success: boolean;
    };
    'fwLiteExtension.findEntry': (entry: string) => {
      success: boolean;
    };
    'fwLiteExtension.simpleFind': () => {
      success: boolean;
    };
    'fwLiteExtension.getBaseUrl': () => {
      baseUrl: string;
    };
  }
}
