import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import {HistoryService} from '$lib/services/history-service';
import {type ResourceReturn} from 'runed';

const symbol = Symbol.for('fw-lite-activity-authors');
export function useActivityAuthors(): ActivityAuthorsService {
  const projectContext = useProjectContext();
  return projectContext.getOrAdd(symbol, () => new ActivityAuthorsService(projectContext));
}

/**
 * The distinct set of author names recorded in commit metadata for the current project.
 * Loaded lazily on first read of `current` and cached on the project context.
 * Includes a `null`/`undefined` entry if any commits lack an author name.
 */
export class ActivityAuthorsService {
  constructor(projectContext: ProjectContext) {
    const historyService = new HistoryService(projectContext);
    this.#resource = projectContext.apiResource(
      [],
      () => historyService.authors(projectContext.projectCode),
    );
  }

  #resource: ResourceReturn<(string | undefined)[], unknown, true>;

  current: (string | undefined)[] = $derived.by(() => this.#resource.current);
  loading: boolean = $derived.by(() => this.#resource.loading);

  async refetch() {
    await this.#resource.refetch();
    return this.current;
  }
}
