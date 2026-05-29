<script lang="ts" module>
  import type {Snippet} from 'svelte';
  export type Filter<T = Record<string, unknown>> = Readonly<
    NonNullable<
      {
        [K in keyof T]: { value: T[K]; key: K & string; clear: () => void };
      }[keyof T]
    >
  >;
  export type OnFiltersChanged = (newFilters: Readonly<Filter[]>) => void;
</script>

<script lang="ts">
  import type {ConditionalPick} from 'type-fest';
  import Loader from '$lib/components/Loader.svelte';
  import {PlainInput} from '$lib/forms';
  import {pick} from '$lib/util/object';
  import t from '$lib/i18n';
  import Dropdown from '../Dropdown.svelte';
  import {Previous} from 'runed';
  import {untrack} from 'svelte';
  import {DEFAULT_DEBOUNCE_TIME} from '$lib/util/time';
  import {debouncedFilter} from '$lib/util/debouncedFilter.svelte';

  type DumbFilters = $$Generic<Record<string, unknown>>;
  type Filters = DumbFilters & Record<typeof searchKey, string>;

  let searchInput: PlainInput | undefined = $state();

  interface Props {
    searchKey: keyof ConditionalPick<DumbFilters, string>;
    autofocus?: true;
    filters: Filters;
    filterDefaults: Filters;
    onFiltersChanged?: OnFiltersChanged;
    hasActiveFilter?: boolean;
    /**
     * Explicitly specify the filter object keys that should be used from the `filters` (optional)
     */
    filterKeys?: Readonly<(keyof Filters)[]>;
    loading?: boolean;
    /**
     * ms between the last keystroke and the URL/store write. Defaults to
     * `DEFAULT_DEBOUNCE_TIME`. Set to `0` to write through immediately
     * (appropriate for pure client-side filters that don't trigger server
     * fetches; for server-filtered inputs like the user-list search, keep
     * the default so we don't hammer load() per character).
     */
    debounceMs?: number;
    activeFilterSlot?: Snippet<[{ activeFilters: Readonly<Filter<Filters>[]> }]>;
    filterSlot?: Snippet;
  }

  let {
    searchKey,
    autofocus,
    filters,
    filterDefaults: allFilterDefaults,
    onFiltersChanged,
    hasActiveFilter = $bindable(false),
    filterKeys,
    loading = false,
    debounceMs = DEFAULT_DEBOUNCE_TIME,
    activeFilterSlot,
    filterSlot,
  }: Props = $props();

  // Two-way binding for the search input: typing updates `search.value`
  // immediately, the upstream `filters[searchKey]` updates `debounceMs` after
  // the last keystroke, and external writes to the upstream store flow back
  // into the input. See debouncedFilter.svelte.ts for the echo-suppression
  // mechanics that keep in-flight typing from being clobbered by the round-trip.
  //
  // `untrack` here just tells Svelte we intentionally bind these props once
  // at setup — they're stable for the lifetime of the component instance.
  const search = untrack(() => debouncedFilter(filters, searchKey, debounceMs));

  function onClearFiltersClick(): void {
    for (const key of Object.keys(filterDefaults)) {
      (filters as Record<string, unknown>)[key] = (filterDefaults as Record<string, unknown>)[key];
    }
    searchInput?.focus();
  }

  function resetFilter(key: string): void {
    (filters as Record<string, unknown>)[key] = (filterDefaults as Record<string, unknown>)[key];
  }

  function pickActiveFilters(values: Filters, defaultValues: Filters): Readonly<Filter<Filters>[]> {
    const filters: Filter<Filters>[] = [];
    // Iterate from defaults (plain object) — runed's proxy doesn't expose schema keys via for…in.
    for (const key of Object.keys(defaultValues)) {
      const value = (values as Record<string, unknown>)[key];
      const fromDefault = (defaultValues as Record<string, unknown>)[key];
      // Treat null and undefined as the same "unset" — runed stores unset URL
      // params as null while our defaults object has undefined.
      if (value == null && fromDefault == null) continue;
      if (value !== fromDefault) {
        filters.push({ key, value, clear: () => resetFilter(key) } as Filter<Filters>);
      }
    }
    return Object.freeze(filters);
  }

  let filterDefaults = $derived(Object.freeze(filterKeys ? pick(allFilterDefaults, filterKeys) : allFilterDefaults));
  let activeFilters: Readonly<Filter<Filters>[]> = $derived(pickActiveFilters(filters, filterDefaults));
  let prevActiveFilters = new Previous(() => activeFilters, []);
  $effect(() => {
    hasActiveFilter = activeFilters.length > 0;
  });
  $effect(() => {
    if (JSON.stringify(prevActiveFilters.current) !== JSON.stringify(activeFilters)) {
      onFiltersChanged?.(activeFilters);
    }
  });
</script>

<div class="input filter-bar input-bordered flex items-center gap-2 py-1.5 px-2 flex-wrap h-[unset] min-h-12">
  {@render activeFilterSlot?.({ activeFilters })}
  <div class="flex grow">
    <PlainInput
      bind:value={() => search.value, (v) => (search.value = v ?? '')}
      bind:this={searchInput}
      placeholder={$t('filter.placeholder')}
      style="seach-input border-none h-8 px-1 focus:outline-none min-w-[120px] flex-grow"
      {autofocus}
    />
    <div class="ml-auto flex join">
      {#if loading}
        <div class="flex mr-2">
          <Loader loading />
        </div>
      {/if}
      <!-- show the X if the input has unflushed typed text, or any non-search filter is active -->
      {#if !!search.value || activeFilters.find((f) => f.key !== searchKey)}
        <button class="btn btn-square btn-sm join-item" onclick={onClearFiltersClick}>
          <span class="text-lg">✕</span>
        </button>
      {/if}
      {#if filterSlot}
        <div class="join-item">
          <Dropdown>
            <button class="btn btn-square join-item btn-sm gap-2" aria-label={$t('filter.aria_open_filters')}>
              <span class="i-mdi-filter-outline text-xl"></span>
            </button>
            {#snippet content()}
              <div class="card w-[calc(100vw-1rem)] sm:max-w-[35rem]">
                <div class="card-body max-sm:p-4">
                  {@render filterSlot?.()}
                </div>
              </div>
            {/snippet}
          </Dropdown>
        </div>
      {/if}
    </div>
  </div>
</div>

<style lang="postcss">
  .filter-bar:has(:global(.seach-input):focus) {
    /* copied from .input:focus */
    outline-style: solid;
    outline-width: 2px;
    outline-offset: 2px;
    outline-color: oklch(var(--bc) / 0.2);
  }
</style>
