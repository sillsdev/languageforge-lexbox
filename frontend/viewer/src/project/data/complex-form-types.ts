import {useProjectContext} from '$project/project-context.svelte';

const complexFormTypesSymbol = Symbol.for('fw-lite-complex-form-types');
export function useComplexFormTypes() {
  const projectContext = useProjectContext();
  return projectContext.getOrAddAsync(complexFormTypesSymbol, [], api => api.getComplexFormTypes());
}
