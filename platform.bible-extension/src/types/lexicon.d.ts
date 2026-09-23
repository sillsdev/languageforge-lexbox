import type { OpenWebViewOptions, WebViewProps } from '@papi/core';
import type { IEntryService, IProjectModel, SuccessHolder } from 'lexicon';
import type {
  AuthServerStatus,
  DownloadAndSelectResult,
  LocalLexiconsResult,
  LoginResult,
} from '../utils/fw-lite-api';

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

  /**
   * Reads and writes the lexical data of one lexicon at a time.
   *
   * Record-returning methods resolve `undefined` when the lexicon or record is missing and reject
   * only for backend faults or other unknown failures. `deleteEntry` is the exception: a missing
   * lexicon rejects because there is no meaningful absence value to return.
   *
   * IDs are GUIDs minted by the lexicon; any other shape is treated as not found rather than as a
   * fault.
   */
  export interface IEntryService {
    /**
     * @param query - Ignored unless it narrows by surface form or semantic domain.
     * @returns Matching entries, or `undefined` when the query narrows by nothing or the lexicon is
     *   missing. Empty when the lexicon holds no match.
     */
    getEntries(lexiconCode: string, query: IEntryQuery): Promise<IEntry[] | undefined>;
    getEntry(lexiconCode: string, id: string): Promise<IEntry | undefined>;
    getSense(lexiconCode: string, id: string): Promise<ISense | undefined>;
    /**
     * @returns The created entry, including any IDs minted by the lexicon, or `undefined` when the
     *   lexicon is missing. Rejects when the backend refuses the entry.
     */
    addEntry(lexiconCode: string, entry: PartialEntry): Promise<IEntry | undefined>;
    updateEntry(lexiconCode: string, entry: IEntry): Promise<void>;
    deleteEntry(lexiconCode: string, id: string): Promise<void>;
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
     * Deletes any local CRDT lexicon (downloaded or local-only). Refuses FwData projects. Clears
     * `projectId`'s lexicon setting when it pointed at the deleted lexicon.
     */
    'lexicon.deleteDownloadedLexicon': (
      lexiconCode: string,
      projectId?: string,
    ) => Promise<SuccessHolder>;
    /**
     * Opens the browse view on one entry of the lexicon named, rather than of whichever lexicon the
     * project's setting holds by then — so an entry just written to one lexicon is never shown from
     * another. `projectId` scopes the browse tab, which a later entry of the same project reuses.
     */
    'lexicon.displayEntry': (
      projectId: string,
      lexiconCode: string,
      entryId: string,
    ) => Promise<SuccessHolder>;
    /**
     * Downloads a remote project (resolves once its initial sync finishes) and selects it for the
     * given project.
     */
    'lexicon.downloadAndSelectLexicon': (
      projectId: string,
      authority: string,
      lexiconCode: string,
    ) => Promise<DownloadAndSelectResult>;
    'lexicon.findEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'lexicon.findRelatedEntries': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    /**
     * Local lexicons for the project the web view is bound to (resolved from its definition, never
     * prompting), filtered to that project's language when a real subset matches. `result.filtered`
     * reports whether that happened; `result.noMatch` is true when a language matched nothing.
     * `all` skips the filter. `keepCodes` are codes to keep regardless of language (applied this
     * session), on top of the project's current lexicon.
     */
    'lexicon.lexicons': (
      webViewId: string,
      all?: boolean,
      keepCodes?: string[],
    ) => Promise<LocalLexiconsResult | undefined>;
    'lexicon.login': (
      authority: string,
    ) => Promise<{ result?: LoginResult; servers?: AuthServerStatus[] }>;
    'lexicon.logout': (authority: string) => Promise<AuthServerStatus[] | undefined>;
    /**
     * Opens the lexicon selector on a project, leaving to the caller whether reopening it on an
     * already-linked project is appropriate.
     *
     * @returns Whether the selector opened, which is not whether a lexicon was chosen; a chosen
     *   lexicon lands in the project's `lexicon.lexiconCode` setting.
     */
    'lexicon.openSelector': (projectId: string) => Promise<SuccessHolder>;
    /** Remote (Lexbox server) CRDT projects the signed-in user can download. */
    'lexicon.remoteProjects': () => Promise<IProjectModel[] | undefined>;
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
