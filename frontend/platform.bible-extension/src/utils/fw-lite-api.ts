import papi, { logger } from '@papi/backend';
import type { DicionaryRef, IEntry, IProjectModel, IWritingSystems } from 'fw-lite-extension';

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

  private async fetchPath(path: string, postBody?: unknown): Promise<unknown> {
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

  private checkDictionaryCode(code?: string): DicionaryRef {
    code = code || this.dictionaryCode;
    if (!code?.trim()) {
      throw new Error('FieldWorks dictionary code not specified');
    }
    if (code.includes('/')) {
      throw new Error(`Invalid FieldWorks dictionary code: ${code}`);
    }
    return { code, type: 'FwData' };
  }

  async getEntries(search: string, dictionaryCode?: string): Promise<IEntry[]> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/entries/${search}`;
    return (await this.fetchPath(path)) as IEntry[];
  }

  async getProjects(): Promise<IProjectModel[]> {
    return (await this.fetchPath('localProjects')) as IProjectModel[];
  }

  async getWritingSystems(dictionaryCode?: string): Promise<IWritingSystems> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/writingSystems`;
    return (await this.fetchPath(path)) as IWritingSystems;
  }

  async postNewEntry(entry: IEntry, dictionaryCode?: string): Promise<void> {
    const { code, type } = this.checkDictionaryCode(dictionaryCode);
    const path = `mini-lcm/${type}/${code}/entry`;
    await this.fetchPath(path, entry);
  }
}
