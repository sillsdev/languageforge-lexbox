<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import SyncDialogStory from './SyncDialogStory.svelte';
  import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';

  const {Story} = defineMeta({
    title: 'sync/sync-dialog',
    parameters: {fwlite: {resizable: false}},
  });

  const now = Date.now();
  function hoursAgo(hours: number) {
    return new Date(now - hours * 60 * 60 * 1000).toISOString();
  }
</script>

<Story name="Pending">
  <SyncDialogStory data={{
    localStatus: {local: 3, remote: 12},
    latestSyncedCommitDate: hoursAgo(1),
    remoteStatus: {
      status: ProjectSyncStatusEnum.ReadyToSync,
      pendingCrdtChanges: 5,
      pendingMercurialChanges: 2,
      lastCrdtCommitDate: hoursAgo(1),
      lastMercurialCommitDate: hoursAgo(7 * 24),
    },
  }} />
</Story>
