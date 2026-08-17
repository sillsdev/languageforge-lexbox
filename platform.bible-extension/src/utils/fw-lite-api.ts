import papi, { logger } from '@papi/backend';
import type {
  IEntry,
  IProjectModel,
  ISense,
  IWritingSystems,
  LexiconRef,
  PartialEntry,
} from 'lexicon';
import type {
  ILexboxServer,
  IServerStatus,
  LoginResult as GeneratedLoginResult,
} from '@dotnet-types';
import { getErrorMessage } from 'platform-bible-utils';
import { GridifyConditionalOperator } from '../types/enums';
import { HttpStatusError } from './http-status-error';

// Local aliases for the FW Lite backend's generated API types (type-only via @dotnet-types).
export type LexboxServer = ILexboxServer;
export type AuthServerStatus = IServerStatus;

/** The generated `LoginResult` enum as a string union, which keeps the import type-only. */
export type LoginResult = `${GeneratedLoginResult}`;

/**
 * Outcome of downloading a remote project. The REST route flattens the backend's
 * DownloadProjectByCodeResult to HTTP status codes, so we map those back to a union here rather
 * than reusing the generated enum. 'NotFound'/'Forbidden' can't happen for a project we just
 * listed, but a project's access can change between listing and download, so they're handled.
 */
export type DownloadResult = 'Success' | 'AlreadyDownloaded' | 'Forbidden' | 'NotFound' | 'Error';

/** Throws if urlComponent is empty; otherwise, returns it encoded. */
function sanitizeUrlComponent(urlComponent?: string): string {
  if (!urlComponent) throw new Error(`Empty URL component`);
  return encodeURIComponent(urlComponent);
}

/** Throws if urlComponent is empty or has any special URL characters it; otherwise, returns it. */
function validateUrlComponent(urlComponent?: string): string {
  const sanitizedComponent = sanitizeUrlComponent(urlComponent);
  if (urlComponent !== sanitizedComponent) throw new Error(`Invalid URL component`);
  return urlComponent;
}

/**
 * Backend error bodies are JSON: either a bare string or a `ProblemDetails` object with a `detail`
 * field. Unwrap either shape to plain text so it doesn't reach the user still wrapped in
 * JSON-string quotes; anything else, including invalid JSON, is returned as-is.
 */
function extractErrorMessage(body: string): string {
  if (!body) return body;
  try {
    const parsed: unknown = JSON.parse(body);
    if (typeof parsed === 'string') return parsed;
    if (
      parsed &&
      typeof parsed === 'object' &&
      'detail' in parsed &&
      typeof parsed.detail === 'string'
    ) {
      return parsed.detail;
    }
  } catch {
    // Not JSON — fall through and use the raw text.
  }
  return body;
}

async function fetchUrl(input: string, init?: RequestInit): Promise<unknown> {
  logger.info(`About to fetch: ${input}`);
  if (init) {
    logger.info(JSON.stringify(init));
  }
  const results = await papi.fetch(input, init);
  if (!results.ok) {
    const errorBody = await results.text();
    throw new HttpStatusError(
      results.status,
      extractErrorMessage(errorBody) || `Failed to fetch: ${results.status} ${results.statusText}`,
    );
  }
  const text = await results.text();
  // eslint-disable-next-line no-type-assertion/no-type-assertion
  return text ? (JSON.parse(text) as unknown) : undefined;
}

export class FwLiteApi {
  // Shared across all instances (EntryService, main) — all talk to the same backend process.
  private static readonly projectTypeByCode = new Map<string, 'FwData' | 'Harmony'>();

  private readonly baseUrl: string;
  private lexiconCode?: string;
  constructor(baseUrl: string, lexiconCode?: string) {
    this.baseUrl = baseUrl;
    this.setLexiconCode(lexiconCode);
  }

  setLexiconCode(lexiconCode?: string): void {
    this.lexiconCode = lexiconCode;
  }

  async deleteEntry(id: string, lexiconCode?: string): Promise<void> {
    const { code, type } = await this.checkLexiconCode(lexiconCode);
    const path = `mini-lcm/${type}/${code}/entry/${id}`;
    await this.fetchPath(path, 'DELETE');
  }

  async doesProjectMatchLangTag(code: string, langTag: string): Promise<boolean> {
    const tag = langTag.trim().toLocaleLowerCase().split('-')[0];
    if (!code || !tag) return false;
    const writingSystems = await this.getWritingSystems(code);
    const vernLangTags = writingSystems.vernacular.map((ws) => ws.wsId.toLocaleLowerCase());
    return vernLangTags.some((v) => v === tag || v.startsWith(`${tag}-`));
  }

  /* eslint-disable no-type-assertion/no-type-assertion */

