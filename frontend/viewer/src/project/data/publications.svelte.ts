import type {IPublication} from '$lib/dotnet-types';
import {useWritingSystemService, type WritingSystemService} from './writing-system-service.svelte';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import {type ResourceReturn} from 'runed';

type LabeledPublication = IPublication & { label: string };

const symbol = Symbol.for('fw-lite-publications');
export function usePublications(): PublicationService {
  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();
  return projectContext.getOrAdd(symbol, () => {
    return new PublicationService(projectContext, writingSystemService);
  });
}

export class PublicationService {
  constructor(projectContext: ProjectContext, private writingSystemService: WritingSystemService) {
    this.#publicationsResource = projectContext.apiResource([], api => api.getPublications());
  }

  #publicationsResource: ResourceReturn<IPublication[], unknown, true>;

  current: LabeledPublication[] = $derived.by(() => {
    return this.#publicationsResource.current.map(pub => ({
      ...pub,
      label: this.getLabel(pub),
    })).sort((a, b) => a.label.localeCompare(b.label));
  });

  async refetch() {
    await this.#publicationsResource.refetch();
    return this.current;
  }

  getLabel(pub: IPublication): string {
    return this.writingSystemService.pickBestAlternative(pub.name, 'analysis');
  }
}
