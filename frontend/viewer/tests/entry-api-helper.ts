import {type Page} from '@playwright/test';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
import {MorphType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/MorphType';

const DEFAULT_ORDER = {field: SortField.Headword, writingSystem: 'default', ascending: true} as const;

/**
 * Helper for interacting with the MiniLcm API via page.evaluate().
 * Wraps all API calls to keep tests clean and readable.
 */
export class EntryApiHelper {
  constructor(private page: Page) {}

  async countEntries(): Promise<number> {
    return this.page.evaluate(async () => {
      return window.__PLAYWRIGHT_UTILS__.demoApi.countEntries();
    });
  }

  async getEntryAtIndex(index: number): Promise<{entryId: string; headword: string; senseCount: number}> {
    return this.page.evaluate(async ({idx, order}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries({offset: idx, count: 1, order});
      const entry = entries[0];
      return {
        entryId: entry.id,
        headword: entry.citationForm?.seh ?? entry.lexemeForm?.seh ?? '',
        senseCount: entry.senses.length,
      };
    }, {idx: index, order: DEFAULT_ORDER});
  }

  async getHeadwordAtIndex(index: number): Promise<string> {
    const {headword} = await this.getEntryAtIndex(index);
    return headword;
  }

  async getEntryIdAtIndex(index: number): Promise<string> {
    const {entryId} = await this.getEntryAtIndex(index);
    return entryId;
  }

  async getLastEntry(): Promise<{headword: string; index: number}> {
    return this.page.evaluate(async ({order}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const count = await api.countEntries();
      const entries = await api.getEntries({offset: count - 1, count: 1, order});
      const entry = entries[0];
      return {
        headword: entry.citationForm?.seh ?? entry.lexemeForm?.seh ?? '',
        index: count - 1,
      };
    }, {order: DEFAULT_ORDER});
  }

  async getEntryIndex(entryId: string): Promise<number> {
    return this.page.evaluate(async ({id}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      return api.getEntryIndex(id, undefined, undefined);
    }, {id: entryId});
  }

  async createEntryAtIndex(targetIndex: number): Promise<{id: string; headword: string}> {
    return this.page.evaluate(async ({idx, order, morphType}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const offset = Math.max(0, idx - 1);
      const entries = await api.getEntries({offset, count: 1, order});
      const entry = entries[0];
      const baseHeadword = entry?.citationForm?.seh ?? entry?.lexemeForm?.seh ?? '#';
      const newHeadword = baseHeadword + '-inserted';
      const newEntry = {
        id: crypto.randomUUID(),
        lexemeForm: {seh: newHeadword},
        citationForm: {seh: newHeadword},
        senses: [],
        note: {},
        literalMeaning: {},
        morphType,
        components: [],
        complexForms: [],
        complexFormTypes: [],
        publishIn: [],
      };
      const created = await api.createEntry(newEntry);
      return {id: created.id, headword: newHeadword};
    }, {idx: targetIndex, order: DEFAULT_ORDER, morphType: MorphType.Unknown});
  }

  async createEntryWithHeadword(headword: string): Promise<{id: string; headword: string}> {
    return this.page.evaluate(async ({hw, morphType}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const newEntry = {
        id: crypto.randomUUID(),
        lexemeForm: {seh: hw},
        citationForm: {seh: hw},
        senses: [],
        note: {},
        literalMeaning: {},
        morphType,
        components: [],
        complexForms: [],
        complexFormTypes: [],
        publishIn: [],
      };
      const created = await api.createEntry(newEntry);
      return {id: created.id, headword: hw};
    }, {hw: headword, morphType: MorphType.Unknown});
  }

  async deleteEntry(entryId: string): Promise<void> {
    await this.page.evaluate(async (id) => {
      await window.__PLAYWRIGHT_UTILS__.demoApi.deleteEntry(id);
    }, entryId);
  }

  async updateEntryHeadword(index: number, suffix: string): Promise<{id: string; updatedHeadword: string}> {
    return this.page.evaluate(async ({idx, sfx, order}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries({offset: idx, count: 1, order});
      const entry = entries[0];
      const currentHeadword = entry.citationForm?.seh ?? entry.lexemeForm?.seh ?? 'entry';
      const newHeadword = currentHeadword + sfx;
      const updated = {
        ...entry,
        lexemeForm: {...entry.lexemeForm, seh: newHeadword},
        citationForm: {...entry.citationForm, seh: newHeadword},
      };
      await api.updateEntry(entry, updated);
      return {id: entry.id, updatedHeadword: newHeadword};
    }, {idx: index, sfx: suffix, order: DEFAULT_ORDER});
  }

  async updateEntryHeadwordPrepend(index: number, prefix: string): Promise<{id: string; updatedHeadword: string}> {
    return this.page.evaluate(async ({idx, pfx, order}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries({offset: idx, count: 1, order});
      const entry = entries[0];
      const currentHeadword = entry.lexemeForm?.seh ?? entry.lexemeForm?.en ?? 'entry';
      const newHeadword = pfx + currentHeadword;
      const updated = {
        ...entry,
        citationForm: {...entry.lexemeForm, seh: newHeadword},
      };
      await api.updateEntry(entry, updated);
      return {id: entry.id, updatedHeadword: newHeadword};
    }, {idx: index, pfx: prefix, order: DEFAULT_ORDER});
  }

  async getEntryWithEnglishGloss(): Promise<{entryId: string; headword: string; originalGloss: string}> {
    return this.page.evaluate(async ({order}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries({offset: 0, count: 50, order});
      const entry = entries.find(e => e.senses.length > 0 && e.senses[0].gloss?.en);
      if (!entry) throw new Error('No suitable entry with English gloss found in demo data');
      return {
        entryId: entry.id,
        headword: entry.citationForm?.seh ?? entry.lexemeForm?.seh ?? '',
        originalGloss: entry.senses[0].gloss?.en ?? '',
      };
    }, {order: DEFAULT_ORDER});
  }

  async getEntry(entryId: string): Promise<unknown> {
    return this.page.evaluate(async (id) => {
      return window.__PLAYWRIGHT_UTILS__.demoApi.getEntry(id);
    }, entryId);
  }

  async getEntryGloss(entryId: string, writingSystem: string): Promise<string> {
    return this.page.evaluate(async ({id, ws}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entry = await api.getEntry(id);
      return entry?.senses[0]?.gloss?.[ws] ?? '';
    }, {id: entryId, ws: writingSystem});
  }

  async getEntryLexeme(entryId: string): Promise<string> {
    return this.page.evaluate(async (id) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entry = await api.getEntry(id);
      return entry?.lexemeForm?.seh ?? '';
    }, entryId);
  }

  async getEntrySenseCount(entryId: string): Promise<number> {
    return this.page.evaluate(async (id) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entry = await api.getEntry(id);
      return entry?.senses.length ?? 0;
    }, entryId);
  }

  async entryHasGlossValue(entryId: string, glossValue: string): Promise<boolean> {
    return this.page.evaluate(async ({id, value}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entry = await api.getEntry(id);
      return entry?.senses.some(s =>
        Object.values(s.gloss || {}).some(v => v === value)
      ) ?? false;
    }, {id: entryId, value: glossValue});
  }
}