  async getEntries(
    search?: string,
    semanticDomain?: string,
    lexiconCode?: string,
  ): Promise<IEntry[]> {
    const { code, type } = await this.checkLexiconCode(lexiconCode);
    let path = `mini-lcm/${type}/${code}/entries`;
    if (search) path += `/${search}`;
    if (semanticDomain) {
      const filterValue = `senses.semanticDomains.code${GridifyConditionalOperator.Equal}${semanticDomain}`;
      path += `?GridifyFilter=${encodeURIComponent(filterValue)}`;
    }
    return (await this.fetchPath(path)) as IEntry[];
  }

  async getEntry(id: string, lexiconCode?: string): Promise<IEntry> {
    const { code, type } = await this.checkLexiconCode(lexiconCode);
    const path = `mini-lcm/${type}/${code}/entry/${id}`;
    return (await this.fetchPath(path)) as IEntry;
  }

  async getSense(id: string, lexiconCode?: string): Promise<ISense> {
    const { code, type } = await this.checkLexiconCode(lexiconCode);
    const path = `mini-lcm/${type}/${code}/sense/${id}`;
    return (await this.fetchPath(path)) as ISense;
  }

  async getProjects(): Promise<IProjectModel[]> {
    const projects = (await this.fetchPath('localProjects')) as IProjectModel[];
    projects.forEach((p) => FwLiteApi.projectTypeByCode.set(p.code, p.crdt ? 'Harmony' : 'FwData'));
    return projects;
  }

  /**
   * Local projects, preferring those whose vernacular matches `langTag`. `filtered` is true only
   * when a real subset was returned — when nothing (or everything) matches, all projects come back
   * and there's nothing for a "show all" affordance to reveal. `noMatch` is true in the specific
   * case where a language was given but no project matched it (so the caller can explain why the
   * list isn't filtered, rather than silently showing everything). `keepCodes` are always retained
   * even when their language doesn't match — the current lexicon plus any the user applied earlier
   * this session, so a just-replaced non-matching lexicon doesn't vanish from the list.
   */
  async getProjectsMatchingLanguage(
    langTag?: string,
    keepCodes?: string[],
  ): Promise<{ projects: IProjectModel[]; filtered: boolean; noMatch: boolean }> {
    const projects = await this.getProjects();
    if (!langTag?.trim()) return { projects, filtered: false, noMatch: false };

    // Promise.allSettled so one project's failed check (e.g. a broken/deleted project) only
    // drops that project from consideration.
    const results = await Promise.allSettled(
      projects.map(async (p) =>
        (await this.doesProjectMatchLangTag(p.code, langTag)) ? p : undefined,
      ),
    );
    results.forEach((result, i) => {
      if (result.status === 'rejected') {
        logger.warn(
          `Could not check language match for project '${projects[i].code}':`,
          getErrorMessage(result.reason),
        );
      }
    });
    const matches = results
      .filter(
        (result): result is PromiseFulfilledResult<IProjectModel | undefined> =>
          result.status === 'fulfilled',
      )
      .map((result) => result.value)
      .filter((p): p is IProjectModel => Boolean(p));
    if (!matches.length) return { projects, filtered: false, noMatch: true };

    // Retain the kept lexicons regardless of language, keeping original order and no duplicate.
    const keep = new Set(matches.map((p) => p.code));
    keepCodes?.forEach((c) => keep.add(c));
    const kept = projects.filter((p) => keep.has(p.code));
    if (kept.length === projects.length) return { projects, filtered: false, noMatch: false };
    return { projects: kept, filtered: true, noMatch: false };
  }

  async getWritingSystems(lexiconCode?: string): Promise<IWritingSystems> {
    const { code, type } = await this.checkLexiconCode(lexiconCode);
    const path = `mini-lcm/${type}/${code}/writingSystems`;
    return (await this.fetchPath(path)) as IWritingSystems;
  }

  async postNewEntry(entry: PartialEntry, lexiconCode?: string): Promise<IEntry> {
    const { code, type } = await this.checkLexiconCode(lexiconCode);
    const path = `mini-lcm/${type}/${code}/entry`;
    return (await this.fetchPath(path, 'POST', entry)) as IEntry;
  }

  async getAuthServers(): Promise<AuthServerStatus[]> {
    return (await this.fetchPath('auth/servers')) as AuthServerStatus[];
  }

  /**
   * Remote (Lexbox server) projects the signed-in user can download, across all configured servers,
   * flattened into one list (each carries its own `server`). Servers the user isn't signed into
   * return nothing. Filtered to CRDT projects — the only ones FW Lite can download; the `crdt` flag
   * is the backend's "has Harmony commits" signal.
   */
  async getRemoteProjects(): Promise<IProjectModel[]> {
    const byServer = (await this.fetchPath('remoteProjects')) as Record<string, IProjectModel[]>;
    return Object.values(byServer)
      .flat()
      .filter((p) => p.crdt);
  }

