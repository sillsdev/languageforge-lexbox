import {useProjectContext} from '$lib/project-context.svelte';

const publicationsSymbol = Symbol.for('fw-lite-publications');
export function usePublications() {
  const projectContext = useProjectContext();
  return projectContext.getOrAddAsync(publicationsSymbol, [], async (api) => {
    const publications = await api.getPublications();
    return publications.sort((a, b) => {
      const aName = Object.values(a.name)[0] || '';
      const bName = Object.values(b.name)[0] || '';
      return aName.localeCompare(bName);
    });
  });
}
