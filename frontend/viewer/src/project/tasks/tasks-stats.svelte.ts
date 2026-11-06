import {useDebounce} from 'runed';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import {useProjectEventBus} from '$lib/services/event-bus';
import {useProjectContext} from '$project/project-context.svelte';
import {useTasksService, type Task} from './tasks-service';

const tasksStatsSymbol = Symbol.for('fw-lite-tasks-stats');

export type TaskProgress = {
  remaining: number;
  percentDone: number;
};

/**
 * Entry counts, not sense or example counts: an entry with three senses missing a gloss
 * counts once. Don't present the number as anything else.
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

export function useTasksStats() {
  const projectContext = useProjectContext();
  const projectEventBus = useProjectEventBus();
  const tasksService = useTasksService();

  return projectContext.getOrAdd(tasksStatsSymbol, () => {
    const stats = new TasksStats(() => tasksService.listTasks());
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
