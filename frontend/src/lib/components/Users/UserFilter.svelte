<script  context="module" lang="ts">
  export type UserType = 'admin' | 'nonAdmin' | 'guest' | undefined;

  export type UserFilters = {
    userSearch: string;
    usersICreated: boolean;
    userType: UserType;
  }
</script>

<script lang="ts">
  import type { Writable } from 'svelte/store';
  import FilterBar from '$lib/components/FilterBar/FilterBar.svelte';
  import ActiveFilter from '$lib/components/FilterBar/ActiveFilter.svelte';
  import { Icon } from '$lib/icons';
  import t from '$lib/i18n';
  import UserTypeSelect from '$lib/forms/UserTypeSelect.svelte';


  type Filters = Partial<UserFilters> & Pick<UserFilters, 'userSearch'>;
  export let filters: Writable<Filters>;
  export let filterDefaults: Filters;
  export let hasActiveFilter: boolean = false;
  export let autofocus: true | undefined = undefined;
  export let filterKeys: ReadonlyArray<(keyof Filters)> = ['userSearch', 'usersICreated', 'userType'];
  export let loading = false;

  function filterEnabled(filter: keyof Filters): boolean {
    return filterKeys.includes(filter);
  }
</script>

<FilterBar
  debounce
  on:change
  searchKey="userSearch"
  {autofocus}
  {filters}
  {filterDefaults}
  bind:hasActiveFilter
  {filterKeys}
  {loading}
>
  <svelte:fragment slot="activeFilterSlot" let:activeFilters>
    {#each activeFilters as filter}
      {#if filter.key === 'userType' && filter.value}
        <ActiveFilter {filter}>
          {#if filter.value === 'admin'}
            <Icon icon="i-mdi-security" color="text-accent" />
            {$t('admin_dashboard.user_filter.user_type.admin')}
          {:else if filter.value === 'nonAdmin'}
            <Icon icon="i-mdi-account" />
            {$t('admin_dashboard.user_filter.user_type.nonAdmin')}
          {:else if filter.value === 'guest'}
            <Icon icon="i-mdi-account-outline" />
            {$t('admin_dashboard.user_filter.user_type.guest')}
          {/if}
        </ActiveFilter>
      {:else if filter.key === 'usersICreated' && filter.value}
        <ActiveFilter {filter}>
          <Icon icon="i-mdi-account-plus-outline" color="text-warning" />
          {$t('admin_dashboard.user_filter.guest_users_i_added')}
        </ActiveFilter>
      {/if}
    {/each}
  </svelte:fragment>
  <svelte:fragment slot="filterSlot">
    <h2 class="card-title">{$t('admin_dashboard.user_filter.title')}</h2>
    {#if filterEnabled('userType')}
      <div class="form-control">
        <UserTypeSelect bind:value={$filters.userType} undefinedOptionLabel={$t('common.any')}/>
      </div>
    {/if}
    {#if filterEnabled('usersICreated')}
      <div class="form-control">
        <label class="cursor-pointer label gap-4">
          <span class="label-text inline-flex items-center gap-2">
            {$t('admin_dashboard.user_filter.guest_users_i_added')}
            <Icon icon="i-mdi-account-plus-outline" color="text-warning" />
          </span>
          <input bind:checked={$filters.usersICreated} type="checkbox" class="toggle toggle-warning" />
        </label>
      </div>
    {/if}
  </svelte:fragment>
</FilterBar>
