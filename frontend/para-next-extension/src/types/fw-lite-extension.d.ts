declare module 'fw-lite-extension' {
  import { type DataProviderDataType, type IDataProvider } from '@papi/core';

  export type ExtensionVerseSetData = string | { text: string; isHeresy: boolean };

  export type ExtensionVerseDataTypes = {
    Verse: DataProviderDataType<string, string | undefined, ExtensionVerseSetData>;
    Heresy: DataProviderDataType<string, string | undefined, string>;
    Chapter: DataProviderDataType<[book: string, chapter: number], string | undefined, never>;
  };

  /** Network event that informs subscribers when the command `fwLiteExtension.doStuff` is run */
  export type DoStuffEvent = {
    /** How many times the extension template has run the command `fwLiteExtension.doStuff` */
    count: number;
  };

  export type ExtensionVerseDataProvider = IDataProvider<ExtensionVerseDataTypes>;
}

declare module 'papi-shared-types' {
  import type { ExtensionVerseDataProvider } from 'fw-lite-extension';

  export interface CommandHandlers {
    'fwLiteExtension.doStuff': (message: string) => {
      response: string;
      occurrence: number;
    };
  }

  export interface DataProviders {
    'paranextExtensionTemplate.quickVerse': ExtensionVerseDataProvider;
  }
}
