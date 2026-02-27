import {SortField} from '$lib/dotnet-types';

export const sortOptions = [
  {field: SortField.SearchRelevance, dir: 'asc'},
  {field: SortField.Headword, dir: 'asc'},
  {field: SortField.Headword, dir: 'desc'}
] as const;

export type SortConfig = typeof sortOptions[number];
