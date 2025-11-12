import {useProjectContext} from '$project/project-context.svelte';

const semanticDomainsSymbol = Symbol.for('fw-lite-semantic-domains');
export function useSemanticDomains() {
  const projectContext = useProjectContext();
  return projectContext.getOrAddAsync(semanticDomainsSymbol, [], async (api) => {
    const semanticDomains = await api.getSemanticDomains();
    return semanticDomains.sort((a, b) => a.code.localeCompare(b.code));
  });
}
