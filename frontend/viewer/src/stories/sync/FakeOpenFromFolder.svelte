<script lang="ts">
  import type {Snippet} from 'svelte';
  import {DotnetService, type ICombinedProjectsService} from '$lib/dotnet-types';
  import type {IOpenFromFolderSummary} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/IOpenFromFolderSummary';
  import {delay} from '$lib/utils/time';
  import {FolderSyncStatus} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/FolderSyncStatus';

  const {result, children}: {result: IOpenFromFolderSummary; children: Snippet} = $props();

  // OpenFromFolderItem only calls these
  window.lexbox.ServiceProvider.setService(DotnetService.CombinedProjectsService, {
    openFromFolder: () => delay(500).then(() => result),
    syncProjectWithFolder: () => delay(1000).then(() => ({status: FolderSyncStatus.Synced, mediaSent: 0, mediaReceived: 2, mediaFailed: 0})),
  } as Partial<ICombinedProjectsService> as ICombinedProjectsService);
</script>

{@render children()}
