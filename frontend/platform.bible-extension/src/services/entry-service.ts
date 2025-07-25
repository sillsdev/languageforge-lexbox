import { logger } from '@papi/backend';
import type { IEntry, IEntryQuery, IEntryService } from 'fw-lite-extension';
import { FwLiteApi } from '../utils/fw-lite-api';
import { ProjectManager } from '../utils/project-managers';

export class EntryService implements IEntryService {
  private fwLiteApi: FwLiteApi;
  constructor(baseUrl: string, dictionaryCode?: string) {
    this.fwLiteApi = new FwLiteApi(baseUrl, dictionaryCode);
  }

  async getEntries(projectId: string, query: IEntryQuery): Promise<IEntry[] | undefined> {
    if (!query.surfaceForm) {
      logger.debug('No query!');
      return;
    }
    const dictionaryCode = await ProjectManager.getFwDictionaryCode(projectId);
    if (!dictionaryCode) return;
    console.log(`About to fetch entries for '${query.surfaceForm}' in '${dictionaryCode}'`);
    return this.fwLiteApi.getEntries(query.surfaceForm, dictionaryCode);
  }

  async addEntry(projectId: string, entry: Partial<IEntry>): Promise<IEntry | undefined> {
    const dictionaryCode = await ProjectManager.getFwDictionaryCode(projectId);
    if (!dictionaryCode) return;
    return await this.fwLiteApi.postNewEntry(entry, dictionaryCode);
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  updateEntry(projectId: string, reference: IEntry): Promise<void> {
    throw new Error('Method not implemented.');
  }

  async deleteEntry(projectId: string, id: string): Promise<undefined> {
    const dictionaryCode = await ProjectManager.getFwDictionaryCode(projectId);
    if (!dictionaryCode) return;
    await this.fwLiteApi.deleteEntry(id, dictionaryCode);
  }
}
