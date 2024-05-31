<script lang="ts">
  import t, { date, number } from '$lib/i18n';
  import { Page } from '$lib/layout';
  import type { PageData } from './$types';

  export let data: PageData;
  $: orgs = data.orgs;

  type Column = 'name' | 'users' | 'created_at';
  let sortColumn = 'created_at' as Column;
  type Dir = 'ascending' | 'descending';
  let sortDir = 'ascending' as Dir;

  function swapSortDir(): void {
    sortDir = sortDir == 'ascending' ? 'descending' : 'ascending';
  }

  function handleSortClick(clickedColumn: Column): void {
    if (sortColumn === clickedColumn) {
      swapSortDir();
    } else {
      sortColumn = clickedColumn;
      sortDir = 'ascending';
    }
  }
</script>

<!--
TODO:

* Sort options: name, created date, # users
* Paging
-->

<Page wide>
  <h1 class="text-3xl text-left grow max-w-full mb-4">Organisations</h1>
  <div class="overflow-x-auto @container scroll-shadow">
    <table class="table table-lg">
      <thead>
        <tr class="bg-base-200">
          <th on:click={() => handleSortClick('name')}>
            {$t('project.table.name')}
            {#if sortColumn == 'name'}
            <span class="{`i-mdi-sort-${sortDir}`} text-xl align-[-5px] ml-2" />
            {/if}
          </th>
          <th on:click={() => handleSortClick('users')} class="hidden @md:table-cell">
            {$t('project.table.users')}
            {#if sortColumn == 'users'}
            <span class="{`i-mdi-sort-${sortDir}`} text-xl align-[-5px] ml-2" />
            {/if}
          </th>
          <th on:click={() => handleSortClick('created_at')} class="hidden @xl:table-cell">
              {$t('project.table.created_at')}
              {#if sortColumn == 'created_at'}
              <span class="{`i-mdi-sort-${sortDir}`} text-xl align-[-5px] ml-2" />
              {/if}
          </th>
        </tr>
      </thead>
      <tbody>
        {#each $orgs as org}
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
