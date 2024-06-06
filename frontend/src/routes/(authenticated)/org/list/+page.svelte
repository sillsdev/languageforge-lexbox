<script lang="ts">
  import { FilterBar } from '$lib/components/FilterBar';
  import type { OrgListPageQuery } from '$lib/gql/types';
  import t, { date, number } from '$lib/i18n';
  import { Icon } from '$lib/icons';
  import { Page } from '$lib/layout';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import type { PageData } from './$types';
  import type { OrgListSearchParams } from './+page';

  export let data: PageData;
  $: orgs = data.orgs;

  const queryParams = getSearchParams<OrgListSearchParams>({
    search: queryParam.string<string>(''),
  });
  const { queryParamValues, defaultQueryParamValues } = queryParams;

  type OrgList = OrgListPageQuery['orgs']

  type Column = 'name' | 'users' | 'created_at';
  let sortColumn = 'created_at' as Column;
  type Dir = 'ascending' | 'descending';
  let sortDir = 'ascending' as Dir;

  function swapSortDir(): void {
    sortDir = sortDir === 'ascending' ? 'descending' : 'ascending';
  }

  function handleSortClick(clickedColumn: Column): void {
    if (sortColumn === clickedColumn) {
      swapSortDir();
    } else {
      sortColumn = clickedColumn;
      sortDir = 'ascending';
    }
  }

  function filterOrgs(orgs: OrgList, search: string): OrgList {
    return orgs.filter((org) => org.name.toLowerCase().includes(search.toLowerCase()));
  }

  function sortOrgs(orgs: OrgList, sortColumn: Column, sortDir: Dir): OrgList {
    const data = [... orgs];
    let mult = sortDir === 'ascending' ? 1 : -1;
    data.sort((a, b) => {
      if (sortColumn === 'users') {
        return (a.members.length - b.members.length) * mult;
      } else if (sortColumn === 'name') {
        const comp = a.name < b.name ? -1 : a.name > b.name ? 1 : 0;
        return comp * mult;
      } else if (sortColumn === 'created_at') {
        const comp = a.createdDate < b.createdDate ? -1 : a.createdDate > b.createdDate ? 1 : 0;
        return comp * mult;
      }
      return 0;
    });
    return data;
  }

  $: filteredOrgs = $orgs ? filterOrgs($orgs, $queryParamValues.search) : [];
  $: displayOrgs = sortOrgs(filteredOrgs, sortColumn, sortDir);
</script>

<!--
TODO:

* Sort options: name, created date, # users
* Paging
-->

<Page wide>
  <h1 class="text-3xl text-left grow max-w-full mb-4 flex gap-4 items-center">
    Organisations
    <Icon icon="i-mdi-account-group-outline" size="text-5xl" />
  </h1>

  <div class="mt-4">
    <FilterBar
      searchKey="search"
      filterKeys={['search']}
      filters={queryParamValues}
      filterDefaults={defaultQueryParamValues}
    />
  </div>

  <div class="divider" />
  <div class="overflow-x-auto @container scroll-shadow">
    <table class="table table-lg">
      <thead>
        <tr class="bg-base-200">
          <th on:click={() => handleSortClick('name')} class="cursor-pointer">
            {$t('project.table.name')}
            {#if sortColumn === 'name'}
            <span class="{`i-mdi-sort-${sortDir}`} text-xl align-[-5px] ml-2" />
            {/if}
          </th>
          <th on:click={() => handleSortClick('users')} class="cursor-pointer hidden @md:table-cell">
            {$t('project.table.users')}
            {#if sortColumn === 'users'}
            <span class="{`i-mdi-sort-${sortDir}`} text-xl align-[-5px] ml-2" />
            {/if}
          </th>
          <th on:click={() => handleSortClick('created_at')} class="cursor-pointer hidden @xl:table-cell">
              {$t('project.table.created_at')}
              {#if sortColumn === 'created_at'}
              <span class="{`i-mdi-sort-${sortDir}`} text-xl align-[-5px] ml-2" />
              {/if}
          </th>
        </tr>
      </thead>
      <tbody>
        {#each displayOrgs as org}
          <tr>
            <td>
              <span class="flex gap-2 items-center">
                  <a class="link" href={`/org/${org.id}`}>
                    {org.name}
                  </a>
              </span>
            </td>
            <td class="hidden @md:table-cell">
              {$number(org.members.length)}
            </td>
            <td class="hidden @xl:table-cell">
              {$date(org.createdDate)}
            </td>
          </tr>
        {/each}
      </tbody>
    </table>
  </div>
</Page>
