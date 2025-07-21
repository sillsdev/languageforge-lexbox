import papi, { logger } from '@papi/backend';
import type { IEntry, IProjectModel, IWritingSystems } from 'fw-lite-extension';

/** Returns text that can be used in JSON.parse() */
async function fetchUrl(input: string, init?: RequestInit): Promise<string> {
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

  setDictionaryCode(dictionaryCode?: string): void {
    this.dictionaryCode = dictionaryCode;
  }

  private getUrl(path: string): string {
    return `${this.baseUrl}/api/${path}`;
  }

  private async fetchPath(path: string, postBody?: any): Promise<string> {
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
    return JSON.parse(await this.fetchPath(path)) as IEntry[];
  }

  async getProjects(): Promise<IProjectModel[]> {
    return JSON.parse(await this.fetchPath('localProjects')) as IProjectModel[];
  }

  async getWritingSystems(dictionaryCode?: string): Promise<IWritingSystems> {
    const path = `mini-lcm/FwData/${this.checkDictionaryCode(dictionaryCode)}/writingSystems`;
    return JSON.parse(await this.fetchPath(path)) as IWritingSystems;
  }

  async postNewEntry(entry: IEntry, dictionaryCode?: string): Promise<void> {
    const path = `mini-lcm/FwData/${this.checkDictionaryCode(dictionaryCode)}/entry`;
    await this.fetchPath(path, entry);
  }
}
