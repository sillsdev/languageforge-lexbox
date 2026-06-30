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
    throw new Error(`Failed to fetch: ${results.status} ${results.statusText}`);
  }
  const text = await results.text();
  // eslint-disable-next-line no-type-assertion/no-type-assertion
  return text ? (JSON.parse(text) as unknown) : undefined;
}

export function getBrowseUrl(baseUrl: string, lexiconCode: string, entryId?: string): string {
  let url = `${baseUrl}/paratext/fwdata/${sanitizeUrlComponent(lexiconCode)}`;
  if (entryId) url += `/browse?entryId=${validateUrlComponent(entryId)}&entryOpen=true`;
  return url;
}

export class FwLiteApi {
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
    const { code, type } = this.checkLexiconCode(lexiconCode);
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
    const { code, type } = this.checkLexiconCode(lexiconCode);
    let path = `mini-lcm/${type}/${code}/entries`;
    if (search) path += `/${search}`;
    if (semanticDomain) {
      const filterValue = `senses.semanticDomains.code${GridifyConditionalOperator.Equal}${semanticDomain}`;
      path += `?GridifyFilter=${encodeURIComponent(filterValue)}`;
    }
    return (await this.fetchPath(path)) as IEntry[];
  }

  async getEntry(id: string, lexiconCode?: string): Promise<IEntry> {
    const { code, type } = this.checkLexiconCode(lexiconCode);
    const path = `mini-lcm/${type}/${code}/entry/${id}`;
    return (await this.fetchPath(path)) as IEntry;
  }

  async getSense(id: string, lexiconCode?: string): Promise<ISense> {
    const { code, type } = this.checkLexiconCode(lexiconCode);
    const path = `mini-lcm/${type}/${code}/sense/${id}`;
    return (await this.fetchPath(path)) as ISense;
  }

  async getProjects(): Promise<IProjectModel[]> {
    return (await this.fetchPath('localProjects')) as IProjectModel[];
  }

  async getProjectsMatchingLanguage(langTag?: string): Promise<IProjectModel[]> {
    const projects = (await this.fetchPath('localProjects')) as IProjectModel[];
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
    const { code, type } = this.checkLexiconCode(lexiconCode);
    const path = `mini-lcm/${type}/${code}/writingSystems`;
    return (await this.fetchPath(path)) as IWritingSystems;
  }

  async postNewEntry(entry: PartialEntry, lexiconCode?: string): Promise<IEntry> {
    const { code, type } = this.checkLexiconCode(lexiconCode);
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

  async createProject(
    name: string,
    code: string,
    vernacularWs: string,
    analysisWs?: string,
  ): Promise<void> {
    const params = new URLSearchParams({ name, code, vernacularWs });
    if (analysisWs) params.append('analysisWs', analysisWs);
    await this.fetchPath(`project/create?${params.toString()}`, 'POST');
  }

  private checkLexiconCode(lexiconCode?: string): LexiconRef {
    const code = sanitizeUrlComponent(lexiconCode || this.lexiconCode);
    return { code, type: 'FwData' };
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
