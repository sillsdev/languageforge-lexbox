import papi, { logger } from '@papi/backend';
import type { IEntry, IProjectModel } from 'fw-lite-extension';

export class MiniLcmApi {
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

  /** Returns text that can be used in JSON.parse() */
  private async fetch(path: string) {
    const apiUrl = this.getUrl(path);
    logger.info(`About to fetch: ${apiUrl}`);
    const results = await papi.fetch(apiUrl);
    if (!results.ok) {
      throw new Error(`Failed to fetch: ${results.statusText}`);
    }
    return await results.text();
  }

  async fetchEntries(search: string, dictionaryCode?: string) {
    const code = dictionaryCode || this.dictionaryCode;
    if (!code) {
      throw new Error(`FW dictionary code not specified`);
    }
    const path = `mini-lcm/FwData/${dictionaryCode || this.dictionaryCode}/entries/${search}`;
    const jsonText = await this.fetch(path);
    return JSON.parse(jsonText) as IEntry[];
  }

  async fetchProjects() {
    const jsonText = await this.fetch('localProjects');
    return JSON.parse(jsonText) as IProjectModel[];
  }
}
