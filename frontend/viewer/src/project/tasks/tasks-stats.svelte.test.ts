import {describe, expect, it} from 'vitest';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {IFilterQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IFilterQueryOptions';
import type {Task} from './tasks-service';
import {TasksStats} from './tasks-stats.svelte';

const TOTAL = 'total';

/** Counts by filter, 'total' for the unfiltered one. Tasks use their id as their filter. */
class FakeApi {
  remaining: Record<string, number> = {[TOTAL]: 100};
  calls: string[] = [];
  failing = new Set<string>();
  maxInFlightPerFilter = 0;
  #inFlight: Record<string, number> = {};
  #held: Array<() => void> | undefined;

  readonly api = {
    countEntries: async (_query?: string, options?: IFilterQueryOptions) => {
      const filter = options?.filter?.gridifyFilter ?? TOTAL;
      this.calls.push(filter);
      this.#inFlight[filter] = (this.#inFlight[filter] ?? 0) + 1;
      this.maxInFlightPerFilter = Math.max(this.maxInFlightPerFilter, this.#inFlight[filter]);
      // read before waiting, like a query that started before a later save
      const result = this.remaining[filter] ?? 0;
      if (this.#held && filter !== TOTAL) await new Promise<void>(resolve => this.#held!.push(resolve));
      this.#inFlight[filter]--;
      if (this.failing.has(filter)) throw new Error(`count failed: ${filter}`);
      return result;
    },
  } as unknown as IMiniLcmJsInvokable;

  /** Holds task counts open (not the total) until release. */
  hold() {
    this.#held = [];
  }

  release() {
    const held = this.#held ?? [];
    this.#held = undefined;
    held.forEach(resolve => resolve());
  }

  takeCalls(): string[] {
    const calls = this.calls.sort();
    this.calls = [];
    return calls;
  }
}

function setup(...taskIds: string[]) {
  const tasks = taskIds.map(id => ({id, gridifyFilter: id}) as Task);
  const fake = new FakeApi();
  const stats = new TasksStats(() => tasks);
  return {fake, stats, tasks, api: fake.api};
}

/** Lets a held pass get as far as it can. */
async function settle() {
  for (let i = 0; i < 10; i++) await Promise.resolve();
}

describe('TasksStats', () => {
  it('counts every task on the list once, and nothing again until entries change', async () => {
    const {fake, stats, api} = setup('gloss', 'definition');
    fake.remaining = {[TOTAL]: 200, gloss: 50, definition: 0};
    stats.showing = 'all';

    await stats.refresh(api);
    expect(fake.takeCalls()).toEqual(['definition', 'gloss', TOTAL]);
    expect(stats.totalEntries).toBe(200);
    expect(stats.progress).toEqual({
      gloss: {remaining: 50, percentDone: 75},
      definition: {remaining: 0, percentDone: 100},
    });

    await stats.refresh(api);
    expect(fake.takeCalls()).toEqual([]);
  });

  it('recounts only the open task after an entry change, then the rest when the list comes back', async () => {
    const {fake, stats, api} = setup('gloss', 'definition', 'example');
    fake.remaining = {[TOTAL]: 100, gloss: 10, definition: 20, example: 30};
    stats.showing = 'all';
    await stats.refresh(api);
    fake.takeCalls();

    stats.showing = 'gloss';
    await stats.refresh(api);
    expect(fake.takeCalls()).toEqual([]);

    // filling in the gloss of an entry without senses also gives it a sense, so definition changes too
    fake.remaining = {[TOTAL]: 100, gloss: 9, definition: 19, example: 30};
    stats.invalidate();
    await stats.refresh(api);
    expect(fake.takeCalls()).toEqual(['gloss', TOTAL]);
    expect(stats.progress.gloss.remaining).toBe(9);

    stats.showing = 'all';
    await stats.refresh(api);
    expect(fake.takeCalls()).toEqual(['definition', 'example', TOTAL]);
    expect(stats.progress.definition.remaining).toBe(19);
  });

  it('counts nothing while no count is visible, and catches up once one is', async () => {
    const {fake, stats, api} = setup('gloss', 'definition');
    stats.showing = 'all';
    await stats.refresh(api);
    fake.takeCalls();

    stats.showing = undefined;
    fake.remaining.gloss = 7;
    stats.invalidate();
    await stats.refresh(api);
    expect(fake.takeCalls()).toEqual([]);

    stats.showing = 'all';
    await stats.refresh(api);
    expect(fake.takeCalls()).toEqual(['definition', 'gloss', TOTAL]);
    expect(stats.progress.gloss.remaining).toBe(7);
  });

  it('folds entry changes during a pass into one follow-up pass that ends on the latest counts', async () => {
    const {fake, stats, api} = setup('gloss', 'definition');
    fake.remaining = {[TOTAL]: 100, gloss: 10, definition: 20};
    stats.showing = 'all';

    fake.hold();
    const done = stats.refresh(api);
    await settle();
    for (const gloss of [9, 8, 7]) {
      fake.remaining.gloss = gloss;
      stats.invalidate();
      void stats.refresh(api);
    }
    fake.release();
    await settle();
    fake.release();
    await done;

    expect(fake.takeCalls()).toEqual(['definition', 'definition', 'gloss', 'gloss', TOTAL, TOTAL]);
    expect(fake.maxInFlightPerFilter).toBe(1);
    expect(stats.progress.gloss.remaining).toBe(7);
  });

  it('recounts a task whose count was running when an entry changed, without another refresh', async () => {
    const {fake, stats, api} = setup('gloss');
    fake.remaining = {[TOTAL]: 100, gloss: 10};
    stats.showing = 'gloss';

    fake.hold();
    const done = stats.refresh(api);
    await settle();
    fake.remaining.gloss = 9;
    stats.invalidate();
    fake.release();
    await done;
    expect(stats.progress.gloss.remaining).toBe(10);

    // what the debounced refetch does 500 ms after the change
    await stats.refresh(api);
    expect(stats.progress.gloss.remaining).toBe(9);
  });

  it('retries only the count that failed, and is not stuck after a failure', async () => {
    const {fake, stats, api} = setup('gloss', 'definition');
    stats.showing = 'all';
    fake.failing.add('gloss');

    await expect(stats.refresh(api)).rejects.toThrow('count failed: gloss');
    expect(stats.progress.definition).toBeDefined();
    fake.takeCalls();

    fake.failing.clear();
    await stats.refresh(api);
    expect(fake.takeCalls()).toEqual(['gloss', TOTAL]);
    expect(stats.progress.gloss).toBeDefined();
  });

  it('counts a task added later without recounting the others', async () => {
    const {fake, stats, tasks, api} = setup('gloss');
    stats.showing = 'all';
    await stats.refresh(api);
    fake.takeCalls();

    tasks.push({id: 'definition', gridifyFilter: 'definition'} as Task);
    await stats.refresh(api);
    expect(fake.takeCalls()).toEqual(['definition', TOTAL]);
  });

  it('recounts everything for a new api', async () => {
    const {fake, stats, api} = setup('gloss', 'definition');
    stats.showing = 'all';
    await stats.refresh(api);
    fake.takeCalls();

    const other = new FakeApi();
    other.remaining = {[TOTAL]: 5, gloss: 1, definition: 2};
    await stats.refresh(other.api);
    expect(fake.takeCalls()).toEqual([]);
    expect(other.takeCalls()).toEqual(['definition', 'gloss', TOTAL]);
    expect(stats.progress.gloss.remaining).toBe(1);
  });
});
