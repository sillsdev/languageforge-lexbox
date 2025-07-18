import papi, { logger } from '@papi/backend';
import type { IEntry, IProjectModel, IWritingSystems } from 'fw-lite-extension';

/** Returns text that can be used in JSON.parse() */
async function fetchUrl(url: string) {
  logger.info(`About to fetch: ${url}`);
  const results = await papi.fetch(url);
  if (!results.ok) {
    throw new Error(`Failed to fetch: ${results.statusText}`);
  }
  return await results.text();
}

export class FwLiteApi {
  private readonly baseUrl: string;
  private dictionaryCode?: string;
  constructor(baseUrl: string, dictionaryCode?: string) {
    this.baseUrl = baseUrl;
    this.setDictionaryCode(dictionaryCode);
  }

  setDictionaryCode(dictionaryCode?: string) {
    this.dictionaryCode = dictionaryCode;
  }

  private getUrl(path: string) {
    return `${this.baseUrl}/api/${path}`;
  }

  private async fetchPath(path: string) {
    return await fetchUrl(this.getUrl(path));
  }

  private checkDictionaryCode(code?: string): string {
    code = code || this.dictionaryCode;
    if (!code?.trim()) {
      throw new Error('FieldWorks dictionary code not specified');
    }
    if (code.includes('/')) {
      throw new Error(`Invalid FieldWorks dictionary code: ${code}`);
    }
    return code;
  }

  async fetchEntries(search: string, dictionaryCode?: string): Promise<IEntry[]> {
    const path = `mini-lcm/FwData/${this.checkDictionaryCode(dictionaryCode)}/entries/${search}`;
    return JSON.parse(await this.fetchPath(path));
  }

  async fetchProjects(): Promise<IProjectModel[]> {
    return JSON.parse(await this.fetchPath('localProjects'));
  }

  async fetchWritingSystems(dictionaryCode?: string): Promise<IWritingSystems> {
    const path = `mini-lcm/FwData/${this.checkDictionaryCode(dictionaryCode)}/writingSystems`;
    return JSON.parse(await this.fetchPath(path));
  }
}
