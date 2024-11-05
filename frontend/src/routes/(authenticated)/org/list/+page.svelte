<script lang="ts">
  import { FilterBar } from '$lib/components/FilterBar';
  import type { OrgListPageQuery } from '$lib/gql/types';
  import t, { date, number } from '$lib/i18n';
  import { Icon } from '$lib/icons';
  import { HeaderPage } from '$lib/layout';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import type { PageData } from './$types';
  import type { OrgListSearchParams } from './+page';
  import orderBy from 'just-order-by';

  export let data: PageData;
  $: orgs = data.orgs;
  $: myOrgsMap = data.myOrgsMap;

  const queryParams = getSearchParams<OrgListSearchParams>({
    search: queryParam.string<string>(''),
  });
  const { queryParamValues, defaultQueryParamValues } = queryParams;

  type OrgList = OrgListPageQuery['orgs']
  type Org = OrgList[number];

  type Column = keyof Pick<Org, 'name' | 'memberCount' | 'createdDate'>;
  let sortColumn: Column = 'name';
  type Dir = 'asc' | 'desc';
  let sortDir: Dir = 'asc';

  function swapSortDir(): void {
    sortDir = sortDir === 'asc' ? 'desc' : 'asc';
  }

  function handleSortClick(clickedColumn: Column): void {
    if (sortColumn === clickedColumn) {
      swapSortDir();
    } else {
      sortColumn = clickedColumn;
      sortDir = clickedColumn === 'name' ? 'asc' : 'desc';
    }
  }

  function filterOrgs(orgs: OrgList, search: string): OrgList {
    return orgs.filter((org) => org.name.toLowerCase().includes(search.toLowerCase()));
  }

  function lowerCaseName(org: Org): string {
    return org.name.toLocaleLowerCase();
  }

  function sortOrgs(orgs: OrgList, sortColumn: Column, sortDir: Dir): OrgList {
    return orderBy(orgs, [
      {
        property: sortColumn == 'name' ? lowerCaseName : sortColumn,
        order: sortDir,
      },
      { property: lowerCaseName },
    ]);
  }

  $: filteredOrgs = $orgs ? filterOrgs($orgs, $queryParamValues.search) : [];
  $: displayOrgs = sortOrgs(filteredOrgs, sortColumn, sortDir);
  $: filtering = filteredOrgs.length !== $orgs.length;

  let myOrgs: OrgList = [];
  let otherOrgs: OrgList = [];
  $: {
    myOrgs = [];
    otherOrgs = [];
    displayOrgs.forEach(org => {
      if ($myOrgsMap.has(org.id)) {
        myOrgs.push(org);
      } else {
        otherOrgs.push(org);
      }
    });
  }
</script>

<!--
TODO:

* Sort options: name, created date, # users
* Paging
-->

<HeaderPage wide titleText={$t('org.table.title')}>
  <svelte:fragment slot="actions">
    <AdminContent>
      <a href="/org/create" class="btn btn-success">
        {$t('org.create.title')}
        <span class="i-mdi-plus text-2xl" />
      </a>
    </AdminContent>
  </svelte:fragment>
  <svelte:fragment slot="title">
    {$t('org.table.title')}
    <Icon icon="i-mdi-account-group-outline" size="text-5xl" y="10%" />
  </svelte:fragment>
  <svelte:fragment slot="headerContent">
    <FilterBar
      searchKey="search"
      filterKeys={['search']}
      filters={queryParamValues}
      filterDefaults={defaultQueryParamValues}
    />
  </svelte:fragment>
  <div class="overflow-x-auto @container scroll-shadow">
    <table class="table table-lg">
      <thead>
        <tr class="bg-base-200">
          <th on:click={() => handleSortClick('name')} class="cursor-pointer hover:bg-base-300">
            {$t('org.table.name')}
            <span class:invisible={sortColumn !== 'name'}  class="{`i-mdi-sort-${sortDir}ending`} text-xl align-[-5px] ml-2" />
          </th>
          <th on:click={() => handleSortClick('memberCount')} class="cursor-pointer hover:bg-base-300 hidden @md:table-cell">
            {$t('org.table.members')}
            <span class:invisible={sortColumn !== 'memberCount'} class="{`i-mdi-sort-${sortDir}ending`} text-xl align-[-5px] ml-2" />
          </th>
          <th on:click={() => handleSortClick('createdDate')} class="cursor-pointer hover:bg-base-300 hidden @xl:table-cell">
            {$t('org.table.created_at')}
            <span class:invisible={sortColumn !== 'createdDate'}  class="{`i-mdi-sort-${sortDir}ending`} text-xl align-[-5px] ml-2" />
          </th>
        </tr>
      </thead>
      <tbody>
        {#if !displayOrgs.length}
          <tr>
            <td colspan="3" class="text-center text-secondary">
              {$t('org.table.no_orgs_found')}
            </td>
          </tr>
        {:else}
          {@const showingMyOrgsHeader = !filtering || myOrgs.length}
          {#if showingMyOrgsHeader}
            <tr>
              <td colspan="3" class="text-sm bg-neutral/75 text-neutral-content py-2">
                {$t('org.table.my_orgs')}
              </td>
            </tr>
            {#if !$myOrgsMap.size}
              <tr>
                <td colspan="3" class="text-center text-secondary">
                  {$t('org.table.not_in_any_orgs')}
                </td>
              </tr>
            {/if}
          {/if}
          {#each [...myOrgs, ...otherOrgs] as org, i}
            {@const isFirstOtherOrg = i === myOrgs.length}
            {#if showingMyOrgsHeader && isFirstOtherOrg}
              <tr>
                <td colspan="3" class="text-sm bg-neutral/75 text-neutral-content py-2">
                  {$t('org.table.other_orgs')}
                </td>
              </tr>
            {/if}
            <tr>
              <td>
                  <a class="link" href={`/org/${org.id}`}>
                    {org.name}
                  </a>
              </td>
              <td class="hidden @md:table-cell">
                {$number(org.memberCount)}
              </td>
              <td class="hidden @xl:table-cell">
                {$date(org.createdDate)}
              </td>
            </tr>
          {/each}
        {/if}
      </tbody>
    </table>
  </div>
</HeaderPage>
