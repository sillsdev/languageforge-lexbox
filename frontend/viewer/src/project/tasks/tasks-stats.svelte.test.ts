import {expect, it} from 'vitest';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import type {IFilterQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IFilterQueryOptions';
import type {Task} from './tasks-service';
import {TasksStats} from './tasks-stats.svelte';

it('counts only the open task, and every task when none is open', async () => {
  const calls: string[] = [];
  const api = {
    countEntries: (_query?: string, options?: IFilterQueryOptions) => {
      const filter = options?.filter?.gridifyFilter ?? 'total';
      calls.push(filter);
      return Promise.resolve(filter === 'total' ? 100 : 25);
    },
  } as unknown as IMiniLcmJsInvokable;
  const stats = new TasksStats(() => ['gloss', 'definition'].map(id => ({id, gridifyFilter: id}) as Task));

  stats.openTaskId = 'gloss';
  await stats.load(api);
  expect(calls.sort()).toEqual(['gloss', 'total']);
  expect(stats.progress).toEqual({gloss: {remaining: 25, percentDone: 75}});

  calls.length = 0;
  stats.openTaskId = undefined;
  await stats.load(api);
  expect(calls.sort()).toEqual(['definition', 'gloss', 'total']);
});
