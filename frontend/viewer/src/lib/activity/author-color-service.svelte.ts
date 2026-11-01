import {useProjectContext} from '$project/project-context.svelte';
import {useHistoryService, type HistoryService} from '$lib/services/history-service';
import type {IActivityAuthor} from '$lib/dotnet-types';
import {assignAuthorColors, authorColorFallback, NO_AUTHOR_COLOR} from './author-color';
import {authorFilterKey, compareActivityAuthors, FIELDWORKS_AUTHOR_KEY, SYSTEM_AUTHOR_KEY, UNKNOWN_AUTHOR_KEY} from './utils';

const symbol = Symbol.for('fw-lite-author-colors');

export function useAuthorColors(): AuthorColorService {
  const projectContext = useProjectContext();
  const historyService = useHistoryService();
  return projectContext.getOrAdd(symbol, () => new AuthorColorService(historyService));
}

function isPersonAuthor(key: string): boolean {
  return key !== FIELDWORKS_AUTHOR_KEY && key !== SYSTEM_AUTHOR_KEY && key !== UNKNOWN_AUTHOR_KEY;
}

// Shared, project-scoped author list: the activity filter lists it and person authors get colours from it.
// Once the list has loaded it hands out the set-based assignment (distinct colours while the palette lasts);
// before then, and for a name not in the loaded set, it falls back to the per-name hash, which equals the
// set-based colour whenever that name's hash slot is uncontested, so most icons never change colour once the
// list arrives.
export class AuthorColorService {
  #historyService: HistoryService;
  #authors: IActivityAuthor[] = $state([]);
  #inflight: Promise<void> | undefined;
  #requested = false;

  constructor(historyService: HistoryService) {
    this.#historyService = historyService;
  }

  /** Sorted for display */
  get authors(): IActivityAuthor[] {
    this.#loadOnFirstRead();
    return this.#authors;
  }

  /** Reloads the list, joining a load already in flight */
  refresh(): Promise<void> {
    if (!this.#historyService.loaded) return Promise.resolve();
    this.#requested = true;
    this.#inflight ??= this.#historyService.listActivityAuthors()
      .then((data) => {
        this.#authors = Array.isArray(data) ? data.sort(compareActivityAuthors) : [];
      })
      .finally(() => this.#inflight = undefined);
    return this.#inflight;
  }

  #loadOnFirstRead(): void {
    // `loaded` is reactive, so a read before history loads runs again once it has
    if (this.#requested || !this.#historyService.loaded) return;
    this.#requested = true;
    // Reads happen during render, where writing $state isn't allowed
    queueMicrotask(() => void this.refresh());
  }

  #colors: Map<string, string> = $derived.by(() => {
    const names = this.authors
      .filter(a => a.authorName && isPersonAuthor(authorFilterKey(a)))
      .map(a => a.authorName!);
    return assignAuthorColors(names);
  });

  colorFor(name: string | undefined | null): string {
    if (!name) return NO_AUTHOR_COLOR;
    return this.#colors.get(name) ?? authorColorFallback(name);
  }
}
