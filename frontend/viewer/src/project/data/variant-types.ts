import {useProjectContext} from '$project/project-context.svelte';

const variantTypesSymbol = Symbol.for('fw-lite-variant-types');
export function useVariantTypes() {
  const projectContext = useProjectContext();
  return projectContext.getOrAddAsync(variantTypesSymbol, [], api => api.getVariantTypes());
}

/** Unspecified Variant — the type FLEx assigns when none is chosen (well-known guid) */
export const UNSPECIFIED_VARIANT_TYPE_ID = '3942addb-99fd-43e9-ab7d-99025ceb0d4e';