  /**
   * Downloads a remote CRDT project, blocking until its initial sync completes (can take minutes).
   * Pass `signal` to abandon the wait; the backend leaves the request running until the sync
   * resolves regardless. Distinguishes the terminal outcomes the download route can return.
   */
  async downloadProject(
    authority: string,
    code: string,
    signal?: AbortSignal,
  ): Promise<DownloadResult> {
    const path = `download/crdt/${sanitizeUrlComponent(authority)}/${sanitizeUrlComponent(code)}`;
    const response = await papi.fetch(this.getUrl(path), { method: 'POST', signal });
    switch (response.status) {
      case 200:
        FwLiteApi.projectTypeByCode.set(code, 'Harmony');
        return 'Success';
      case 204:
        return 'AlreadyDownloaded';
      case 403:
        return 'Forbidden';
      case 404:
        return 'NotFound';
      default:
        return 'Error';
    }
  }

  /** Deletes a local CRDT project. Callers must ensure it's re-downloadable (synced to a server). */
  async deleteProject(code: string): Promise<void> {
    await this.fetchPath(`crdt/${sanitizeUrlComponent(code)}`, 'DELETE');
    FwLiteApi.projectTypeByCode.delete(code);
  }

  /**
   * Triggers a system-browser sign-in. Doesn't resolve until the user finishes in their browser,
   * cancels, or MSAL gives up. Pass `signal` to abort an abandoned sign-in, which the backend
   * otherwise leaves pending indefinitely.
   */
  async login(authority: string, signal?: AbortSignal): Promise<LoginResult> {
    const path = `auth/login-web-view/${sanitizeUrlComponent(authority)}`;
    return (await fetchUrl(this.getUrl(path), { signal })) as LoginResult;
  }

  async logout(authority: string): Promise<void> {
    const path = `auth/logout/${sanitizeUrlComponent(authority)}`;
    // The endpoint redirects to the web-app root, so fetchPath would choke parsing HTML as JSON.
    const results = await papi.fetch(this.getUrl(path));
    if (!results.ok) throw new Error(`Failed to fetch: ${results.status} ${results.statusText}`);
  }

  /* eslint-enable no-type-assertion/no-type-assertion */

  async getBrowseUrl(lexiconCode: string, entryId?: string): Promise<string> {
    const type = await this.resolveProjectType(lexiconCode);
    const segment = type === 'Harmony' ? 'project' : 'fwdata';
    let url = `${this.baseUrl}/paratext/${segment}/${sanitizeUrlComponent(lexiconCode)}`;
    if (entryId) url += `/browse?entryId=${validateUrlComponent(entryId)}&entryOpen=true`;
    return url;
  }

  async createProject(
    name: string,
    code: string,
    vernacularWs: string,
    analysisWs?: string,
  ): Promise<void> {
    const params = new URLSearchParams({ name, code, vernacularWs });
    if (analysisWs) params.append('analysisWs', analysisWs);
    await this.fetchPath(`project/create?${params.toString()}`, 'POST');
    FwLiteApi.projectTypeByCode.set(code, 'Harmony');
  }

  private async checkLexiconCode(lexiconCode?: string): Promise<LexiconRef> {
    const rawCode = lexiconCode || this.lexiconCode;
    const code = sanitizeUrlComponent(rawCode);
    const type = await this.resolveProjectType(rawCode ?? '');
    return { code, type };
  }

  /**
   * Looks up a project's API type. The cache is in-memory only and empty after an extension
   * restart, so on a miss we repopulate it from the backend; else a Harmony/CRDT project could be
   * misrouted to the FwData endpoints and every operation on it would fail. `getProjects` has no
   * per-code failure mode (it just enumerates), so if it throws, the type genuinely can't be
   * determined; propagate the failure instead of guessing 'FwData' and misrouting a Harmony
   * project. Only defaults to 'FwData' when the backend answered but the code is unrecognized.
   */
  private async resolveProjectType(code: string): Promise<'FwData' | 'Harmony'> {
    const cached = FwLiteApi.projectTypeByCode.get(code);
    if (cached) return cached;
    await this.getProjects();
    return FwLiteApi.projectTypeByCode.get(code) ?? 'FwData';
  }

  private getUrl(path: string): string {
    return `${this.baseUrl}/api/${path}`;
  }

  private async fetchPath(path: string, method?: string, postBody?: unknown): Promise<unknown> {
    return await fetchUrl(
      this.getUrl(path),
      // eslint-disable-next-line no-nested-ternary
      postBody
        ? {
            body: JSON.stringify(postBody),
            headers: { 'Content-Type': 'application/json' },
            method: method || 'POST',
          }
        : method
          ? { method }
          : undefined,
    );
  }
}
