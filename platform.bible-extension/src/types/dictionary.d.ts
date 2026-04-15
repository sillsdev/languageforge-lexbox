import type { OpenWebViewOptions, WebViewProps } from '@papi/core';
import type { IEntryService, IProjectModel, SuccessHolder } from 'dictionary';

// TODO: Sort out internal types and those that need to be exposed for other extensions.

declare module 'dictionary' {
  /* eslint-disable @typescript-eslint/no-shadow */

  export type IEntry = import('@dotnet-types').IEntry;
  export type IMultiString = import('@dotnet-types').IMultiString;
  export type IPartOfSpeech = import('@dotnet-types').IPartOfSpeech;
  export type IProjectModel = import('@dotnet-types').IProjectModel;
  export type ISense = import('@dotnet-types').ISense;
  export type ISemanticDomain = import('@dotnet-types').ISemanticDomain;
  export type IWritingSystems = import('@dotnet-types').IWritingSystems;

  export type ProjectSettingKey = import('./enums.ts').ProjectSettingKey;
  export type WebViewType = import('./enums.ts').WebViewType;

  export type PartialEntry = Omit<Partial<IEntry>, 'senses'> & {
    senses?: Partial<ISense>[];
  };

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
    addEntry(projectId: string, reference: PartialEntry): Promise<IEntry | undefined>;
    updateEntry(projectId: string, reference: IEntry): Promise<void>;
    deleteEntry(projectId: string, id: string): Promise<void>;
  }

  /** Additions for options/props of project-specific WebViews. */
  interface ProjectOptions {
    projectId?: string;
  }

  /** Base extension of OpenWebViewOptions for all project-specific WebViews. */
  export interface ProjectWebViewOptions extends OpenWebViewOptions, ProjectOptions {}

  /** Base extension of WebViewProps for all project-specific WebViews. */
  type ProjectWebViewProps = WebViewProps & ProjectOptions;

  /** Additions for options/props of WebViews that browse FW Lite. */
  interface BrowseOptions {
    url?: string;
  }

  /** Options for WebViews that browse FW Lite. */
  export interface BrowseWebViewOptions extends ProjectWebViewOptions, BrowseOptions {}

  /** Props for WebViews that browse FW Lite. */
  export type BrowseWebViewProps = ProjectWebViewProps & BrowseOptions;

  export interface DictionaryLanguages {
    analysisLanguage: string;
    vernacularLanguage: string;
  }

  /** Additions for options/props of WebViews that interact with a dictionary via the FwLiteApi. */
  interface DictionaryOptions extends Partial<DictionaryLanguages> {
    dictionaryCode?: string;
    word?: string;
  }

  /** Options for WebViews that interact with a dictionary via the FwLiteApi. */
  export interface DictionaryWebViewOptions extends ProjectWebViewOptions, DictionaryOptions {}

  /** Props for WebViews that interact with a dictionary via the FwLiteApi. */
  export type DictionaryWebViewProps = ProjectWebViewProps & DictionaryOptions;

  /* eslint-enable @typescript-eslint/no-shadow */
}

declare module 'papi-shared-types' {
  export interface CommandHandlers {
    'dictionary.addEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'dictionary.browseDictionary': (webViewId: string) => Promise<SuccessHolder>;
    'dictionary.displayEntry': (projectId: string, entryId: string) => Promise<SuccessHolder>;
    'dictionary.selectDictionary': (
      projectId: string,
      dictionaryCode: string,
    ) => Promise<SuccessHolder>;
    'dictionary.dictionaries': (projectId?: string) => Promise<IProjectModel[] | undefined>;
    'dictionary.findEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'dictionary.findRelatedEntries': (
      webViewId: string,
      entry: string,
    ) => Promise<SuccessHolder>;
  }

  export interface ProjectSettingTypes {
    'dictionary.analysisLanguage': string;
    'dictionary.dictionaryCode': string;
  }

  export interface NetworkableObject {
    'dictionary.entryService': IEntryService;
  }
}
