import papi, { logger } from '@papi/backend';
import { IEntry, IEntryQuery, IEntryService } from 'fw-lite-extension';

export class EntryService implements IEntryService {
  readonly baseUrl: string;
  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }
  async getEntries(webViewId: string, query: IEntryQuery): Promise<IEntry[] | undefined> {
    let projectId: string | undefined;
    let tabIdFromWebViewId: string | undefined;

    logger.debug('Opening find UI');

    if (webViewId) {
      const webViewDefinition = await papi.webViews.getOpenWebViewDefinition(webViewId);
      projectId = webViewDefinition?.projectId;
      tabIdFromWebViewId = webViewDefinition?.id;
    }

    if (!projectId) {
      logger.debug('No project!');
      return undefined;
    }
    // Construct the query parameters from the IEntryQuery object
    // parse the json from the results and return the entries
    const results = await papi.fetch(`${this.baseUrl}/api/mini-lcm/FwData/${projectId}/Entries`);
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
