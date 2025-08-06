import { logger } from '@papi/backend';
import type { IEntry, IEntryQuery, IEntryService, PartialEntry } from 'fw-lite-extension';
import { FwLiteApi } from '../utils/fw-lite-api';
import { ProjectManager } from '../utils/project-manager';

export class EntryService implements IEntryService {
  private fwLiteApi: FwLiteApi;
  constructor(baseUrl: string, dictionaryCode?: string) {
    this.fwLiteApi = new FwLiteApi(baseUrl, dictionaryCode);
  }

  async getEntries(projectId: string, query: IEntryQuery): Promise<IEntry[] | undefined> {
    const { semanticDomain, surfaceForm } = query;
    if (!semanticDomain && !surfaceForm) {
      logger.debug('No query!');
      return;
    }
    const dictionaryCode = await ProjectManager.getFwDictionaryCode(projectId);
    if (!dictionaryCode) return;
    logger.info(
      `Fetching entries for '${surfaceForm}' (semantic domain '${semanticDomain}') in '${dictionaryCode}'`,
    );
    return this.fwLiteApi.getEntries(surfaceForm, semanticDomain, dictionaryCode);
  }

  async addEntry(projectId: string, entry: PartialEntry): Promise<IEntry | undefined> {
    const dictionaryCode = await ProjectManager.getFwDictionaryCode(projectId);
    if (!dictionaryCode) return;
    return await this.fwLiteApi.postNewEntry(entry, dictionaryCode);
  }

  // eslint-disable-next-line @typescript-eslint/class-methods-use-this, @typescript-eslint/no-unused-vars
  updateEntry(projectId: string, reference: IEntry): Promise<void> {
    throw new Error('Method not implemented.');
  }

  async deleteEntry(projectId: string, id: string): Promise<undefined> {
    const dictionaryCode = await ProjectManager.getFwDictionaryCode(projectId);
    if (!dictionaryCode) return;
    await this.fwLiteApi.deleteEntry(id, dictionaryCode);
  }
}
