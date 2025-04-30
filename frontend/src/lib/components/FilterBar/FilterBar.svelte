<script lang="ts" module>
  import type { Snippet } from 'svelte';
  export type Filter<T = Record<string, unknown>> = Readonly<
    NonNullable<
      {
        [K in keyof T]: { value: T[K]; key: K & string; clear: () => void };
      }[keyof T]
    >
  >;
</script>

<script lang="ts">
  import { run } from 'svelte/legacy';

  import { createEventDispatcher } from 'svelte';
  import type { ConditionalPick } from 'type-fest';
  import Loader from '$lib/components/Loader.svelte';
  import { PlainInput } from '$lib/forms';
  import { pick } from '$lib/util/object';
  import t from '$lib/i18n';
  import type { Writable } from 'svelte/store';
  import Dropdown from '../Dropdown.svelte';

  type DumbFilters = $$Generic<Record<string, unknown>>;
  // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents
  type Filters = DumbFilters & Record<typeof searchKey, string>;

  const dispatch = createEventDispatcher<{
    change: Readonly<Filter<Filters>[]>;
  }>();

  let searchInput: PlainInput = $state()!;

  interface Props {
    searchKey: keyof ConditionalPick<DumbFilters, string>;
    autofocus?: true | undefined;
    filters: Writable<Filters>;
    filterDefaults: Filters;
    hasActiveFilter?: boolean;
    /**
     * Explicitly specify the filter object keys that should be used from the `filters` (optional)
     */
    filterKeys?: Readonly<(keyof Filters)[]> | undefined;
    loading?: boolean;
    debounce?: number | boolean;
    debouncing?: boolean;
    activeFilterSlot?: Snippet<unknown[]>;
    filterSlot?: Snippet;
  }

  let {
    searchKey,
    autofocus = undefined,
    filters: allFilters,
    filterDefaults: allFilterDefaults,
    hasActiveFilter = $bindable(false),
    filterKeys = undefined,
    loading = false,
    debounce = $bindable(false),
    debouncing = $bindable(false),
    activeFilterSlot,
    filterSlot,
  }: Props = $props();
  let undebouncedSearch: string | undefined = $state(undefined);

  let activeFilters: Readonly<Filter<Filters>[]> = $state([]);

  function onClearFiltersClick(): void {
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
  run(() => {
    const currFilters = activeFilters;
    const newFilters = pickActiveFilters(filters, filterDefaults);
    if (JSON.stringify(currFilters) !== JSON.stringify(newFilters)) {
      activeFilters = newFilters;
      dispatch('change', activeFilters);
    }
  });
  run(() => {
    hasActiveFilter = activeFilters.length > 0;
  });
</script>

<div class="input filter-bar input-bordered flex items-center gap-2 py-1.5 px-2 flex-wrap h-[unset] min-h-12">
  {@render activeFilterSlot?.({ activeFilters })}
  <div class="flex grow">
    <PlainInput
      bind:value={$allFilters[searchKey]}
      bind:debounce
      bind:debouncing
      bind:undebouncedValue={undebouncedSearch}
      bind:this={searchInput}
      placeholder={$t('filter.placeholder')}
      style="seach-input border-none h-8 px-1 focus:outline-none min-w-[120px] flex-grow"
      {autofocus}
    />
    <div class="ml-auto flex join">
      {#if loading || debouncing}
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
