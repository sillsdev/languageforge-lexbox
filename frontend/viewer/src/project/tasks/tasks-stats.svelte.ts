import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
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
 *
 * Each count scans every entry, so while a task is open only that task is counted.
 */
export class TasksStats {
  totalEntries = $state<number>();
  progress = $state<Record<string, TaskProgress>>({});
  openTaskId: string | undefined;
  #loadVersion = 0;
  #tasks: () => Task[];

  constructor(tasks: () => Task[]) {
    this.#tasks = tasks;
  }

  async load(api: IMiniLcmJsInvokable): Promise<void> {
    const version = ++this.#loadVersion;
    const fresh = () => version === this.#loadVersion;
    const tasks = this.#tasks().filter(task => !this.openTaskId || task.id === this.openTaskId);

    const totalEntries = await api.countEntries(undefined, undefined);
    if (!fresh()) return;
    this.totalEntries = totalEntries;

    await Promise.all(tasks.map(async task => {
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
  const tasksService = useTasksService();

  return projectContext.getOrAdd(tasksStatsSymbol, () => {
    const stats = new TasksStats(() => tasksService.listTasks());
    return projectContext.apiResource(stats, async (api) => {
      await stats.load(api);
      return stats;
    });
  });
}
