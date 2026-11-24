import {describe, expect, it} from 'vitest';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {IFilterQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IFilterQueryOptions';
import type {WritingSystemService} from '$project/data';
import {DashboardStats} from './dashboard-stats.svelte';

const TOTAL = 'total';

class FakeApi {
  total = 100;
  calls: string[] = [];
  fail = false;
  /** Filters whose next count fails, or waits for releaseHeld. */
  failNext = new Set<string>();
  holdNext = new Set<string>();
  #held: Array<() => void> = [];
  maxInFlightPerFilter = 0;
  #inFlight: Record<string, number> = {};
  #releaseTotal?: () => void;

  readonly api = {
    countEntries: async (_query?: string, options?: IFilterQueryOptions) => {
      const filter = options?.filter?.gridifyFilter ?? TOTAL;
      this.calls.push(filter);
      this.#inFlight[filter] = (this.#inFlight[filter] ?? 0) + 1;
      this.maxInFlightPerFilter = Math.max(this.maxInFlightPerFilter, this.#inFlight[filter]);
      // read before waiting, like a query that started before a later change
      const result = filter === TOTAL ? this.total : 0;
      const fail = this.fail || this.failNext.delete(filter);
      if (this.holdNext.delete(filter)) await new Promise<void>(resolve => this.#held.push(resolve));
      if (filter === TOTAL && this.holdNextTotal) {
        this.holdNextTotal = false;
        await new Promise<void>(resolve => this.#releaseTotal = resolve);
      }
      this.#inFlight[filter]--;
      if (fail) throw new Error('count failed');
      return result;
    },
    getWritingSystems: () => Promise.resolve({vernacular: [{wsId: 'seh', abbreviation: 'seh'}], analysis: []}),
  } as unknown as IMiniLcmJsInvokable;

  holdNextTotal = false;

  release() {
    this.#releaseTotal?.();
  }

  releaseHeld() {
    this.#held.forEach(resolve => resolve());
  }
}

function setup() {
  const fake = new FakeApi();
  const stats = new DashboardStats({vernacular: [], analysis: []} as unknown as WritingSystemService);
  return {fake, stats, api: fake.api};
}

describe('DashboardStats', () => {
  it('folds loads requested during a load into one reload that ends on the latest counts', async () => {
    const {fake, stats, api} = setup();
    fake.holdNextTotal = true;
    const done = stats.load(api);
    await Promise.resolve();

    for (const total of [99, 98, 97]) {
      fake.total = total;
      void stats.load(api);
    }
    fake.release();
    await done;

    expect(fake.calls.filter(filter => filter === TOTAL)).toHaveLength(2);
    expect(fake.maxInFlightPerFilter).toBe(1);
    expect(stats.totalEntries).toBe(97);
    expect(stats.entriesWithSenses).toEqual({filled: 97, percent: 100});
  });

  it('still runs a queued reload when the running load fails', async () => {
    const {fake, stats, api} = setup();
    fake.fail = true;
    fake.holdNextTotal = true;
    const done = stats.load(api);
    await Promise.resolve();
    void stats.load(api);
    fake.fail = false;
    fake.release();

    await done;
    expect(stats.totalEntries).toBe(100);
  });

  it('keeps a slow count from a failed load off the numbers of the reload after it', async () => {
    const {fake, stats, api} = setup();
    fake.holdNext.add('Senses=null');
    fake.failNext.add('Senses.ExampleSentences=null');
    const done = stats.load(api);
    fake.total = 90;
    void stats.load(api);

    await done;
    expect(stats.entriesWithSenses).toEqual({filled: 90, percent: 100});
    fake.releaseHeld();
    await new Promise(resolve => setTimeout(resolve));
    expect(stats.entriesWithSenses).toEqual({filled: 90, percent: 100});
  });

  it('loads again after a failed load', async () => {
    const {fake, stats, api} = setup();
    fake.fail = true;
    await expect(stats.load(api)).rejects.toThrow('count failed');

    fake.fail = false;
    await stats.load(api);
    expect(stats.totalEntries).toBe(100);
  });
});
