import type { OpenWebViewOptions, WebViewProps } from '@papi/core';
import type { IEntryService, IProjectModel, SuccessHolder } from 'lexicon';
import type { AuthServerStatus, DownloadResult, LoginResult } from '../utils/fw-lite-api';

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

  export interface LexiconRef {
    code: string;
    type: 'FwData' | 'Harmony';
  }

  export type WebViewIds = {
    [webViewKey in WebViewType]?: string;
  };

  export interface SuccessHolder {
    success: boolean;
    /** When `success` is false, a human-readable reason (e.g. a backend validation message). */
    error?: string;
  }

  export interface IEntryQuery {
    readonly surfaceForm?: string;
    readonly exactMatch?: boolean;
    readonly partOfSpeech?: string;
    readonly semanticDomain?: string;
  }

  export interface IEntryService {
    getEntries(projectId: string, query: IEntryQuery): Promise<IEntry[] | undefined>;
    getEntry(projectId: string, id: string): Promise<IEntry | undefined>;
    getSense(projectId: string, id: string): Promise<ISense | undefined>;
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
    /** The Paratext project's short name, for showing which project the view is scoped to. */
    projectName?: string;
    word?: string;
  }

  /** Options for WebViews that interact with a lexicon via the FwLiteApi. */
  export interface LexiconWebViewOptions extends ProjectWebViewOptions, LexiconOptions {}

  /** Props for WebViews that interact with a lexicon via the FwLiteApi. */
  export type LexiconWebViewProps = ProjectWebViewProps & LexiconOptions;

  /* eslint-enable @typescript-eslint/no-shadow */
}

declare module 'papi-shared-types' {
  export interface CommandHandlers {
    'lexicon.addEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'lexicon.authServers': () => Promise<AuthServerStatus[] | undefined>;
    'lexicon.browseLexicon': (webViewId: string) => Promise<SuccessHolder>;
    /** DEV-ONLY lexicon switcher; remove before release (see src/main.ts changeLexiconCommand). */
    'lexicon.changeLexicon': (webViewId: string) => Promise<SuccessHolder>;
    'lexicon.createLexicon': (
      name: string,
      code: string,
      vernacularWs: string,
      analysisWs?: string,
    ) => Promise<SuccessHolder>;
    /**
     * Deletes a CRDT lexicon on this computer — a downloaded copy, or a local-only one (permanent,
     * since it isn't re-downloadable). Refuses FwData projects, which FieldWorks manages.
     */
    'lexicon.deleteDownloadedLexicon': (lexiconCode: string) => Promise<SuccessHolder>;
    'lexicon.displayEntry': (projectId: string, entryId: string) => Promise<SuccessHolder>;
    'lexicon.findEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'lexicon.findRelatedEntries': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    /**
     * Local lexicons, filtered to the project's language when a real subset matches. `filtered`
     * reports whether that happened; `noMatch` is true when a language matched nothing; `langTag`
     * is that language either way. `all` skips the filter. `keepCodes` are codes to keep regardless
     * of language (applied this session), on top of the project's current lexicon.
     */
    'lexicon.lexicons': (
      projectId?: string,
      all?: boolean,
      keepCodes?: string[],
    ) => Promise<
      | { projects: IProjectModel[]; filtered: boolean; noMatch: boolean; langTag?: string }
      | undefined
    >;
    'lexicon.login': (
      authority: string,
    ) => Promise<{ result?: LoginResult; servers?: AuthServerStatus[] }>;
    'lexicon.logout': (authority: string) => Promise<AuthServerStatus[] | undefined>;
    /** Remote (Lexbox server) CRDT projects the signed-in user can download. */
    'lexicon.remoteProjects': () => Promise<IProjectModel[] | undefined>;
    /**
     * Downloads a remote project (its promise resolves only once the initial sync finishes) and, on
     * success, selects it for the Paratext project. `success` is true only when both steps
     * completed; when false, `result` says why the download ended (or is a success value if the
     * download was fine but selection failed).
     */
    'lexicon.downloadAndSelectLexicon': (
      projectId: string,
      authority: string,
      lexiconCode: string,
    ) => Promise<{ result: DownloadResult; success: boolean }>;
    /**
     * Resolves the Paratext project a WebView is scoped to, prompting with the core project picker
     * when it has none (e.g. a selector tab restored from a saved layout); `projectId` is undefined
     * if the user dismisses. Kept separate from the acting commands so their timeouts don't tick
     * while the picker waits. `projectName` is the resolved project's short name.
     */
    'lexicon.resolveProject': (
      webViewId: string,
    ) => Promise<{ projectId?: string; projectName?: string }>;
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
