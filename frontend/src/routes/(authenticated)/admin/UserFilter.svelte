<script  context="module" lang="ts">
  import type { User } from './+page';

  export type UserFilters = {
    userSearch: string;
    usersICreated: boolean;
  }

  export function filterUsers(
    users: User[],
    userFilter: Partial<UserFilters>,
    adminId: string | undefined,
  ): User[] {
    return users.filter(
      (u) =>
        (!userFilter.usersICreated || u.createdById === adminId)
    );
  }
</script>

<script lang="ts">
  import type { Writable } from 'svelte/store';
  import FilterBar from '$lib/components/FilterBar/FilterBar.svelte';
  import ActiveFilter from '$lib/components/FilterBar/ActiveFilter.svelte';
  import { Icon } from '$lib/icons';
  import t from '$lib/i18n';


  type Filters = Partial<UserFilters> & Pick<UserFilters, 'userSearch'>;
  export let filters: Writable<Filters>;
  export let filterDefaults: Filters;
  export let hasActiveFilter: boolean = false;
  export let autofocus: true | undefined = undefined;
  export let filterKeys: ReadonlyArray<(keyof Filters)> = ['userSearch', 'usersICreated'];
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
  <svelte:fragment slot="activeFilters" let:activeFilters>
    {#each activeFilters as filter}
      {#if filter.key === 'usersICreated' && filter.value}
        <ActiveFilter {filter}>
          <Icon icon="i-mdi-account-plus-outline" color="text-warning" />
          {$t('admin_dashboard.user_filter.guest_users_i_added')}
        </ActiveFilter>
      {/if}
    {/each}
  </svelte:fragment>
  <svelte:fragment slot="filters">
    <h2 class="card-title">{$t('admin_dashboard.user_filter.title')}</h2>
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
