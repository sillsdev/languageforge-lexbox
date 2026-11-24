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
 *
 * Each count scans every entry, so only what's on screen ({@link showing}) is recounted,
 * one pass at a time. Everything else stays out of date until it's shown.
 */
export class TasksStats {
  totalEntries = $state<number>();
  progress = $state<Record<string, TaskProgress>>({});
  /** A task id, 'all' for the task list, or undefined when no count is visible. Refresh after changing it. */
  showing: string | undefined;
  #tasks: () => Task[];
  #invalidations = 0;
  /** The invalidation each task's count started in; an older one means it's out of date. */
  #countedIn: Record<string, number> = {};
  #api?: IMiniLcmJsInvokable;
  #running?: Promise<void>;
  #rerun = false;

  constructor(tasks: () => Task[]) {
    this.#tasks = tasks;
  }

  invalidate(): void {
    this.#invalidations++;
  }

  /** Counts whatever is showing and out of date. A call during a pass queues a single follow-up pass. */
  refresh(api: IMiniLcmJsInvokable): Promise<void> {
    if (this.#api !== api) this.invalidate();
    this.#api = api;
    if (this.#running) {
      this.#rerun = true;
      return this.#running;
    }
    this.#running = this.#runPasses();
    return this.#running;
  }

  async #runPasses(): Promise<void> {
    let failure: {error: unknown} | undefined;
    do {
      this.#rerun = false;
      try {
        await this.#countOutOfDate(this.#api!);
        failure = undefined;
      } catch (error) {
        failure = {error};
      }
    } while (this.#rerun);
    // cleared in the same tick the loop ends, or a refresh arriving in between would queue behind nothing
    this.#running = undefined;
    if (failure) throw failure.error;
  }

  async #countOutOfDate(api: IMiniLcmJsInvokable): Promise<void> {
    const showing = this.showing;
    const tasks = this.#tasks().filter(task =>
      (showing === 'all' || task.id === showing) && this.#countedIn[task.id] !== this.#invalidations);
    if (!tasks.length) return;
    const invalidations = this.#invalidations;

    const totalEntries = await api.countEntries(undefined, undefined);
    this.totalEntries = totalEntries;

    const results = await Promise.allSettled(tasks.map(async task => {
      const remaining = await api.countEntries(undefined, {filter: {gridifyFilter: task.gridifyFilter}});
      this.progress = {
        ...this.progress,
        [task.id]: {
          remaining,
          percentDone: totalEntries === 0 ? 0 : Math.round(((totalEntries - remaining) / totalEntries) * 100),
        },
      };
      // not the current one: a count that raced an entry change may already be wrong
      this.#countedIn[task.id] = invalidations;
    }));
    const failed = results.find(result => result.status === 'rejected');
    if (failed) throw failed.reason;
  }
}

export function useTasksStats() {
  const projectContext = useProjectContext();
  const projectEventBus = useProjectEventBus();
  const tasksService = useTasksService();

  return projectContext.getOrAdd(tasksStatsSymbol, () => {
    const stats = new TasksStats(() => tasksService.listTasks());
    const resource = projectContext.apiResource(stats, async (api) => {
      await stats.refresh(api);
      return stats;
    });
    const debouncedRefetch = useDebounce(() => void resource.refetch(), 500);
    projectEventBus.onEntriesChanged(() => {
      stats.invalidate();
      void debouncedRefetch();
    });
    return resource;
  });
}
