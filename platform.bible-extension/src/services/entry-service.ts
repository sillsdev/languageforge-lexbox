import { logger } from '@papi/backend';
import type { IEntry, IEntryQuery, IEntryService, ISense, PartialEntry } from 'lexicon';
import { FwLiteApi } from '../utils/fw-lite-api';
import { HttpStatusError } from '../utils/http-status-error';

/**
 * Runs one read or write against a lexicon and reports a lexicon that is not there as `undefined`,
 * which is what this service promises for it.
 *
 * Only a 404 is absence: the backend answers that for a lexicon code it does not hold. Anything
 * else is left to reject — a 400 means the request itself was malformed, a 500 or an unreachable
 * backend means the answer is unknown rather than "no", and swallowing either would report an empty
 * lexicon where there is a fault.
 *
 * The backend also answers a missing entry or sense with `null` rather than a 404, so a nullish
 * result becomes `undefined` too and callers can trust the one absent value the types declare.
 */
async function readingLexicon<T>(
  read: () => Promise<T | null | undefined>,
): Promise<T | undefined> {
  try {
    return (await read()) ?? undefined;
  } catch (e) {
    if (e instanceof HttpStatusError && e.status === 404) {
      logger.info('The lexicon is not there:', e.message);
      return undefined;
    }
    throw e;
  }
}

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
    return readingLexicon(() =>
      this.fwLiteApi.getEntries(surfaceForm, semanticDomain, lexiconCode),
    );
  }

  async getEntry(lexiconCode: string, id: string): Promise<IEntry | undefined> {
    if (!lexiconCode) return;
    return readingLexicon(() => this.fwLiteApi.getEntry(id, lexiconCode));
  }

  async getSense(lexiconCode: string, id: string): Promise<ISense | undefined> {
    if (!lexiconCode) return;
    return readingLexicon(() => this.fwLiteApi.getSense(id, lexiconCode));
  }

  async addEntry(lexiconCode: string, entry: PartialEntry): Promise<IEntry | undefined> {
    if (!lexiconCode) return;
    return readingLexicon(() => this.fwLiteApi.postNewEntry(entry, lexiconCode));
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
