import papi, { logger } from '@papi/backend';
import type {
  DictionaryRef,
  IEntry,
  IProjectModel,
  IWritingSystems,
  PartialEntry,
} from 'fw-lite-extension';
import { GridifyConditionalOperator } from '../types/enums';

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

export function getBrowseUrl(baseUrl: string, dictionaryCode: string, entryId?: string): string {
  let url = `${baseUrl}/paratext/fwdata/${sanitizeUrlComponent(dictionaryCode)}`;
  if (entryId) url += `/browse?entryId=${validateUrlComponent(entryId)}`;
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

  async deleteEntry(id: string, dictionaryCode?: string): Promise<void> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
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
    dictionaryCode?: string,
  ): Promise<IEntry[]> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    let path = `mini-lcm/${type}/${code}/entries`;
    if (search) path += `/${search}`;
    if (semanticDomain) {
      const filterValue = `senses.semanticDomains.code${GridifyConditionalOperator.Equal}${semanticDomain}`;
      path += `?GridifyFilter=${encodeURIComponent(filterValue)}`;
    }
    return (await this.fetchPath(path)) as IEntry[];
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

  async getWritingSystems(dictionaryCode?: string): Promise<IWritingSystems> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/writingSystems`;
    return (await this.fetchPath(path)) as IWritingSystems;
  }

  async postNewEntry(entry: PartialEntry, dictionaryCode?: string): Promise<IEntry> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/entry`;
    return (await this.fetchPath(path, 'POST', entry)) as IEntry;
  }

  /* eslint-enable no-type-assertion/no-type-assertion */

  private checkDictionaryCode(dictionaryCode?: string): DictionaryRef {
    const code = sanitizeUrlComponent(dictionaryCode || this.dictionaryCode);
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
