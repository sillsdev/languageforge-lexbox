import type { OpenWebViewOptions, WebViewProps } from '@papi/core';
import type { IEntryService, IProjectModel, SuccessHolder } from 'lexicon';

// TODO: Sort out internal types and those that need to be exposed for other extensions.

declare module 'lexicon' {
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

  export interface LexiconLanguages {
    analysisLanguage: string;
    vernacularLanguage: string;
  }

  /** Additions for options/props of WebViews that interact with a lexicon via the FwLiteApi. */
  interface LexiconOptions extends Partial<LexiconLanguages> {
    lexiconCode?: string;
    word?: string;
  }

  /** Options for WebViews that interact with a lexicon via the FwLiteApi. */
  export interface DictionaryWebViewOptions extends ProjectWebViewOptions, LexiconOptions {}

  /** Props for WebViews that interact with a lexicon via the FwLiteApi. */
  export type DictionaryWebViewProps = ProjectWebViewProps & LexiconOptions;

  /* eslint-enable @typescript-eslint/no-shadow */
}

declare module 'papi-shared-types' {
  export interface CommandHandlers {
    'lexicon.addEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'lexicon.browseLexicon': (webViewId: string) => Promise<SuccessHolder>;
    'lexicon.displayEntry': (projectId: string, entryId: string) => Promise<SuccessHolder>;
    'lexicon.findEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'lexicon.findRelatedEntries': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'lexicon.lexicons': (projectId?: string) => Promise<IProjectModel[] | undefined>;
    'lexicon.selectLexicon': (projectId: string, lexiconCode: string) => Promise<SuccessHolder>;
  }

  export interface ProjectSettingTypes {
    'lexicon.analysisLanguage': string;
    'lexicon.lexiconCode': string;
  }

  export interface NetworkableObject {
    'lexicon.entryService': IEntryService;
  }
}
