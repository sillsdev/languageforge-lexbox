<script lang="ts">
  import t, { date, number } from '$lib/i18n';
  import { Page } from '$lib/layout';
  import type { PageData } from './$types';

  export let data: PageData;
  $: orgs = data.orgs;
</script>

<!--
TODO:

* Sort options: name, created date, # users
* Paging
-->

<Page wide>
  <h1 class="text-3xl text-left grow max-w-full my-2">Organisations</h1>
  <div class="overflow-x-auto @container scroll-shadow">
    <table class="table table-lg">
      <thead>
        <tr class="bg-base-200">
          <th>{$t('project.table.name')}</th>
          <th class="hidden @md:table-cell">{$t('project.table.users')}</th>
          <th class="hidden @xl:table-cell">
              {$t('project.table.created_at')}
              <span class="i-mdi-sort-descending text-xl align-[-5px] ml-2" />
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
