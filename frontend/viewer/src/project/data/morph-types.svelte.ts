import type {MorphType, IMorphTypeData} from '$lib/dotnet-types';

import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import {type ResourceReturn} from 'runed';

const morphTypesSymbol = Symbol.for('fw-lite-morph-types');
export function useMorphTypesService(): MorphTypesService {
  const projectContext = useProjectContext();
  return projectContext.getOrAdd(morphTypesSymbol, () => {
    return new MorphTypesService(projectContext);
  });
}

export class MorphTypesService {
  constructor(projectContext: ProjectContext) {
    this.#morphTypesResource = projectContext.apiResource([], api => api.getAllMorphTypeData());
  }

  #morphTypesResource: ResourceReturn<IMorphTypeData[], unknown, true>;

  current: IMorphTypeData[] = $derived.by(() => {
    return this.#morphTypesResource.current;
  });

  async refetch() {
    await this.#morphTypesResource.refetch();
    return this.current;
  }

  prefixes = $derived.by(() => {
    const result: Partial<{[morphType in MorphType]: string|undefined}> = {};
    this.current.forEach(morphTypeData => {
      result[morphTypeData.morphType] = morphTypeData.leadingToken;
    });
    return result;
  });

  suffixes = $derived.by(() => {
    const result: Partial<{[morphType in MorphType]: string|undefined}> = {};
    this.current.forEach(morphTypeData => {
      result[morphTypeData.morphType] = morphTypeData.trailingToken;
    });
    return result;
  });

  getPrefix(morphType: MorphType): string|undefined {
    return this.prefixes[morphType];
  }

  getSuffix(morphType: MorphType): string|undefined {
    return this.suffixes[morphType];
  }

  decorate(headword: string | undefined, morphType: MorphType): string|undefined {
    if (!headword) return headword;
    const prefix = this.getPrefix(morphType) ?? '';
    const suffix = this.getSuffix(morphType) ?? '';
    return `${prefix}${headword}${suffix}`;
  }
}
