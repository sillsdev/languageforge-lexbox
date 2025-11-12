import {useProjectContext} from '$project/project-context.svelte';
import {useProjectEventBus} from '$lib/services/event-bus';

export type ProjectStats = {
  totalEntryCount: number;
}

const projectStatsSymbol = Symbol.for('fw-lite-project-stats');
export function useProjectStats() {
  const projectContext = useProjectContext();
  const projectEventBus = useProjectEventBus();
  const statsResource = projectContext.getOrAddAsync<ProjectStats | undefined>(projectStatsSymbol, undefined, async (api) => {
    const totalEntryCount = await api.countEntries(undefined, undefined);
    return {
      totalEntryCount,
    }
  }, {
    debounce: 500,
    onAdd: (resource) => {
      projectEventBus.onEntryDeleted(() => void resource.refetch());
      projectEventBus.onEntryUpdated(() => void resource.refetch());
    }
  });
  return statsResource;
}
