import {SortField} from '$lib/dotnet-types';

export type SortDirection = 'asc' | 'desc';

export const sortOptions = [
  {field: SortField.SearchRelevance, dir: 'asc'},
  {field: SortField.Headword, dir: 'asc'},
  {field: SortField.Headword, dir: 'desc'}
] as const;

export interface SortConfig {
  field: SortField;
  dir: SortDirection;
  /**
   * Writing system to sort by (and, for headword sorts, to display in the entry list).
   * Chosen separately via the writing-system pill; undefined means the project's
   * default vernacular — the historical behavior.
   */
  writingSystem?: string;
}
