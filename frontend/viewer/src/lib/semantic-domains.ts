import {useProjectContext} from '$lib/project-context.svelte';

const semanticDomainsSymbol = Symbol.for('fw-lite-semantic-domains');
export function useSemanticDomains() {
  const projectContext = useProjectContext();
  return projectContext.lazyLoad(semanticDomainsSymbol, [], api => api.getSemanticDomains());
}
