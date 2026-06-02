import type {IPartOfSpeech} from '$lib/dotnet-types';
import {useWritingSystemService, type WritingSystemService} from './writing-system-service.svelte';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
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

  // Plain getter, not a `$derived.by` (mirrors WritingSystemService): a cached
  // service's derived can read back stale once its observers unmount, which
  // dropped PoS labels from the dictionary preview.
  get current(): LabeledPartOfSpeech[] {
    return this.#posResource.current.map(pos => ({
      ...pos,
      label: this.getLabel(pos),
    })).sort((a, b) => a.label.localeCompare(b.label));
  }

  async refetch() {
    await this.#posResource.refetch();
    return this.current;
  }

  getLabel(pos: IPartOfSpeech): string {
    return this.writingSystemService.pickBestAlternative(pos.name, 'analysis');
  }
}
