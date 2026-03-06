import { logger } from '@papi/backend';
import type { IEntry, IEntryQuery, IEntryService, ISense, PartialEntry } from 'lexicon';
import { FwLiteApi } from '../utils/fw-lite-api';
import { ProjectManager } from '../utils/project-manager';

export class EntryService implements IEntryService {
  private fwLiteApi: FwLiteApi;
  // TODO: remove the need to specify baseUrl, so other extensions can use this network service.
  constructor(baseUrl: string, lexiconCode?: string) {
    this.fwLiteApi = new FwLiteApi(baseUrl, lexiconCode);
  }

  async getEntries(projectId: string, query: IEntryQuery): Promise<IEntry[] | undefined> {
    const { semanticDomain, surfaceForm } = query;
    if (!semanticDomain && !surfaceForm) {
      logger.debug('No query!');
      return;
    }
    const lexiconCode = await ProjectManager.getLexiconCode(projectId);
    if (!lexiconCode) return;
    logger.info(
      `Fetching entries for '${surfaceForm}' (semantic domain '${semanticDomain}') in '${lexiconCode}'`,
    );
    return this.fwLiteApi.getEntries(surfaceForm, semanticDomain, lexiconCode);
  }

  async getEntry(projectId: string, id: string): Promise<IEntry | undefined> {
    const lexiconCode = await ProjectManager.getLexiconCode(projectId);
    if (!lexiconCode) return;
    return this.fwLiteApi.getEntry(id, lexiconCode);
  }

  async getSense(projectId: string, id: string): Promise<ISense | undefined> {
    const lexiconCode = await ProjectManager.getLexiconCode(projectId);
    if (!lexiconCode) return;
    return this.fwLiteApi.getSense(id, lexiconCode);
  }

  async addEntry(projectId: string, entry: PartialEntry): Promise<IEntry | undefined> {
    const lexiconCode = await ProjectManager.getLexiconCode(projectId);
    if (!lexiconCode) return;
    return await this.fwLiteApi.postNewEntry(entry, lexiconCode);
  }

  // eslint-disable-next-line @typescript-eslint/class-methods-use-this, @typescript-eslint/no-unused-vars
  updateEntry(_projectId: string, _reference: IEntry): Promise<void> {
    throw new Error('Method not implemented.');
  }

  async deleteEntry(projectId: string, id: string): Promise<undefined> {
    const lexiconCode = await ProjectManager.getLexiconCode(projectId);
    if (!lexiconCode) return;
    await this.fwLiteApi.deleteEntry(id, lexiconCode);
  }
}
