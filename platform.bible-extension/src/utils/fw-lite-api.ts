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
import { GridifyConditionalOperator } from '../types/enums';

// Local aliases for the FW Lite backend's generated API types (type-only via @dotnet-types).
export type LexboxServer = ILexboxServer;
export type AuthServerStatus = IServerStatus;

/** The generated `LoginResult` enum as a string union, which keeps the import type-only. */
export type LoginResult = `${GeneratedLoginResult}`;

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

async function fetchUrl(input: string, init?: RequestInit): Promise<unknown> {
  logger.info(`About to fetch: ${input}`);
  if (init) {
    logger.info(JSON.stringify(init));
  }
  const results = await papi.fetch(input, init);
  if (!results.ok) {
    const errorBody = await results.text();
    throw new Error(errorBody || `Failed to fetch: ${results.status} ${results.statusText}`);
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
    const vernLangTags = Object.keys(writingSystems.vernacular).map((v) => v.toLocaleLowerCase());
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

  async getProjectsMatchingLanguage(langTag?: string): Promise<IProjectModel[]> {
    const projects = await this.getProjects();
    if (!langTag?.trim()) return projects;

    try {
      const matches = (
        await Promise.all(
          projects.map(async (p) =>
            (await this.doesProjectMatchLangTag(p.code, langTag)) ? p : undefined,
          ),
        )
      ).filter((p) => p) as IProjectModel[];
      return matches.length ? matches : projects;
    } catch {
      return projects;
    }
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
   * restart, so on a miss we repopulate it from the backend; otherwise a previously created
   * Harmony/CRDT project would be misrouted to the FwData endpoints and every operation on it would
   * fail. Falls back to 'FwData' if the type still can't be determined.
   */
  private async resolveProjectType(code: string): Promise<'FwData' | 'Harmony'> {
    const cached = FwLiteApi.projectTypeByCode.get(code);
    if (cached) return cached;
    try {
      await this.getProjects();
    } catch (e) {
      logger.warn(
        'Could not load project types; defaulting to FwData:',
        e instanceof Error ? e.message : String(e),
      );
    }
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
