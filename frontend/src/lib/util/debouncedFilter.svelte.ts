import {Debounced} from 'runed';
import {untrack} from 'svelte';

/**
 * Two-way bindable wrapper between a text input and a URL-backed filter store:
 * `value` updates per keystroke, `filters[key]` after `debounceMs` of idle typing,
 * and external writes to `filters[key]` (deep links, back button, programmatic)
 * sync back into the input — without the store's echo of our own debounced write
 * clobbering newer typing.
 *
 * Coerce nullish input so the value stays a string:
 * ```svelte
 * const search = debouncedFilter(filters, 'userSearch', 400);
 * <input bind:value={() => search.value, (v) => (search.value = v ?? '')} />
 * ```
 */
export function debouncedFilter<T extends Record<string, unknown>, K extends keyof T>(
  filters: T,
  key: K,
  debounceMs: number,
): {value: T[K]} {
  let local: T[K] = $state(untrack(() => filters[key]));
  let pendingEcho: T[K] | undefined = undefined;

  // Flush typing → upstream store, debounced.
  const flushed = new Debounced(() => local, debounceMs);
  $effect(() => {
    const value = flushed.current;
    // untracked: re-firing on external filters[key] writes (e.g. the clear-✕ button)
    // would replay the still-stale flushed.current over them
    untrack(() => {
      if (filters[key] === value) return;
      pendingEcho = value;
      filters[key] = value;
    });
  });

  // Sync upstream changes back into local, except when echoing our own write.
  $effect(() => {
    const fromStore = filters[key];
    untrack(() => {
      if (fromStore === pendingEcho) {
        pendingEcho = undefined;
      } else if (pendingEcho === undefined && fromStore !== local) {
        local = fromStore;
      }
    });
  });

  return {
    get value() {
      return local;
    },
    set value(v: T[K]) {
      local = v;
    },
  };
}
