import {ActivitySort, type IActivityAuthor, type IActivityQuery, type IProjectActivity} from '$lib/dotnet-types';
import type {ChangeType} from '$lib/dotnet-types/generated-types/LcmCrdt/ChangeType';
import {gt} from 'svelte-i18n-lingui';

export const ALL_AUTHORS = '__all__';
export const UNKNOWN_AUTHOR_KEY = '__unknown__';
// Mirrors FieldWorksAuthorName in backend/FwLite/LcmCrdt/Utils/CommitHelpers.cs
export const FIELDWORKS_AUTHOR_KEY = authorFilterKey({authorName: 'FieldWorks'});
// Mirrors SystemAuthorId in backend/FwLite/LcmCrdt/Utils/CommitHelpers.cs
export const SYSTEM_AUTHOR_KEY = '00000000-0000-0000-0000-000000000001';
// Mirrors LocalAuthor.AnonymousIdPrefix in backend/FwLite/LcmCrdt/CurrentAuthorService.cs
export const ANONYMOUS_AUTHOR_ID_PREFIX = 'anonymous:';
export const ALL_CHANGE_TYPES = '__all__';
export const MIN_VISIBLE_FILTERED = 20;

export type ActivityLoad = {
  items: IProjectActivity[];
  hasMorePages: boolean;
  queryKey: string;
};

export const emptyActivityLoad: ActivityLoad = {
  items: [],
  hasMorePages: true,
  queryKey: '',
};

export type MultiFilterSelection = string[] | 'all';

export type ActivityFilters = {
  authorFilterKeys: MultiFilterSelection;
  changeTypeFilterKeys: MultiFilterSelection;
  sort: ActivitySort;
};

export function createDefaultActivityFilters(): ActivityFilters {
  return {
    authorFilterKeys: 'all',
    changeTypeFilterKeys: 'all',
    sort: ActivitySort.NewestFirst,
  };
}

export function isAllFilterSelection(selected: MultiFilterSelection, allKeys: string[]): boolean {
  return selected === 'all' || (allKeys.length > 0 && selected.length === allKeys.length && allKeys.every(k => selected.includes(k)));
}

export function resolveFilterKeys(selected: MultiFilterSelection, allKeys: string[]): string[] {
  return selected === 'all' ? allKeys : selected;
}

export function toServerQuery(filters: ActivityFilters): IActivityQuery {
  return {
    authorFilterKeys: filters.authorFilterKeys === 'all' ? undefined : filters.authorFilterKeys,
    changeTypeKeys: filters.changeTypeFilterKeys === 'all' ? undefined : filters.changeTypeFilterKeys as ChangeType[],
    sort: filters.sort,
  };
}

export function serverQueryKey(filters: ActivityFilters): string {
  return JSON.stringify(toServerQuery(filters));
}

export function formatJsonForUi(json: object) {
  return JSON.stringify(json, null, 2)
    .split('\n')
    .slice(1, -1)
    .map(line => line.slice(2))
    .join('\n');
}

export function authorFilterKey(author: Omit<IActivityAuthor, 'commitCount'>): string {
  if (!author.authorId && !author.authorName) return UNKNOWN_AUTHOR_KEY;
  if (author.authorId) return author.authorId;
  return `name:${author.authorName}`;
}

export function wellKnownAuthorKeyToLabel(key: string | undefined): string | undefined {
  if (key === UNKNOWN_AUTHOR_KEY) return gt`Unknown`;
  if (key === SYSTEM_AUTHOR_KEY) return gt`System`;
  return undefined;
}

/** A device that edited before anyone signed in, or chose a name, on it. */
export function isAnonymousAuthor(authorId: string | undefined): boolean {
  return !!authorId?.startsWith(ANONYMOUS_AUTHOR_ID_PREFIX);
}

/** What to show for an author: a translated label for well-known and unnamed offline authors, else the stored name. */
export function authorDisplayName(author: Omit<IActivityAuthor, 'commitCount'>): string | undefined {
  return wellKnownAuthorKeyToLabel(authorFilterKey(author))
    ?? author.authorName
    ?? (isAnonymousAuthor(author.authorId) ? gt`Offline user` : undefined);
}

function authorSortRank(author: IActivityAuthor): number {
  // Unknown is pinned to the top (it's the catch-all, not a real author); FieldWorks, System
  // and people all sort alphabetically together.
  return authorFilterKey(author) === UNKNOWN_AUTHOR_KEY ? 0 : 1;
}

export function compareActivityAuthors(a: IActivityAuthor, b: IActivityAuthor): number {
  const rankDiff = authorSortRank(a) - authorSortRank(b);
  if (rankDiff !== 0) return rankDiff;
  const aName = authorDisplayName(a) || a.authorId || '';
  const bName = authorDisplayName(b) || b.authorId || '';
  return aName.localeCompare(bName);
}

export function applyMultiSelectValue(
  value: string[],
  allKeys: string[],
  allKey: string,
  currentSelection: MultiFilterSelection,
): MultiFilterSelection {
  if (value.includes(allKey)) {
    return isAllFilterSelection(currentSelection, allKeys) ? [] : 'all';
  }
  if (isAllFilterSelection(value, allKeys)) {
    return 'all';
  }
  return value;
}

export function hasActiveServerSideFilters(filters: ActivityFilters): boolean {
  return filters.authorFilterKeys !== 'all' || filters.changeTypeFilterKeys !== 'all';
}
