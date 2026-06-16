import {ActivitySort, type IActivityAuthor, type IActivityQuery, type IProjectActivity} from '$lib/dotnet-types';

export const ALL_AUTHORS = '__all__';
export const UNKNOWN_AUTHOR = '__unknown__';
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

export type ActivityFilters = {
  authorFilter: string;
  changeTypeFilter: string;
  excludeFieldWorks: boolean;
  sort: ActivitySort;
};

export function createDefaultActivityFilters(): ActivityFilters {
  return {
    authorFilter: ALL_AUTHORS,
    changeTypeFilter: ALL_CHANGE_TYPES,
    excludeFieldWorks: false,
    sort: ActivitySort.NewestFirst,
  };
}

export function toServerQuery(filters: ActivityFilters): IActivityQuery {
  return {
    ...parseAuthorFilter(filters.authorFilter),
    excludeFieldWorks: filters.excludeFieldWorks,
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

export function authorFilterKey(author: IActivityAuthor): string {
  if (!author.authorId && !author.authorName) return UNKNOWN_AUTHOR;
  if (author.authorId) return author.authorId;
  return `name:${author.authorName}`;
}

export function parseAuthorFilter(key: string): Pick<IActivityQuery, 'authorId' | 'authorName'> {
  if (key === ALL_AUTHORS) return {};
  if (key === UNKNOWN_AUTHOR) return {authorId: ''};
  if (key.startsWith('name:')) return {authorName: key.slice(5)};
  return {authorId: key};
}

export function filterActivityByChangeType(
  activities: IProjectActivity[],
  changeTypeKey: string,
): IProjectActivity[] {
  if (changeTypeKey === ALL_CHANGE_TYPES) return activities;
  return activities.filter(a => a.changeTypes?.includes(changeTypeKey));
}
