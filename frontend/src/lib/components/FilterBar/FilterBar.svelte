<script lang="ts" context="module">
  export type Filter<T = Record<string, unknown>> = Readonly<NonNullable<
    {
      [K in keyof T]: { value: T[K]; key: K & string, clear: () => void };
    }[keyof T]
  >>;
</script>

<script lang="ts">
  import Input from '$lib/forms/Input.svelte';

  import { createEventDispatcher } from 'svelte';
  import type { ConditionalPick } from 'type-fest';
  import Loader from '$lib/components/Loader.svelte';
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

  let searchInput: Input;

  export let searchKey: keyof ConditionalPick<DumbFilters, string>;
  export let autofocus: true | undefined = undefined;
  let allFilters: Writable<Filters>;
  export { allFilters as filters };
  let allFilterDefaults: Filters;
  export { allFilterDefaults as filterDefaults };
  export let hasActiveFilter: boolean = false;

  /**
   * Explicitly specify the filter object keys that should be used from the `filters` (optional)
   */
  export let filterKeys: Readonly<(keyof Filters)[]> | undefined = undefined;

  export let loading = false;
  export let debounce: number | boolean = false;
  export let debouncing = false;
  let undebouncedSearch: string | undefined = undefined;

  $: filters = Object.freeze(filterKeys ? pick($allFilters, filterKeys) : $allFilters);
  $: filterDefaults = Object.freeze(filterKeys ? pick(allFilterDefaults, filterKeys) : allFilterDefaults);
  let activeFilters: Readonly<Filter<Filters>[]>;
  $: {
    const currFilters = activeFilters;
    const newFilters = pickActiveFilters(filters, filterDefaults);
    if (JSON.stringify(currFilters) !== JSON.stringify(newFilters)) {
      activeFilters = newFilters;
      dispatch('change', activeFilters);
    }
  }
  $: hasActiveFilter = activeFilters.length > 0;

  function reseFilters(): void {
    searchInput.clear();
    $allFilters = {
      ...$allFilters,
      ...filterDefaults,
    }
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
</script>

<div class="input filter-bar input-bordered flex items-center gap-2 py-1.5 px-2 flex-wrap h-[unset] min-h-12">
  <slot name="activeFilters" {activeFilters} />
  <div class="flex grow">
    <Input
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
      {#if !!undebouncedSearch || activeFilters.find(f => f.key !== searchKey)}
        <button class="btn btn-square btn-sm join-item" on:click={reseFilters}>
          <span class="text-lg">✕</span>
        </button>
      {/if}
      {#if $$slots.filters}
        <div class="join-item">
          <Dropdown>
            <!-- svelte-ignore a11y-label-has-associated-control -->
            <label tabindex="-1" class="btn btn-square btn-sm gap-2">
              <span class="i-mdi-filter-outline text-xl" />
            </label>
            <div slot="content" class="card w-[calc(100vw-1rem)] sm:max-w-[35rem]">
              <div class="card-body max-sm:p-4">
                <slot name="filters" />
              </div>
            </div>
          </Dropdown>
        </div>
      {/if}
    </div>
  </div>
</div>

<style lang="postcss">
  .filter-bar:has(.seach-input:focus) {
    /* copied from .input:focus */
    outline-style: solid;
    outline-width: 2px;
    outline-offset: 2px;
    outline-color: oklch(var(--bc) / 0.2);
  }
</style>
