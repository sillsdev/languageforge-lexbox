<script lang="ts">
  import t, {date} from '$lib/i18n';
  import Loader from './Loader.svelte';
  import type {ProjectHarmonyCommitsQuery} from '$lib/gql/types';
  import {Icon} from '$lib/icons';
  import DevContent from '$lib/layout/DevContent.svelte';

  type Commits = NonNullable<ProjectHarmonyCommitsQuery['projectByCode']>['harmonyCommits'];

  interface Props {
    commits: Commits;
    loading: boolean;
  }

  const {commits, loading}: Props = $props();

  // The server returns each commit's CommitMetadata as an opaque JSON scalar. We only surface authorName
  // today; other fields stay available to the client for future use without a schema change.
  // eslint-disable-next-line @typescript-eslint/naming-convention
  type CommitMetadata = {AuthorName?: string | null; AuthorId?: string | null, ClientVersion?: string | null};
  function authorName(metadata: unknown): string {
    const name = (metadata as CommitMetadata | null | undefined)?.AuthorName;
    return typeof name === 'string' && name.length > 0 ? name : $t('project_page.harmony.unknown_author');
  }
  function authorId(metadata: unknown): string {
    const id = (metadata as CommitMetadata | null | undefined)?.AuthorId;
    return typeof id === 'string' && id.length > 0 ? id : $t('project_page.harmony.unknown_author');
  }
  function clientVersion(metadata: unknown): string {
    const version = (metadata as CommitMetadata | null | undefined)?.ClientVersion;
    return typeof version === 'string' && version.length > 0 ? version : 'Uknown';
  }
</script>

<table class="table table-zebra">
  <thead>
    <tr class="sticky top-0 z-[1] bg-base-100">
      <th>{$t('project_page.harmony.date_header')}</th>
      <th>{$t('project_page.harmony.author_header')}</th>
      <DevContent>
        <th>Client ID</th>
        <th>Client Version</th>
      </DevContent>
    </tr>
  </thead>
  <tbody>
    {#if commits?.length}
      {#each commits as commit (commit.id)}
        <tr>
          <td title={$date(commit.hybridDateTime.dateTime, {dateStyle: 'full', timeStyle: 'long'})}>
            {$date(commit.hybridDateTime.dateTime)}
            {#if commit.hybridDateTime.counter > 0}
              <span class="text-xs text-secondary">+{commit.hybridDateTime.counter}</span>
            {/if}
          </td>
          <td title={authorId(commit.metadata)}>{authorName(commit.metadata)}</td>
          <DevContent>
            <td>{commit.clientId}</td>
            <td>{clientVersion(commit.metadata)}</td>
          </DevContent>
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
