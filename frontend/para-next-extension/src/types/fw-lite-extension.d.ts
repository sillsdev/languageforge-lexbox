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
}

declare module 'papi-shared-types' {

  export interface CommandHandlers {
    'fwLiteExtension.doStuff': (message: string) => {
      response: string;
      occurrence: number;
    };
    'fwLiteExtension.findEntry': (entry: string) => {
      success: boolean;
    };
    'fwLiteExtension.simpleFind': () => {
      success: boolean;
    };
  }
}
