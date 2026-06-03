import {Debounced} from 'runed';
import {untrack} from 'svelte';

/**
 * Two-way bindable wrapper that debounces writes from a typed input into a
 * URL-backed filter store, without clobbering in-flight typing when the store
 * echoes the write back: `search.value` updates per keystroke, `filters[key]`
 * after `debounceMs` of idle typing, and external writes to `filters[key]`
 * (deep links, back button, programmatic) sync back into the input. The echo
 * of our own debounced flush — which round-trips through SvelteKit navigation
 * and would otherwise clobber newer typing — is recognised via `pendingEcho`
 * and skipped.
 *
 * Use it like this — coerce nullish input so the value stays a string:
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
    // Read+write `filters[key]` untracked: this effect should only re-fire when
    // the *debounced* local value settles. If we read filters[key] reactively
    // and an external write happens (e.g. the user clicks the clear-X button)
    // we'd re-fire here with the still-stale `flushed.current` and clobber it.
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
