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
  import FakeOpenFromFolder from './FakeOpenFromFolder.svelte';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {FolderSyncStatus} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/FolderSyncStatus';
  import {OpenFromFolderOutcome} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/OpenFromFolderOutcome';
  import {DotnetService} from '$lib/dotnet-types';
  import {fallbackPlatformFeaturesService, refreshPlatformFeatures} from '$lib/services/platform-features-service';
  import {Toaster} from '$lib/components/ui/sonner';
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
  const localStatus = {local: 4, remote: 0};
</script>

<Story name="Sync dialog: first folder sync">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync}}>
    <FwLiteSyncView syncStatus={SyncStatus.Offline} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
  <Toaster />
</Story>
<Story name="Sync dialog: remembered folder">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync, rememberedSyncFolder: String.raw`E:\test-project`}}>
    <FwLiteSyncView syncStatus={SyncStatus.Offline} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="Sync dialog: remembered folder, long path">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync, rememberedSyncFolder: String.raw`C:\Users\someone\Documents\Dictionary work\Shared\test-project`}}>
    <FwLiteSyncView syncStatus={SyncStatus.Offline} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="Sync dialog: folder already has other files">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync, folderSyncResult: {status: FolderSyncStatus.NotASyncFolder, mediaSent: 0, mediaReceived: 0, mediaFailed: 0}}}>
    <FwLiteSyncView syncStatus={SyncStatus.Offline} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
  <Toaster />
</Story>
<Story name="Home: open from folder">
  <FakeOpenFromFolder result={{outcome: OpenFromFolderOutcome.Downloaded, projectCode: 'test-project', projectName: 'Test Project'}}>
    <Router>
      <OpenFromFolderItem refreshProjects={() => Promise.resolve()} />
    </Router>
  </FakeOpenFromFolder>
  <Toaster />
</Story>
<Story name="Home: folder without a project">
  <FakeOpenFromFolder result={{outcome: OpenFromFolderOutcome.NotASyncFolder}}>
    <Router>
      <OpenFromFolderItem refreshProjects={() => Promise.resolve()} />
    </Router>
  </FakeOpenFromFolder>
  <Toaster />
</Story>
