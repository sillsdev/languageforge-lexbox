import {useProjectContext} from '$project/project-context.svelte';
import {type ResourceReturn} from 'runed';
import {useHistoryService} from '$lib/services/history-service';
import type {IActivityAuthor} from '$lib/dotnet-types';
import {assignAuthorColors, authorColorFallback, NO_AUTHOR_COLOR} from './author-color';
import {authorFilterKey, FIELDWORKS_AUTHOR_KEY, SYSTEM_AUTHOR_KEY, UNKNOWN_AUTHOR_KEY} from './utils';

const symbol = Symbol.for('fw-lite-author-colors');

export function useAuthorColors(): AuthorColorService {
  const projectContext = useProjectContext();
  const historyService = useHistoryService();
  return projectContext.getOrAdd(symbol, () => {
    const resource = projectContext.apiResource<IActivityAuthor[]>(
      [],
      () => (historyService.loaded ? historyService.listActivityAuthors() : Promise.resolve([])),
    );
    return new AuthorColorService(resource);
  });
}

function isPersonAuthor(key: string): boolean {
  return key !== FIELDWORKS_AUTHOR_KEY && key !== SYSTEM_AUTHOR_KEY && key !== UNKNOWN_AUTHOR_KEY;
}

// Shared, project-scoped colour assignment for person authors. Once the author list has loaded it hands out
// the set-based assignment (distinct colours while the palette lasts); before then, and for a name not in the
// loaded set, it falls back to the per-name hash — which equals the set-based colour whenever that name's hash
// slot is uncontested, so most icons never change colour once the list arrives.
export class AuthorColorService {
  #resource: ResourceReturn<IActivityAuthor[], unknown, true>;

  constructor(resource: ResourceReturn<IActivityAuthor[], unknown, true>) {
    this.#resource = resource;
  }

  #colors: Map<string, string> = $derived.by(() => {
    const names = this.#resource.current
      .filter(a => a.authorName && isPersonAuthor(authorFilterKey(a)))
      .map(a => a.authorName!);
    return assignAuthorColors(names);
  });

  colorFor(name: string | undefined | null): string {
    if (!name) return NO_AUTHOR_COLOR;
    return this.#colors.get(name) ?? authorColorFallback(name);
  }
}
