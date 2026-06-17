import type {ResourceReturn} from 'runed';
import {useProjectContext} from '../project-context.svelte';

const symbol = Symbol.for('test:wrapper-service');

export function useWrapperService(fetchData: () => Promise<string[]>): WrapperService {
  const projectContext = useProjectContext();
  return projectContext.getOrAdd(symbol, () => {
    const resource = projectContext.apiResource([], () => fetchData());
    return new WrapperService(resource);
  });
}

/**
 * Mirrors the PartOfSpeechService pattern: a cached class instance owning a
 * DetachedResource and exposing a `$derived.by` view that consumers read from
 * their own `$derived`. This is the shape that went stale under the bug fixed in
 * ProjectContext#ownAndCache.
 */
export class WrapperService {
  #resource: ResourceReturn<string[], unknown, true>;

  constructor(resource: ResourceReturn<string[], unknown, true>) {
    this.#resource = resource;
  }

  transformed: Array<{value: string; derived: string}> = $derived.by(() => {
    return this.#resource.current.map(value => ({value, derived: `${value}-derived`}));
  });
}
