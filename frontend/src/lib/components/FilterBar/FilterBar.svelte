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
  import type {Writable} from 'svelte/store';
  import {untrack} from 'svelte';
  import Dropdown from '../Dropdown.svelte';
  import {Previous, Debounced, watch} from 'runed';
  import {DEFAULT_DEBOUNCE_TIME} from '$lib/util/time';

  type DumbFilters = $$Generic<Record<string, unknown>>;
  type Filters = DumbFilters & Record<typeof searchKey, string>;

  let searchInput: PlainInput | undefined = $state();

  interface Props {
    searchKey: keyof ConditionalPick<DumbFilters, string>;
    autofocus?: true;
    filters: Writable<Filters>;
    filterDefaults: Filters;
    onFiltersChanged?: OnFiltersChanged;
    hasActiveFilter?: boolean;
    /**
     * Explicitly specify the filter object keys that should be used from the `filters` (optional)
     */
    filterKeys?: Readonly<(keyof Filters)[]>;
    loading?: boolean;
    debounce?: number | boolean;
    activeFilterSlot?: Snippet<[{ activeFilters: Readonly<Filter<Filters>[]> }]>;
    filterSlot?: Snippet;
  }

  let {
    searchKey,
    autofocus,
    filters: allFilters,
    filterDefaults: allFilterDefaults,
    onFiltersChanged,
    hasActiveFilter = $bindable(false),
    filterKeys,
    loading = false,
    debounce = false,
    activeFilterSlot,
    filterSlot,
  }: Props = $props();
  // The input — not the store — owns this value; we only adopt store changes when no write
  // of ours is in flight (see $effect below). Prevents keystroke loss from URL round-trip
  // races in #2224.
  let undebouncedSearch: string | undefined = $state(untrack(() => $allFilters[searchKey]));
  // Object wrapper so `null` (no pending write) is distinct from a pending write of undefined.
  let pendingEcho: { value: string | undefined } | null = null;
  const watcher: () => string | undefined = $derived.by(() => {
    if (debounce === false) return () => undebouncedSearch;
    const debounceTime = debounce === true ? DEFAULT_DEBOUNCE_TIME : debounce;
    // untrack: Debounced's constructor calls the getter once, which would otherwise make
    // this derived depend on undebouncedSearch and rebuild a fresh Debounced — with #current
    // set to the latest value — on every keystroke, defeating debouncing entirely.
    const debouncer = untrack(() => new Debounced(() => undebouncedSearch, debounceTime));
    return () => debouncer.current;
  });
  watch(() => watcher(), (value) => {
    if ($allFilters[searchKey] === value) return;
    pendingEcho = { value };
    $allFilters[searchKey] = value as Filters[typeof searchKey];
  });
  $effect(() => {
    const fromStore = $allFilters[searchKey];
    if (pendingEcho) {
      if (fromStore === pendingEcho.value) pendingEcho = null;
      return;
    }
    untrack(() => {
      if (fromStore !== undebouncedSearch) {
        undebouncedSearch = fromStore as string | undefined;
      }
    });
  });

  function onClearFiltersClick(): void {
    if (!searchInput) return;
    searchInput.clear();
    $allFilters = {
      ...$allFilters,
      ...filterDefaults,
    };
    searchInput.focus();
  }

  function resetFilter(key: string): void {
    $allFilters = {
      ...$allFilters,
      [key]: filterDefaults[key],
    };
  }

  function pickActiveFilters(values: Filters, defaultValues: Filters): Readonly<Filter<Filters>[]> {
    const filters: Filter<Filters>[] = [];
    for (const key in values) {
      const value = values[key];
      if (value !== defaultValues[key]) {
        filters.push({ key, value, clear: () => resetFilter(key) } as Filter<Filters>);
      }
    }
    return Object.freeze(filters);
  }

  let filters = $derived(Object.freeze(filterKeys ? pick($allFilters, filterKeys) : $allFilters));
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
      bind:value={undebouncedSearch}
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
      <!-- The user sees the "undebounced" search value, so the X button should consider that (and not the debounced value) -->
      {#if !!undebouncedSearch || activeFilters.find((f) => f.key !== searchKey)}
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
