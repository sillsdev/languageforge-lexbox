import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {flushSync} from 'svelte';
import {debouncedFilter} from './debouncedFilter.svelte';

const DEBOUNCE_MS = 400;

function withFilter(initial: string, body: (ctx: {filters: {search: string}; search: {value: string}}) => void): void {
  const filters = $state<{search: string}>({search: initial});
  let error: unknown;
  const cleanup = $effect.root(() => {
    const search = debouncedFilter(filters, 'search', DEBOUNCE_MS);
    flushSync();
    try {
      body({filters, search});
    } catch (e) {
      error = e;
    }
  });
  cleanup();
  if (error) throw error;
}

// Echo-suppression vs the async URL round-trip is an E2E concern — an in-memory
// store can't re-notify on navigation. See debouncedFilter.svelte.ts.
describe('debouncedFilter', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('reflects typing immediately but debounces the write upstream', () => {
    withFilter('', ({filters, search}) => {
      search.value = 'abc';
      flushSync();
      expect(search.value).toBe('abc');
      expect(filters.search).toBe('');

      vi.advanceTimersByTime(DEBOUNCE_MS);
      flushSync();
      expect(filters.search).toBe('abc');
    });
  });

  it('coalesces rapid edits into a single settled write', () => {
    withFilter('', ({filters, search}) => {
      const writes: string[] = [];
      $effect(() => {
        writes.push(filters.search);
      });
      flushSync();

      search.value = 'a';
      flushSync();
      vi.advanceTimersByTime(DEBOUNCE_MS / 2);
      search.value = 'ab';
      flushSync();
      vi.advanceTimersByTime(DEBOUNCE_MS / 2);
      search.value = 'abc';
      flushSync();

      vi.advanceTimersByTime(DEBOUNCE_MS);
      flushSync();

      expect(search.value).toBe('abc');
      // only the initial value and the final settled value reach the store — no 'a'/'ab' in between
      expect(writes).toEqual(['', 'abc']);
    });
  });

  it('syncs an external store write back into the input', () => {
    withFilter('', ({filters, search}) => {
      filters.search = 'external';
      flushSync();
      expect(search.value).toBe('external');
    });
  });

  it('lets an external write during pending typing win over the stale debounce', () => {
    withFilter('', ({filters, search}) => {
      search.value = 'abc';
      flushSync();
      vi.advanceTimersByTime(DEBOUNCE_MS / 2);

      filters.search = 'external';
      flushSync();
      expect(search.value).toBe('external');

      vi.advanceTimersByTime(DEBOUNCE_MS);
      flushSync();
      expect(search.value).toBe('external');
      expect(filters.search).toBe('external');
    });
  });

  it('seeds from the initial store value and syncs an external clear back in', () => {
    withFilter('preset', ({filters, search}) => {
      expect(search.value).toBe('preset');

      filters.search = '';
      flushSync();
      expect(search.value).toBe('');
    });
  });
});
