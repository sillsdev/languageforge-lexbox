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

// The auth types below alias the FW Lite backend's generated types (imported type-only via the
// tsconfig `@dotnet-types` path) under extension-local names, so they can't drift from the C#
// models the REST API actually returns.

/** A Lexbox server FW Lite can sign in to, as configured on the FW Lite backend. */
export type LexboxServer = ILexboxServer;

/** Sign-in status of a single {@link LexboxServer}, as returned by `GET /api/auth/servers`. */
export type AuthServerStatus = IServerStatus;

/**
 * Outcome of a system-browser login attempt. Derived from FwLiteShared's generated `LoginResult`
 * enum as a string union (rather than the enum itself) so outcomes compare against string literals
 * without pulling the enum's runtime code into a bundle — `@dotnet-types` is type-only here.
 */
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
    throw new Error(`Failed to fetch: ${results.statusText}`);
  }
  return await results.json();
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

  /** Lists configured Lexbox servers along with the current sign-in status of each. */
  async getAuthServers(): Promise<AuthServerStatus[]> {
    return (await this.fetchPath('auth/servers')) as AuthServerStatus[];
  }

  /**
   * Triggers a system-browser sign-in for the given server (see `AuthConfig.SystemWebViewLogin` on
   * the FW Lite backend). The request doesn't resolve until the user finishes in their browser,
   * cancels, or the backend gives up — there is currently no server-side timeout, so this can stay
   * pending indefinitely if the user abandons the browser tab.
   */
  async login(authority: string): Promise<LoginResult> {
    const path = `auth/login-web-view/${sanitizeUrlComponent(authority)}`;
    return (await this.fetchPath(path)) as LoginResult;
  }

  async logout(authority: string): Promise<void> {
    const path = `auth/logout/${sanitizeUrlComponent(authority)}`;
    // The logout endpoint redirects to the FW Lite web app root, so there's no JSON body to parse;
    // this relies on papi.fetch following that redirect by default (hence bypassing fetchPath, which
    // would try to parse the redirected HTML root page as JSON).
    const results = await papi.fetch(this.getUrl(path));
    if (!results.ok) throw new Error(`Failed to fetch: ${results.statusText}`);
  }

  /* eslint-enable no-type-assertion/no-type-assertion */

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
