import {useProjectContext} from '$project/project-context.svelte';
import {useHistoryService, type HistoryService} from '$lib/services/history-service';
import type {IActivityAuthor} from '$lib/dotnet-types';
import {assignAuthorColors, authorColorFallback, NO_AUTHOR_COLOR, type AuthorColor} from './author-color';
import {authorFilterKey, compareActivityAuthors, FIELDWORKS_AUTHOR_KEY, SYSTEM_AUTHOR_KEY, UNKNOWN_AUTHOR_KEY} from './utils';

const symbol = Symbol.for('fw-lite-author-colors');

export function useAuthorColors(): AuthorColorService {
  const projectContext = useProjectContext();
  const historyService = useHistoryService();
  return projectContext.getOrAdd(symbol, () => new AuthorColorService(historyService));
}

type AuthorRef = Pick<IActivityAuthor, 'authorId' | 'authorName'>;

function isPersonAuthor(key: string): boolean {
  return key !== FIELDWORKS_AUTHOR_KEY && key !== SYSTEM_AUTHOR_KEY && key !== UNKNOWN_AUTHOR_KEY;
}

function compareFirstCommit(a: IActivityAuthor, b: IActivityAuthor): number {
  return Date.parse(a.firstCommitDate) - Date.parse(b.firstCommitDate)
    || authorFilterKey(a).localeCompare(authorFilterKey(b));
}

// Project-scoped author list: the activity filter lists it, and activity icons and comment avatars take their
// colours from it, keyed by author id. Colours are handed out by first commit so a new author can't take an
// existing author's colour. The exception: commits made offline long ago and synced later can predate
// authors already listed.
export class AuthorColorService {
  #historyService: HistoryService;
  #authors: IActivityAuthor[] = $state([]);
  #inflight: Promise<void> | undefined;
  #requested = false;
  #loaded = false;
  // eslint-disable-next-line svelte/prefer-svelte-reactivity -- written during render, so it mustn't be reactive
  #missingKeys = new Set<string>();

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
        this.#loaded = true;
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

  #colors: Map<string, AuthorColor> = $derived.by(() => {
    const keys = this.authors
      .toSorted(compareFirstCommit)
      .map(authorFilterKey)
      .filter(isPersonAuthor);
    return assignAuthorColors(keys);
  });

  colorFor(author: AuthorRef): AuthorColor {
    const key = authorFilterKey(author);
    if (!isPersonAuthor(key)) return NO_AUTHOR_COLOR;
    const color = this.#colors.get(key);
    if (color) return color;
    // An author newer than the loaded list (e.g. their first comment was just added). Reload once per key:
    // the current user may have no commits yet, and retrying them would loop.
    if (this.#loaded && !this.#missingKeys.has(key)) {
      this.#missingKeys.add(key);
      queueMicrotask(() => void this.refresh());
    }
    return authorColorFallback(key);
  }
}
