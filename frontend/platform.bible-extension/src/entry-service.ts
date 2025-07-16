import papi, { logger } from '@papi/backend';
import { IEntry, IEntryQuery, IEntryService } from 'fw-lite-extension';

export class EntryService implements IEntryService {
  readonly baseUrl: string;
  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }
  async getEntries(projectId: string, query: IEntryQuery): Promise<IEntry[] | undefined> {
    logger.debug('Opening find UI');

    if (!projectId) {
      logger.debug('No project!');
      return undefined;
    }
    const settings = await papi.projectDataProviders.get('platform.base', projectId);
    const fieldWorksProject = await settings.getSetting('fw-lite-extension.fwProject');
    const apiUrl = `${this.baseUrl}/api/mini-lcm/FwData/${fieldWorksProject}/entries/${query.surfaceForm}`;
    console.log(`About to fetch entries: ${apiUrl}`);
    // Construct the query parameters from the IEntryQuery object
    // parse the json from the results and return the entries
    const results = await papi.fetch(apiUrl);
    if (!results.ok) {
      throw new Error(`Failed to fetch entries: ${results.statusText}`);
    }
    // parse the json from the results
    const jsonText = await results.text();
    const entries = JSON.parse(jsonText) as IEntry[];
    return entries;
  }
  addEntry(projectId: string, reference: IEntry): Promise<void> {
    throw new Error('Method not implemented.');
  }
  updateEntry(projectId: string, reference: IEntry): Promise<void> {
    throw new Error('Method not implemented.');
  }
  deleteEntry(projectId: string, id: string): Promise<void> {
    throw new Error('Method not implemented.');
  }
}
