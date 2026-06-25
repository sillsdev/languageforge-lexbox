import {ActivitySort, type IActivityAuthor, type IActivityQuery, type IProjectActivity} from '$lib/dotnet-types';

export const ALL_AUTHORS = '__all__';
export const UNKNOWN_AUTHOR_KEY = '__unknown__';
export const FIELDWORKS_AUTHOR_KEY = authorFilterKey({authorName: 'FieldWorks'});
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
    changeTypeKeys: filters.changeTypeFilterKeys === 'all' ? undefined : filters.changeTypeFilterKeys,
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

function authorSortRank(author: IActivityAuthor): number {
  const key = authorFilterKey(author);
  if (key === UNKNOWN_AUTHOR_KEY) return 0; // 1st
  if (key === FIELDWORKS_AUTHOR_KEY) return 1; // 2nd
  return 2;
}

export function compareActivityAuthors(a: IActivityAuthor, b: IActivityAuthor): number {
  const rankDiff = authorSortRank(a) - authorSortRank(b);
  if (rankDiff !== 0) return rankDiff;
  return (a.authorName || '').localeCompare(b.authorName || '');
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
