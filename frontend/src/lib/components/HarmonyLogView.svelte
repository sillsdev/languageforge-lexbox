<script lang="ts">
  import t, {date} from '$lib/i18n';
  import Loader from './Loader.svelte';
  import type {ProjectHarmonyCommitsQuery} from '$lib/gql/types';
  import {Icon} from '$lib/icons';

  type Commits = NonNullable<ProjectHarmonyCommitsQuery['projectByCode']>['harmonyCommits'];

  interface Props {
    commits: Commits;
    loading: boolean;
  }

  const {commits, loading}: Props = $props();

  // The server returns each commit's CommitMetadata as an opaque JSON scalar. We only surface authorName
  // today; other fields stay available to the client for future use without a schema change.
  // eslint-disable-next-line @typescript-eslint/naming-convention
  type CommitMetadata = {AuthorName?: string | null};
  function authorName(metadata: unknown): string {
    const name = (metadata as CommitMetadata | null | undefined)?.AuthorName;
    return typeof name === 'string' && name.length > 0 ? name : $t('project_page.harmony.unknown_author');
  }
</script>

<table class="table table-zebra">
  <thead>
    <tr class="sticky top-0 z-[1] bg-base-100">
      <th>{$t('project_page.harmony.date_header')}</th>
      <th>{$t('project_page.harmony.author_header')}</th>
    </tr>
  </thead>
  <tbody>
    {#if false}
      {#each commits as commit (commit.id)}
        <tr>
          <td title={$date(commit.hybridDateTime.dateTime, {dateStyle: 'full', timeStyle: 'long'})}>
            {$date(commit.hybridDateTime.dateTime)}
            {#if commit.hybridDateTime.counter > 0}
              <span class="text-xs text-secondary">+{commit.hybridDateTime.counter}</span>
            {/if}
          </td>
          <td>{authorName(commit.metadata)}</td>
        </tr>
      {/each}
    {:else}
      <tr>
        <td colspan="100">
          <div class="text p-2 text-secondary flex gap-2 items-center">
            {#if loading}
              <Loader loading />
              {$t('project_page.harmony.loading')}
            {:else}
              <Icon icon="i-mdi-creation-outline" size="text-2xl" />
              {$t('project_page.harmony.no_history')}
            {/if}
          </div>
        </td>
      </tr>
    {/if}
  </tbody>
</table>
