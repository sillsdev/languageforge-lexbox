import { logger } from '@papi/backend';
import type { IEntry, IEntryQuery, IEntryService, ISense, PartialEntry } from 'lexicon';
import { FwLiteApi } from '../utils/fw-lite-api';

export class EntryService implements IEntryService {
  private fwLiteApi: FwLiteApi;
  constructor(baseUrl: string) {
    this.fwLiteApi = new FwLiteApi(baseUrl);
  }

  async getEntries(lexiconCode: string, query: IEntryQuery): Promise<IEntry[] | undefined> {
    const { semanticDomain, surfaceForm } = query;
    if (!semanticDomain && !surfaceForm) {
      logger.debug('No query!');
      return;
    }
    if (!lexiconCode) return;
    logger.info(
      `Fetching entries for '${surfaceForm}' (semantic domain '${semanticDomain}') in '${lexiconCode}'`,
    );
    return this.fwLiteApi.getEntries(surfaceForm, semanticDomain, lexiconCode);
  }

  async getEntry(lexiconCode: string, id: string): Promise<IEntry | undefined> {
    if (!lexiconCode) return;
    return this.fwLiteApi.getEntry(id, lexiconCode);
  }

  async getSense(lexiconCode: string, id: string): Promise<ISense | undefined> {
    if (!lexiconCode) return;
    return this.fwLiteApi.getSense(id, lexiconCode);
  }

  async addEntry(lexiconCode: string, entry: PartialEntry): Promise<IEntry | undefined> {
    if (!lexiconCode) return;
    return await this.fwLiteApi.postNewEntry(entry, lexiconCode);
  }

  // eslint-disable-next-line @typescript-eslint/class-methods-use-this, @typescript-eslint/no-unused-vars
  updateEntry(_lexiconCode: string, _entry: IEntry): Promise<void> {
    throw new Error('Method not implemented.');
  }

  async deleteEntry(lexiconCode: string, id: string): Promise<undefined> {
    if (!lexiconCode) return;
    await this.fwLiteApi.deleteEntry(id, lexiconCode);
  }
}
