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
   */
  export interface IEntryService {
    /**
     * @param query - Ignored unless it narrows by surface form or semantic domain; a query that
     *   narrows by neither matches nothing rather than everything.
     * @returns The matching entries, or `undefined` when the lexicon cannot be read or the query
     *   narrows by nothing.
     */
    getEntries(lexiconCode: string, query: IEntryQuery): Promise<IEntry[] | undefined>;
    /** @returns The entry, or `undefined` when the lexicon has no such entry. */
    getEntry(lexiconCode: string, id: string): Promise<IEntry | undefined>;
    /** @returns The sense, or `undefined` when the lexicon has no such sense. */
    getSense(lexiconCode: string, id: string): Promise<ISense | undefined>;
    /**
     * Adds an entry to the lexicon.
     *
     * @returns The created entry, carrying the ids the lexicon minted for it, or `undefined` when
     *   the lexicon cannot be written to.
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

  /**
   * What a command named by {@link LexiconOptions.resultCommand} is handed and must answer: the
   * project and the lexicon the user chose or created for it, and whether the caller recorded the
   * link, so a caller that could not record it says so instead of leaving the selector reporting
   * success.
   *
   * Deliberately the signature of `lexicon.selectLexicon`, the command it stands in for, so a
   * caller records the link where that one would have written this extension's project setting.
   */
  export type LexiconResultCommand = (
    projectId: string,
    lexiconCode: string,
  ) => Promise<SuccessHolder>;

  /** Additions for options/props of WebViews that interact with a lexicon via the FwLiteApi. */
  interface LexiconOptions extends Partial<LexiconLanguages> {
    lexiconCode?: string;
    /**
     * Names the command the lexicon selector reports its result to, in place of recording the
     * selection in `lexicon.lexiconCode` itself. For a caller that keeps the project-to-lexicon
     * link somewhere this extension does not own; when absent, the selector records the selection
     * as it does for this extension's own commands. Must name a command matching
     * {@link LexiconResultCommand}.
     */
    resultCommand?: string;
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
    /**
     * Opens the lexicon selector for a project and reports what the user chose or created to
     * `resultCommand` instead of recording it in `lexicon.lexiconCode`. For a caller that keeps the
     * project-to-lexicon link somewhere this extension does not own.
     *
     * @param projectId - Project whose vernacular language seeds the create form; the selection is
     *   not recorded against it.
     * @param resultCommand - Name of a registered command taking the project and the chosen
     *   lexicon's code and answering whether the link was recorded (a `LexiconResultCommand`). It
     *   is called once, on choose-or-create; a user who dismisses the selector without choosing
     *   leaves it uncalled, so a caller cannot treat it as an answer bound to arrive.
     * @returns Whether the selector opened, which is not whether a lexicon was chosen.
     */
    'lexicon.chooseLexicon': (projectId: string, resultCommand: string) => Promise<SuccessHolder>;
    'lexicon.createLexicon': (
      name: string,
      code: string,
      vernacularWs: string,
      analysisWs?: string,
    ) => Promise<SuccessHolder>;
    'lexicon.displayEntry': (projectId: string, entryId: string) => Promise<SuccessHolder>;
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
