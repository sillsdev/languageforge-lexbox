<script module lang="ts">
  import {setupServiceProvider} from '$lib/services/service-provider';
  import {defineMeta} from '@storybook/addon-svelte-csf';

  const {Story} = defineMeta({});
  setupServiceProvider();
</script>
<script lang="ts">
  import FolderSyncSection from '../../project/sync/FolderSyncSection.svelte';
  import OpenFromFolderItem from '../../home/OpenFromFolderItem.svelte';
  import FakeSyncService from './FakeSyncService.svelte';
  import FakeOpenFromFolder from './FakeOpenFromFolder.svelte';
  import NoFolderPicker from './NoFolderPicker.svelte';
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
    hasFolderPicker: () => Promise.resolve(true),
    pickFolder: () => Promise.resolve(String.raw`E:\Example project`),
  });
  refreshPlatformFeatures();

  const lastSync = new Date(Date.now() - 60 * 60 * 1000).toISOString();
  const localStatus = {local: 4, remote: 0};
</script>

<Story name="Sync dialog: first folder sync">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync}}>
    <FolderSyncSection />
  </FakeSyncService>
  <Toaster />
</Story>
<Story name="Sync dialog: remembered folder">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync, rememberedSyncFolder: String.raw`E:\test-project`}}>
    <FolderSyncSection />
  </FakeSyncService>
</Story>
<Story name="Sync dialog: remembered folder, long path">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync, rememberedSyncFolder: String.raw`C:\Users\someone\Documents\Dictionary work\Shared\test-project`}}>
    <FolderSyncSection />
  </FakeSyncService>
</Story>
<Story name="Sync dialog: folder already has other files">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync, folderSyncResult: {status: FolderSyncStatus.NotASyncFolder, mediaSent: 0, mediaReceived: 0, mediaFailed: 0}}}>
    <FolderSyncSection />
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
<Story name="Sync dialog: sync failed">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync, folderSyncResult: {status: FolderSyncStatus.Failed, mediaSent: 0, mediaReceived: 0, mediaFailed: 0}}}>
    <FolderSyncSection />
  </FakeSyncService>
  <Toaster />
</Story>
<Story name="Sync dialog: no writing system in common">
  <FakeSyncService data={{localStatus, latestSyncedCommitDate: lastSync, folderSyncResult: {status: FolderSyncStatus.NoWritingSystemInCommon, mediaSent: 0, mediaReceived: 0, mediaFailed: 0, localWritingSystemIds: ['en', 'seh'], folderWritingSystemIds: ['fr']}}}>
    <FolderSyncSection />
  </FakeSyncService>
  <Toaster />
</Story>
<Story name="Home: project code in use">
  <FakeOpenFromFolder result={{outcome: OpenFromFolderOutcome.ProjectCodeInUse, projectCode: 'test-project', projectName: 'Test Project'}}>
    <Router>
      <OpenFromFolderItem refreshProjects={() => Promise.resolve()} />
    </Router>
  </FakeOpenFromFolder>
  <Toaster />
</Story>
<Story name="Home: no folder picker">
  <NoFolderPicker>
    <FakeOpenFromFolder result={{outcome: OpenFromFolderOutcome.Downloaded, projectCode: 'test-project', projectName: 'Test Project'}}>
      <Router>
        <OpenFromFolderItem refreshProjects={() => Promise.resolve()} />
      </Router>
    </FakeOpenFromFolder>
  </NoFolderPicker>
  <Toaster />
</Story>
<Story name="Home: project already on this device">
  <FakeOpenFromFolder result={{outcome: OpenFromFolderOutcome.AlreadyOnDevice, projectCode: 'test-project', projectName: 'Test Project'}}>
    <Router>
      <OpenFromFolderItem refreshProjects={() => Promise.resolve()} />
    </Router>
  </FakeOpenFromFolder>
  <Toaster />
</Story>
