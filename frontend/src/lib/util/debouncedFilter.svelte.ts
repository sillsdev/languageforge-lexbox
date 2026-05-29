import {Debounced} from 'runed';
import {untrack} from 'svelte';

/**
 * Two-way bindable wrapper that debounces writes from a typed input into a
 * URL-backed filter store, without clobbering in-flight typing when the store
 * echoes the write back.
 *
 * Use it like this:
 * ```svelte
 * const search = debouncedFilter(filters, 'userSearch', 400);
 * <input bind:value={search.value} />
 * ```
 *
 * Behaviour:
 * - Every keystroke updates `search.value` immediately — the input never lags.
 * - The upstream `filters[key]` only updates after `debounceMs` of idle typing,
 *   which is what keeps server-filtered queries (like `userSearch`) from
 *   firing per character.
 * - External writes to `filters[key]` (e.g. `onUserCreated` setting
 *   `filters.userSearch = newEmail`, or a deep link, or the back button) flow
 *   back into the input — *unless* the write is the echo of our own debounced
 *   flush, in which case we ignore it.
 *
 * The echo-suppression is the subtle bit. When we write `filters.userSearch =
 * 'abc'`, the URL-backed store roundtrips through SvelteKit navigation; the
 * value comes back to us via the reactive read of `filters[key]` and would
 * normally clobber whatever the user has typed in the meantime. We track the
 * value of our last debounced write in `pendingEcho`, and skip the external-
 * sync effect until that exact value echoes back through.
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
    if (filters[key] === value) return;
    pendingEcho = value;
    filters[key] = value;
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
