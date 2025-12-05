/**
 * VirtualListHelper - Stateless utilities for infinite-scroll virtual lists
 * 
 * Provides pure functions for:
 * - Padding placeholder entries for accurate scrollbar representation
 * - Smart scroll detection to determine when to load more entries
 * - Scroll position calculations
 */

import type { IEntry } from '$lib/dotnet-types';
import { MorphType } from '$lib/dotnet-types';

export interface VirtualListConfig {
  pageSize?: number;
  loadThreshold?: number;
}

export class VirtualListHelper {
  pageSize: number;
  loadThreshold: number;

  constructor(config?: VirtualListConfig) {
    this.pageSize = config?.pageSize ?? 100;
    this.loadThreshold = config?.loadThreshold ?? 20;
  }

  /**
   * Create padded entries array with placeholders for accurate scrollbar
   * The padded array makes VList's scrollbar proportional to the full list
   */
  createPaddedEntries(
    loadedEntries: IEntry[],
    windowOffset: number,
    totalCount: number,
  ): IEntry[] {
    if (totalCount <= loadedEntries.length) {
      // All entries loaded or total is less than loaded
      return loadedEntries;
    }

    // Create array with loaded entries + placeholder entries
    const result: IEntry[] = [];

    // Add placeholder entries before the window
    for (let i = 0; i < windowOffset; i++) {
      result.push(createPlaceholder(`placeholder-before-${i}`));
    }

    // Add actual loaded entries
    result.push(...loadedEntries);

    // Add placeholder entries after the window
    const remainingAfter = totalCount - (windowOffset + loadedEntries.length);
    for (let i = 0; i < remainingAfter; i++) {
      result.push(createPlaceholder(`placeholder-after-${i}`));
    }

    return result;
  }

  /**
   * Determine scroll load direction based on current window
   * Returns: 'none' | 'before' | 'after'
   */
  getLoadDirection(
    startIndex: number,
    endIndex: number,
    windowOffset: number,
    loadedCount: number,
    totalCount: number,
  ): 'none' | 'before' | 'after' {
    const windowStart = windowOffset;
    const windowEnd = windowOffset + loadedCount;

    // If scrolled before window, need to load before
    if (startIndex < windowStart && windowStart > 0) {
      return 'before';
    }

    // If scrolled after window, need to load after
    if (endIndex >= windowEnd && windowEnd < totalCount) {
      return 'after';
    }

    return 'none';
  }

  /**
   * Check if should prefetch more entries (LOAD_THRESHOLD logic)
   */
  shouldPrefetch(
    startIndex: number,
    endIndex: number,
    windowOffset: number,
    loadedCount: number,
    totalCount: number,
  ): 'before' | 'after' | null {
    const windowStart = windowOffset;
    const windowEnd = windowOffset + loadedCount;

    // Prefetch before if near start
    if (startIndex < windowStart + this.loadThreshold && windowStart > 0) {
      return 'before';
    }

    // Prefetch after if near end
    if (endIndex > windowEnd - this.loadThreshold && windowEnd < totalCount) {
      return 'after';
    }

    return null;
  }

  /**
   * Calculate target index within the window (for scrolling to a specific entry)
   */
  calculateLocalIndex(globalIndex: number, windowOffset: number): number {
    return globalIndex - windowOffset;
  }
}

/**
 * Create a placeholder entry for unloaded regions
 * Placeholders are rendered as empty divs but take up space in the list
 */
function createPlaceholder(id: string): IEntry {
  return {
    id,
    headword: { default: '' },
    lexemeForm: {},
    citationForm: {},
    literalMeaning: {},
    morphType: MorphType.Root,
    senses: [],
    complexFormTypes: [],
    note: {},
    components: [],
    complexForms: [],
    publishIn: [],
  } as IEntry;
}
