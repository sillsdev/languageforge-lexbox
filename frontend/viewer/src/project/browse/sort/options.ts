import type {SortField} from '$lib/dotnet-types';

export type SortDirection = 'asc' | 'desc';

export interface SortConfig {
  field: SortField;
  dir: SortDirection;
  /**
   * Writing system to sort by (and, for headword sorts, to display in the entry list).
   * Undefined means the project's default vernacular — the historical behavior.
   */
  writingSystem?: string;
}
