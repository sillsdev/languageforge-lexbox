<script module lang="ts">
  import {setupServiceProvider} from '$lib/services/service-provider';
  import {defineMeta} from '@storybook/addon-svelte-csf';

  const {Story} = defineMeta({});
  setupServiceProvider();
</script>
<script lang="ts">
  import FwLiteSyncView from '../../project/sync/FwLiteSyncView.svelte';
  import OpenFromFolderItem from '../../home/OpenFromFolderItem.svelte';
  import FakeSyncService from './FakeSyncService.svelte';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {DotnetService} from '$lib/dotnet-types';
  import {fallbackPlatformFeaturesService, refreshPlatformFeatures} from '$lib/services/platform-features-service';
  import {Router} from 'svelte-routing';

  // Only the Windows app can pick a folder, so stand in for it
  window.lexbox.ServiceProvider.setService(DotnetService.PlatformFeaturesService, {
    ...fallbackPlatformFeaturesService,
    supportsFolderSync: () => Promise.resolve(true),
    pickFolder: () => Promise.resolve(String.raw`E:\Example project`),
  });
  refreshPlatformFeatures();

  const server = {
    id: 'test-server',
    displayName: 'Lexbox',
    authority: 'https://test-server.com',
  };
  const lastSync = new Date(Date.now() - 60 * 60 * 1000).toISOString();
</script>

<Story name="Sync dialog: first folder sync">
  <FakeSyncService data={{localStatus: {local: 4, remote: 0}, latestSyncedCommitDate: lastSync}}>
    <FwLiteSyncView syncStatus={SyncStatus.Offline} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="Sync dialog: remembered folder">
  <FakeSyncService data={{localStatus: {local: 4, remote: 0}, latestSyncedCommitDate: lastSync, rememberedSyncFolder: String.raw`E:\Example project`}}>
    <FwLiteSyncView syncStatus={SyncStatus.Offline} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="Home: open from folder">
  <Router>
    <OpenFromFolderItem refreshProjects={() => Promise.resolve()} />
  </Router>
</Story>
