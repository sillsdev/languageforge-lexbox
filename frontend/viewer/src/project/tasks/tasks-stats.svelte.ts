import {useDebounce} from 'runed';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import {useProjectEventBus} from '$lib/services/event-bus';
import {useProjectContext} from '$project/project-context.svelte';
import type {Task} from './tasks-service';

const tasksStatsSymbol = Symbol.for('fw-lite-tasks-stats');

export type TaskProgress = {
  /** Entries still matching the task's filter. */
  remaining: number;
  percentDone: number;
};

/**
 * How much of each task is left, so the list can show where the work is instead of
 * making people open a task to find out it's already done.
 *
 * These are entry counts, not sense or example counts, because that's what countEntries
 * gives us: an entry with three senses missing a gloss counts once. Good enough to tell
 * tasks apart, so don't present the number as anything but entries.
 */
export class TasksStats {
  totalEntries = $state<number>();
  progress = $state<Record<string, TaskProgress>>({});
  #loadVersion = 0;
  #tasks: () => Task[];

  constructor(tasks: () => Task[]) {
    this.#tasks = tasks;
  }

  async load(api: IMiniLcmJsInvokable): Promise<void> {
    const version = ++this.#loadVersion;
    const fresh = () => version === this.#loadVersion;

    const totalEntries = await api.countEntries(undefined, undefined);
    if (!fresh()) return;
    this.totalEntries = totalEntries;

    await Promise.all(this.#tasks().map(async task => {
      const remaining = await api.countEntries(undefined, {filter: {gridifyFilter: task.gridifyFilter}});
      if (!fresh()) return;
      this.progress = {
        ...this.progress,
        [task.id]: {
          remaining,
          percentDone: totalEntries === 0 ? 0 : Math.round(((totalEntries - remaining) / totalEntries) * 100),
        },
      };
    }));
  }
}

export function useTasksStats(tasks: () => Task[]) {
  const projectContext = useProjectContext();
  const projectEventBus = useProjectEventBus();

  return projectContext.getOrAdd(tasksStatsSymbol, () => {
    const stats = new TasksStats(tasks);
    const resource = projectContext.apiResource(stats, async (api) => {
      await stats.load(api);
      return stats;
    });
    const debouncedRefetch = useDebounce(() => void resource.refetch(), 500);
    projectEventBus.onEntryDeleted(() => void debouncedRefetch());
    projectEventBus.onEntryUpdated(() => void debouncedRefetch());
    return resource;
  });
}
