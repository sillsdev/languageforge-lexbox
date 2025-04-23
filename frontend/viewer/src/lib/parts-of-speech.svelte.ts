import type {IPartOfSpeech} from '$lib/dotnet-types';
import {useWritingSystemService, type WritingSystemService} from './writing-system-service.svelte';
import {type ProjectContext, useProjectContext} from '$lib/project-context.svelte';
import {type ResourceReturn} from 'runed';

type LabeledPartOfSpeech = IPartOfSpeech & { label: string };

const symbol = Symbol.for('fw-lite-pos');
export function usePartsOfSpeech(): PartOfSpeechService {
  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();
  return projectContext.getOrAdd(symbol, () => {
    return new PartOfSpeechService(projectContext, writingSystemService);
  });
}

export class PartOfSpeechService {
  constructor(projectContext: ProjectContext, private writingSystemService: WritingSystemService) {
    this.#posResource = projectContext.apiResource([], api => api.getPartsOfSpeech());
  }

  #posResource: ResourceReturn<IPartOfSpeech[], unknown, true>;

  current: LabeledPartOfSpeech[] = $derived.by(() => {
    return this.#posResource.current.map(pos => ({
      ...pos,
      label: this.writingSystemService.pickBestAlternative(pos.name, 'analysis')
    }));
  });

  async refetch() {
    await this.#posResource.refetch();
    return this.current;
  }
}
