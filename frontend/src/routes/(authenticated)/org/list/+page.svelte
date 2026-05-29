<script lang="ts">
  import {FilterBar} from '$lib/components/FilterBar';
  import type {OrgListPageQuery} from '$lib/gql/types';
  import t, {date, number} from '$lib/i18n';
  import {Icon} from '$lib/icons';
  import {HeaderPage} from '$lib/layout';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import {getSearchParams, queryParam} from '$lib/util/query-params';
  import type {PageData} from './$types';
  import type {OrgListSearchParams} from './+page';
  import orderBy from 'just-order-by';
  import {partition} from '$lib/util/array';
  import {resolve} from '$app/paths';

  interface Props {
    data: PageData;
  }

  const { data }: Props = $props();
  let orgs = $derived(data.orgs);
  let myOrgsMap = $derived(data.myOrgsMap);

  const queryParams = getSearchParams<OrgListSearchParams>({
    search: queryParam.string<string>(''),
  });
  const { queryParamValues, defaultQueryParamValues } = queryParams;

  type OrgList = OrgListPageQuery['orgs'];
  type Org = OrgList[number];

  type Column = keyof Pick<Org, 'name' | 'memberCount' | 'projectCount' | 'createdDate'>;
  let sortColumn: Column = $state('name');
  type Dir = 'asc' | 'desc';
  let sortDir: Dir = $state('asc');

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

  let filteredOrgs = $derived($orgs ? filterOrgs($orgs, $queryParamValues.search) : []);
  let displayOrgs = $derived(sortOrgs(filteredOrgs, sortColumn, sortDir));
  let filtering = $derived(filteredOrgs.length !== $orgs.length);

  const [myOrgs, otherOrgs]: [OrgList, OrgList] = $derived(partition(displayOrgs, (org) => $myOrgsMap.has(org.id)));
</script>

<!--
TODO:

* Sort options: name, created date, # users
* Paging
-->

<HeaderPage wide titleText={$t('org.table.title')}>
  {#snippet actions()}

      <AdminContent>
        <a href={resolve('/org/create')} class="btn btn-success">
          {$t('org.create.title')}
          <span class="i-mdi-plus text-2xl"></span>
        </a>
      </AdminContent>

  {/snippet}
  {#snippet title()}

      {$t('org.table.title')}
      <Icon icon="i-mdi-account-group-outline" size="text-5xl" y="10%" />

  {/snippet}
  {#snippet headerContent()}

      <FilterBar
        searchKey="search"
        filterKeys={['search']}
        filters={queryParamValues}
        filterDefaults={defaultQueryParamValues}
      />

  {/snippet}
  <div class="overflow-x-auto @container scroll-shadow">
    <table class="table table-lg">
      <thead>
        <tr class="bg-base-200">
          <th onclick={() => handleSortClick('name')} class="cursor-pointer hover:bg-base-300">
            {$t('org.table.name')}
            <span class:invisible={sortColumn !== 'name'}  class="{`i-mdi-sort-${sortDir}ending`} text-xl align-[-5px] ml-2"></span>
          </th>
          <th onclick={() => handleSortClick('memberCount')} class="cursor-pointer hover:bg-base-300 hidden @md:table-cell">
            {$t('org.table.members')}
            <span class:invisible={sortColumn !== 'memberCount'} class="{`i-mdi-sort-${sortDir}ending`} text-xl align-[-5px] ml-2"></span>
          </th>
          <th onclick={() => handleSortClick('projectCount')} class="cursor-pointer hover:bg-base-300 hidden @md:table-cell">
            {$t('org.table.projects')}
            <span class:invisible={sortColumn !== 'projectCount'} class="{`i-mdi-sort-${sortDir}ending`} text-xl align-[-5px] ml-2"></span>
          </th>
          <th onclick={() => handleSortClick('createdDate')} class="cursor-pointer hover:bg-base-300 hidden @xl:table-cell">
            {$t('org.table.created_at')}
            <span class:invisible={sortColumn !== 'createdDate'}  class="{`i-mdi-sort-${sortDir}ending`} text-xl align-[-5px] ml-2"></span>
          </th>
        </tr>
      </thead>
      <tbody>
        {#if !displayOrgs.length}
          <tr>
            <td colspan="4" class="text-center text-secondary">
              {$t('org.table.no_orgs_found')}
            </td>
          </tr>
        {:else}
          {@const showingMyOrgsHeader = !filtering || myOrgs.length}
          {#if showingMyOrgsHeader}
            <tr>
              <td colspan="4" class="text-sm bg-neutral/75 text-neutral-content py-2">
                {$t('org.table.my_orgs')}
              </td>
            </tr>
            {#if !$myOrgsMap.size}
              <tr>
                <td colspan="4" class="text-center text-secondary">
                  {$t('org.table.not_in_any_orgs')}
                </td>
              </tr>
            {/if}
          {/if}
          {#each [...myOrgs, ...otherOrgs] as org, i (org.id)}
            {@const isFirstOtherOrg = i === myOrgs.length}
            {#if showingMyOrgsHeader && isFirstOtherOrg}
              <tr>
                <td colspan="4" class="text-sm bg-neutral/75 text-neutral-content py-2">
                  {$t('org.table.other_orgs')}
                </td>
              </tr>
            {/if}
            <tr>
              <td>
                  <a class="link" href={resolve(`/org/${org.id}`)}>
                    {org.name}
                  </a>
              </td>
              <td class="hidden @md:table-cell">
                {$number(org.memberCount)}
              </td>
              <td class="hidden @md:table-cell">
                {$number(org.projectCount)}
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
