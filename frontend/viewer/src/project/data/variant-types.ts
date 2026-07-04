import {useProjectContext} from '$project/project-context.svelte';

const variantTypesSymbol = Symbol.for('fw-lite-variant-types');
export function useVariantTypes() {
  const projectContext = useProjectContext();
  // eager: adding a variant defaults its type to Unspecified Variant, so the list
  // must already be loaded when the user picks an entry (nothing reads it before then)
  return projectContext.getOrAddAsync(variantTypesSymbol, [], api => api.getVariantTypes(), {eager: true});
}

/** Unspecified Variant — the type FLEx assigns when none is chosen (well-known guid) */
export const UNSPECIFIED_VARIANT_TYPE_ID = '3942addb-99fd-43e9-ab7d-99025ceb0d4e';
