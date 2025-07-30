import type { OpenWebViewOptions } from '@papi/core';
import type { IEntryService, IProjectModel, SuccessHolder } from 'fw-lite-extension';

declare module 'fw-lite-extension' {
  /* eslint-disable @typescript-eslint/consistent-type-imports */
  export type IEntry = import('../../../viewer/src/lib/dotnet-types/index.js').IEntry;
  export type IMultiString = import('../../../viewer/src/lib/dotnet-types/index.js').IMultiString;
  export type IPartOfSpeech = import('../../../viewer/src/lib/dotnet-types/index.js').IPartOfSpeech;
  export type IProjectModel = import('../../../viewer/src/lib/dotnet-types/index.js').IProjectModel;
  export type ISense = import('../../../viewer/src/lib/dotnet-types/index.js').ISense;
  export type ISemanticDomain =
    import('../../../viewer/src/lib/dotnet-types/index.js').ISemanticDomain;
  export type IWritingSystems =
    import('../../../viewer/src/lib/dotnet-types/index.js').IWritingSystems;

  export type ProjectSettingKey = import('./enums.ts').ProjectSettingKey;
  export type WebViewType = import('./enums.ts').WebViewType;
  /* eslint-enable @typescript-eslint/consistent-type-imports */

  export interface DictionaryRef {
    code: string;
    type: 'FwData' | 'Harmony';
  }

  export type WebViewIds = {
    [webViewKey in WebViewType]?: string;
  };

  export interface SuccessHolder {
    success: boolean;
  }

  export interface IEntryQuery {
    readonly surfaceForm?: string;
    readonly exactMatch?: boolean;
    readonly partOfSpeech?: string;
    readonly semanticDomain?: string;
  }

  export interface IEntryService {
    getEntries(projectId: string, query: IEntryQuery): Promise<IEntry[] | undefined>;
    addEntry(projectId: string, reference: Partial<IEntry>): Promise<IEntry | undefined>;
    updateEntry(projectId: string, reference: IEntry): Promise<void>;
    deleteEntry(projectId: string, id: string): Promise<void>;
  }

  interface OpenWebViewOptionsWithProjectId extends OpenWebViewOptions {
    projectId?: string;
  }

  interface BrowseWebViewOptions extends OpenWebViewOptionsWithProjectId {
    url?: string;
  }

  interface WordWebViewOptions extends OpenWebViewOptionsWithProjectId {
    word?: string;
  }
}

declare module 'papi-shared-types' {
  export interface CommandHandlers {
    'fwLiteExtension.addEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'fwLiteExtension.browseDictionary': (webViewId: string) => Promise<SuccessHolder>;
    'fwLiteExtension.displayEntry': (projectId: string, entryId: string) => Promise<SuccessHolder>;
    'fwLiteExtension.selectDictionary': (
      projectId: string,
      dictionaryCode: string,
    ) => Promise<SuccessHolder>;
    'fwLiteExtension.fwDictionaries': (projectId?: string) => Promise<IProjectModel[] | undefined>;
    'fwLiteExtension.openFWLite': () => Promise<SuccessHolder>; // Remove before publishing.
    'fwLiteExtension.findEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'fwLiteExtension.findRelatedEntries': (
      webViewId: string,
      entry: string,
    ) => Promise<SuccessHolder>;
    'fwLiteExtension.getBaseUrl': () => string;
  }

  export interface ProjectSettingTypes {
    'fw-lite-extension.fwDictionaryCode': string;
  }

  export interface NetworkableObject {
    'fwLiteExtension.entryService': IEntryService;
  }
}
