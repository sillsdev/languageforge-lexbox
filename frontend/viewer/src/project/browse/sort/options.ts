import {SortField} from '$lib/dotnet-types';

export const sortOptions = [
  {field: SortField.SearchRelevance, dir: 'asc'},
  {field: SortField.Headword, dir: 'asc'},
  {field: SortField.Headword, dir: 'desc'},
  {field: SortField.Gloss, dir: 'asc'},
  {field: SortField.Gloss, dir: 'desc'},
] as const;

export type SortConfig = typeof sortOptions[number];

/**
 * Gloss sorting expands the list to one row per sense/meaning
 * (backed by getEntrySenseRows rather than getEntries).
 */
export function isSenseRowSort(sort: SortConfig | undefined): boolean {
  return sort?.field === SortField.Gloss;
}
