<script module lang="ts">
  import {setupServiceProvider} from '$lib/services/service-provider';
  import {defineMeta} from '@storybook/addon-svelte-csf';

  const {Story} = defineMeta({});
  setupServiceProvider();
</script>
<script lang="ts">
  import StoryState from '../StoryState.svelte';
  import OpenSyncDialog from './OpenSyncDialog.svelte';
  import FakeSyncService from './FakeSyncService.svelte';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
  import {DotnetService, LoginResult} from '$lib/dotnet-types';
  import {fallbackPlatformFeaturesService, refreshPlatformFeatures} from '$lib/services/platform-features-service';
  import {Toaster} from '$lib/components/ui/sonner';
  import {onDestroy} from 'svelte';

  // Only the Windows app can pick a folder, so stand in for it to show the folder sync section
  window.lexbox.ServiceProvider.setService(DotnetService.PlatformFeaturesService, {
    ...fallbackPlatformFeaturesService,
    supportsFolderSync: () => Promise.resolve(true),
    hasFolderPicker: () => Promise.resolve(true),
    pickFolder: () => Promise.resolve(String.raw`E:\Example project`),
  });
  refreshPlatformFeatures();

  // The not-logged-in state shows a login button
  if (!window.lexbox.ServiceProvider.tryGetService(DotnetService.AuthService)) {
    window.lexbox.ServiceProvider.setService(DotnetService.AuthService, {
      useSystemWebView: () => Promise.resolve(true),
      logout: () => Promise.resolve(),
      signInWebView: () => Promise.resolve(LoginResult.Success),
      servers: () => Promise.resolve([]),
    });
    onDestroy(() => {
      window.lexbox.ServiceProvider.removeService(DotnetService.AuthService);
    });
  }

  const server = {
    id: 'test-server',
    displayName: 'Lexbox',
    authority: 'https://test-server.com',
  };
  const now = Date.now();
  const oneHourAgo = new Date(now - 60 * 60 * 1000).toISOString();
  const oneDayAgo = new Date(now - 24 * 60 * 60 * 1000).toISOString();
  const oneWeekAgo = new Date(now - 7 * 24 * 60 * 60 * 1000).toISOString();
  const remoteStatus = {
    status: ProjectSyncStatusEnum.ReadyToSync,
    pendingCrdtChanges: 1,
    pendingMercurialChanges: 2,
    lastCrdtCommitDate: oneDayAgo,
    lastMercurialCommitDate: oneWeekAgo,
  };
</script>

<Story name="Normal">
  <StoryState channel="beta" authorName="Pat">
    <FakeSyncService {server} fieldWorksSync data={{localStatus: {local: 1, remote: 12}, latestSyncedCommitDate: oneHourAgo, remoteStatus}}>
      <OpenSyncDialog />
    </FakeSyncService>
    <Toaster />
  </StoryState>
</Story>
<Story name="Remembered sync folder">
  <StoryState channel="beta" authorName="Pat">
    <FakeSyncService {server} fieldWorksSync data={{localStatus: {local: 1, remote: 12}, latestSyncedCommitDate: oneHourAgo, remoteStatus, rememberedSyncFolder: String.raw`E:\Example project`}}>
      <OpenSyncDialog />
    </FakeSyncService>
    <Toaster />
  </StoryState>
</Story>
<Story name="Up to date">
  <StoryState channel="beta" authorName="Pat">
    <FakeSyncService {server} fieldWorksSync data={{localStatus: {local: 0, remote: 0}, latestSyncedCommitDate: new Date(now).toISOString(), remoteStatus: {...remoteStatus, pendingCrdtChanges: 0, pendingMercurialChanges: 0}}}>
      <OpenSyncDialog />
    </FakeSyncService>
    <Toaster />
  </StoryState>
</Story>
<Story name="Offline">
  <StoryState channel="beta" authorName="Pat">
    <FakeSyncService {server} fieldWorksSync data={{localStatus: {local: 7}, latestSyncedCommitDate: oneDayAgo}}>
      <OpenSyncDialog syncStatus={SyncStatus.Offline} />
    </FakeSyncService>
    <Toaster />
  </StoryState>
</Story>
<Story name="Not logged in">
  <StoryState channel="beta" authorName="Pat">
    <FakeSyncService {server} fieldWorksSync data={{localStatus: {local: 3}, latestSyncedCommitDate: oneDayAgo}}>
      <OpenSyncDialog syncStatus={SyncStatus.NotLoggedIn} />
    </FakeSyncService>
    <Toaster />
  </StoryState>
</Story>
<Story name="No FieldWorks Classic sync">
  <StoryState channel="beta" authorName="Pat">
    <FakeSyncService {server} fieldWorksSync={false} data={{localStatus: {local: 1, remote: 12}, latestSyncedCommitDate: oneHourAgo}}>
      <OpenSyncDialog />
    </FakeSyncService>
    <Toaster />
  </StoryState>
</Story>
<Story name="Local only (no server)">
  <StoryState channel="beta" authorName="Pat">
    <FakeSyncService data={{localStatus: {local: 5}}}>
      <OpenSyncDialog syncStatus={SyncStatus.NoServer} />
    </FakeSyncService>
    <Toaster />
  </StoryState>
</Story>
<!-- docs screenshots (tests/docs-screenshots) open this story -->
<Story name="Pending" parameters={{fwlite: {resizable: false}}}>
  <FakeSyncService {server} fieldWorksSync data={{localStatus: {local: 3, remote: 12}, latestSyncedCommitDate: oneHourAgo, remoteStatus: {...remoteStatus, pendingCrdtChanges: 5, lastCrdtCommitDate: oneHourAgo}}}>
    <OpenSyncDialog />
  </FakeSyncService>
  <Toaster />
</Story>
