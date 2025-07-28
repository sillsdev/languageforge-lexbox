import papi, { logger } from '@papi/backend';
import type { DictionaryRef, IEntry, IProjectModel, IWritingSystems } from 'fw-lite-extension';

/** Throws if urlPart is empty or has a / in it; returns otherwise. */
function validateUrlPart(urlPart?: string): string {
  if (!urlPart?.trim() || urlPart.includes('/')) throw new Error(`Invalid url part: '${urlPart}'`);
  return urlPart;
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

export function getBrowseUrl(baseUrl: string, dictionaryCode: string, entryId?: string): string {
  let url = `${baseUrl}/paratext/fwdata/${validateUrlPart(dictionaryCode)}`;
  if (entryId) url += `/browse?entryId=${validateUrlPart(entryId)}`;
  return url;
}

export class FwLiteApi {
  private readonly baseUrl: string;
  private dictionaryCode?: string;
  constructor(baseUrl: string, dictionaryCode?: string) {
    this.baseUrl = baseUrl;
    this.setDictionaryCode(dictionaryCode);
  }

  setDictionaryCode(dictionaryCode?: string): void {
    this.dictionaryCode = dictionaryCode;
  }

  private getUrl(path: string): string {
    return `${this.baseUrl}/api/${path}`;
  }

  private async fetchPath(path: string, method?: string, postBody?: unknown): Promise<unknown> {
    return await fetchUrl(
      this.getUrl(path),
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

  private checkDictionaryCode(code?: string): DictionaryRef {
    code = validateUrlPart(code || this.dictionaryCode);
    return { code, type: 'FwData' };
  }

  async deleteEntry(id: string, dictionaryCode?: string): Promise<void> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/entry/${id}`;
    await this.fetchPath(path, 'DELETE');
  }

  async doesProjectMatchLanguage(code: string, language: string): Promise<boolean> {
    language = language.trim().toLocaleLowerCase();
    if (!code || !language) return false;
    const writingSystems = await this.getWritingSystems(code);
    return JSON.stringify(writingSystems.vernacular).toLocaleLowerCase().includes(language);
  }

  async getEntries(search: string, dictionaryCode?: string): Promise<IEntry[]> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/entries/${search}`;
    return (await this.fetchPath(path)) as IEntry[];
  }

  async getEntriesInSemanticDomain(domainId: string, dictionaryCode?: string): Promise<IEntry[]> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/entries-in-domain/${domainId}`;
    logger.error(`Not yet implemented: ${path}`);
    return [];
  }

  async getProjects(): Promise<IProjectModel[]> {
    return (await this.fetchPath('localProjects')) as IProjectModel[];
  }

  async getProjectsMatchingLanguage(language?: string): Promise<IProjectModel[]> {
    const projects = (await this.fetchPath('localProjects')) as IProjectModel[];
    if (!language?.trim()) return projects;

    const matches = (
      await Promise.all(
        projects.map(async (p) =>
          (await this.doesProjectMatchLanguage(p.code, language)) ? p : null,
        ),
      )
    ).filter((p) => p) as IProjectModel[];
    return matches.length ? matches : projects;
  }

  async getWritingSystems(dictionaryCode?: string): Promise<IWritingSystems> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/writingSystems`;
    return (await this.fetchPath(path)) as IWritingSystems;
  }

  async postNewEntry(entry: Partial<IEntry>, dictionaryCode?: string): Promise<IEntry> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/entry`;
    return (await this.fetchPath(path, 'POST', entry)) as IEntry;
  }
}
