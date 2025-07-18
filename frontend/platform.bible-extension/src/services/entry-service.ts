import papi, { logger } from '@papi/backend';
import type { IEntry, IEntryQuery, IEntryService } from 'fw-lite-extension';
import { FwLiteApi } from '../utils/fw-lite-api';

export class EntryService implements IEntryService {
  private fwLiteApi: FwLiteApi;
  constructor(baseUrl: string, dictionaryCode?: string) {
    this.fwLiteApi = new FwLiteApi(baseUrl, dictionaryCode);
  }

  async getEntries(projectId: string, query: IEntryQuery): Promise<IEntry[] | undefined> {
    if (!projectId) {
      logger.debug('No project!');
      return;
    }
    if (!query.surfaceForm) {
      logger.debug('No query!');
      return;
    }

    const settings = await papi.projectDataProviders.get('platform.base', projectId);
    const dictionaryCode = await settings.getSetting('fw-lite-extension.fwDictionaryCode');
    console.log(`About to fetch entries for '${query.surfaceForm}' in '${dictionaryCode}'`);
    return this.fwLiteApi.getEntries(query.surfaceForm, dictionaryCode);
  }
  async addEntry(projectId: string, entry: IEntry): Promise<void> {
    if (!projectId) {
      logger.debug('No project!');
      return;
    }

    const settings = await papi.projectDataProviders.get('platform.base', projectId);
    const dictionaryCode = await settings.getSetting('fw-lite-extension.fwDictionaryCode');
    await this.fwLiteApi.postNewEntry(entry, dictionaryCode);
  }
  updateEntry(projectId: string, reference: IEntry): Promise<void> {
    throw new Error('Method not implemented.');
  }
  deleteEntry(projectId: string, id: string): Promise<void> {
    throw new Error('Method not implemented.');
  }
}
