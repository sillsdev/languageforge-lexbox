<script  context="module" lang="ts">
  import type { User } from '$lib/gql/types';

  export type UserFilter = {
    userSearch: string;
    usersICreated: boolean;
  }

  export function filterUsers(
    users: User[],
    userFilter: Partial<UserFilter>,
  ): User[] {
    return users.filter(
      () =>
        (!userFilter.usersICreated)
    );
  }
</script>

<script lang="ts">
  import type { Writable } from 'svelte/store';
  import FilterBar from '$lib/components/FilterBar/FilterBar.svelte';
  import ActiveFilter from '../FilterBar/ActiveFilter.svelte';
  import { Icon } from '$lib/icons';
  // import t from '$lib/i18n';


  type Filters = Partial<UserFilter> & Pick<UserFilter, 'userSearch'>;
  export let filters: Writable<Filters>;
  export let filterDefaults: Filters;
  export let hasActiveFilter: boolean = false;
  export let autofocus: true | undefined = undefined;
  export let filterKeys: (keyof Filters)[] = ['userSearch', 'usersICreated'];
  export let loading = false;

  function filterEnabled(filter: keyof Filters): boolean {
    return filterKeys.includes(filter);
  }
</script>

<FilterBar on:change searchKey="userSearch" {autofocus} {filters} {filterDefaults} bind:hasActiveFilter {filterKeys} {loading}>
  <svelte:fragment slot="activeFilters" let:activeFilters>
    {#each activeFilters as filter}
      {#if filter.key === 'usersICreated' && filter.value}
        <ActiveFilter {filter}>
          <Icon icon="i-mdi-account-plus-outline" />
          {'Users I Created'}
        </ActiveFilter>
      {/if}
    {/each}
  </svelte:fragment>
  <svelte:fragment slot="filters">
    <h2 class="card-title">{'User Filters'}</h2>
    {#if filterEnabled('usersICreated')}
      <div class="form-control">
        <label class="cursor-pointer label gap-4">
          <span class="label-text inline-flex items-center gap-2">
            {'Users I Created'}
            <Icon icon="i-mdi-account-plus-outline" color="text-warning" />
          </span>
          <input bind:checked={$filters.usersICreated} type="checkbox" class="toggle toggle-warning" />
        </label>
      </div>
    {/if}
  </svelte:fragment>
</FilterBar>
