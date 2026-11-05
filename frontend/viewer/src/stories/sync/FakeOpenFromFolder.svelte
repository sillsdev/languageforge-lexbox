<script lang="ts">
  import type {Snippet} from 'svelte';
  import {DotnetService, type ICombinedProjectsService} from '$lib/dotnet-types';
  import type {IOpenFromFolderSummary} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/IOpenFromFolderSummary';
  import {delay} from '$lib/utils/time';

  const {result, children}: {result: IOpenFromFolderSummary; children: Snippet} = $props();

  // OpenFromFolderItem only calls openFromFolder
  window.lexbox.ServiceProvider.setService(DotnetService.CombinedProjectsService, {
    openFromFolder: () => delay(500).then(() => result),
  } as Partial<ICombinedProjectsService> as ICombinedProjectsService);
</script>

{@render children()}
