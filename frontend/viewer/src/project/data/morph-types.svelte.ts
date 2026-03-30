import type {MorphTypeKind, IMorphType} from '$lib/dotnet-types';

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
    this.#morphTypesResource = projectContext.apiResource([], api => api.getMorphTypes());
  }

  #morphTypesResource: ResourceReturn<IMorphType[], unknown, true>;

  current: IMorphType[] = $derived.by(() => {
    return this.#morphTypesResource.current;
  });

  async refetch() {
    await this.#morphTypesResource.refetch();
    return this.current;
  }

  prefixes = $derived.by(() => {
    const result: Partial<{[kind in MorphTypeKind]: string|undefined}> = {};
    this.current.forEach(morphType => {
      result[morphType.kind] = morphType.prefix;
    });
    return result;
  });

  suffixes = $derived.by(() => {
    const result: Partial<{[kind in MorphTypeKind]: string|undefined}> = {};
    this.current.forEach(morphType => {
      result[morphType.kind] = morphType.postfix;
    });
    return result;
  });

  getPrefix(kind: MorphTypeKind): string|undefined {
    return this.prefixes[kind];
  }

  getSuffix(kind: MorphTypeKind): string|undefined {
    return this.suffixes[kind];
  }

  decorate(headword: string | undefined, kind: MorphTypeKind): string|undefined {
    if (!headword) return headword;
    const prefix = this.getPrefix(kind) ?? '';
    const suffix = this.getSuffix(kind) ?? '';
    return `${prefix}${headword}${suffix}`;
  }
}
