import papi, { logger } from '@papi/backend';
import type { IEntry, IProjectModel, IWritingSystems } from 'fw-lite-extension';

/** Returns text that can be used in JSON.parse() */
async function fetchUrl(input: RequestInfo | URL, init?: RequestInit) {
  logger.info(`About to fetch: ${input}`);
  if (init) {
    logger.info(JSON.stringify(init));
  }
  const results = await papi.fetch(input, init);
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

  private async fetchPath(path: string, postBody?: any) {
    return await fetchUrl(
      this.getUrl(path),
      postBody
        ? {
            body: JSON.stringify(postBody),
            headers: { 'Content-Type': 'application/json' },
            method: 'POST',
          }
        : undefined,
    );
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

  async getEntries(search: string, dictionaryCode?: string): Promise<IEntry[]> {
    const path = `mini-lcm/FwData/${this.checkDictionaryCode(dictionaryCode)}/entries/${search}`;
    return JSON.parse(await this.fetchPath(path));
  }

  async getProjects(): Promise<IProjectModel[]> {
    return JSON.parse(await this.fetchPath('localProjects'));
  }

  async getWritingSystems(dictionaryCode?: string): Promise<IWritingSystems> {
    const path = `mini-lcm/FwData/${this.checkDictionaryCode(dictionaryCode)}/writingSystems`;
    return JSON.parse(await this.fetchPath(path));
  }

  async postNewEntry(entry: IEntry, dictionaryCode?: string): Promise<void> {
    // TODO: This path is for CRDT projects. The API endpoint for adding an entry to a FLEx project
    // doesn't exist yet. Use that when it does.
    const path = `test/${this.checkDictionaryCode(dictionaryCode)}/add-new-entry`;
    this.fetchPath(path, entry);
  }
}
