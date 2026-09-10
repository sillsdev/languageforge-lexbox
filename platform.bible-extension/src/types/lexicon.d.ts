import type { OpenWebViewOptions, WebViewProps } from '@papi/core';
import type { IEntryService, IProjectModel, SuccessHolder } from 'lexicon';
import type { AuthServerStatus, LoginResult } from '../utils/fw-lite-api';

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
   * Every method names the lexicon it acts on by its FW Lite lexicon code, so a caller that records
   * which lexicon it is using reaches that lexicon and no other. Mapping a Paratext project to a
   * lexicon is the caller's own business; this service holds no notion of a project.
   *
   * Absence resolves and faults reject, for every method that answers with a record. A lexicon that
   * is not there, and a record it does not hold, both answer `undefined` — so a caller reading a
   * record it recorded a reference to earlier handles a lexicon that has since been deleted the
   * same way it handles a deleted record, without a `catch`. A rejection means the answer is
   * unknown rather than "no": the backend is unreachable, or it answered with a fault.
   *
   * `deleteEntry` is outside that rule and rejects for a lexicon that is not there, since it has no
   * value to answer absence with. Whether a delete against a vanished lexicon should instead read
   * as done is open.
   *
   * A record `id` is one the lexicon minted, a GUID. An id in any other shape names no record the
   * lexicon could hold, so it reads as absence rather than as a fault.
   */
  export interface IEntryService {
    /**
     * @param query - Ignored unless it narrows by surface form or semantic domain; a query that
     *   narrows by neither matches nothing rather than everything.
     * @returns The matching entries, or `undefined` when the query narrows by nothing or the
     *   lexicon is not there. Empty when the lexicon holds no match.
     */
    getEntries(lexiconCode: string, query: IEntryQuery): Promise<IEntry[] | undefined>;
    /** @returns The entry, or `undefined` when neither it nor its lexicon is there. */
    getEntry(lexiconCode: string, id: string): Promise<IEntry | undefined>;
    /** @returns The sense, or `undefined` when neither it nor its lexicon is there. */
    getSense(lexiconCode: string, id: string): Promise<ISense | undefined>;
    /**
     * Adds an entry to the lexicon.
     *
     * @returns The created entry, carrying the ids the lexicon minted for it, or `undefined` when
     *   the lexicon is not there. Rejects when the entry was refused.
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
     * Opens the browse view on one entry of the lexicon named, rather than of whichever lexicon the
     * project's setting holds by then — so an entry just written to one lexicon is never shown from
     * another. `projectId` scopes the browse tab, which a later entry of the same project reuses.
     */
    'lexicon.displayEntry': (
      projectId: string,
      lexiconCode: string,
      entryId: string,
    ) => Promise<SuccessHolder>;
    'lexicon.findEntry': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'lexicon.findRelatedEntries': (webViewId: string, entry: string) => Promise<SuccessHolder>;
    'lexicon.lexicons': (projectId?: string) => Promise<IProjectModel[] | undefined>;
    'lexicon.login': (
      authority: string,
    ) => Promise<{ result?: LoginResult; servers?: AuthServerStatus[] }>;
    'lexicon.logout': (authority: string) => Promise<AuthServerStatus[] | undefined>;
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
