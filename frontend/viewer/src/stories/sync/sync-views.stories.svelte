<script module lang="ts">
  import {setupServiceProvider} from '$lib/services/service-provider';
  import {defineMeta} from '@storybook/addon-svelte-csf';

  const {Story} = defineMeta({});
  setupServiceProvider();
</script>
<script lang="ts">
import FwLiteSyncView from '../../project/sync/FwLiteSyncView.svelte';
import FwClassicSyncView from '../../project/sync/FwClassicSyncView.svelte';
import FakeSyncService from './FakeSyncService.svelte';
import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
import {onDestroy} from 'svelte';
import {DotnetService, LoginResult} from '$lib/dotnet-types';


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

const now = new Date();
const oneHourAgo = new Date(now.getTime() - 60 * 60 * 1000);
const oneDayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);
const oneWeekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);

const server = {
  id: 'test-server',
  displayName: 'Lexbox',
  authority: 'https://test-server.com',
};

</script>

<Story name="FW Lite: Normal">
  <FakeSyncService data={{localStatus: {local: 1, remote: 200}, latestSyncedCommitDate: oneHourAgo.toISOString()}}>
    <FwLiteSyncView syncStatus={SyncStatus.Success} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="FW Lite: Can't sync FieldWorks Classic">
  <FakeSyncService data={{localStatus: {local: 1, remote: 2}, latestSyncedCommitDate: oneHourAgo.toISOString()}}>
    <FwLiteSyncView syncStatus={SyncStatus.Success} {server} serverId="test-server" projectCode="test-project" explainFieldWorksSync />
  </FakeSyncService>
</Story>
<Story name="FW Lite: No Changes">
  <FakeSyncService data={{localStatus: {local: 0, remote: 0}, latestSyncedCommitDate: now.toISOString()}}>
    <FwLiteSyncView syncStatus={SyncStatus.Success} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="FW Lite: No Server">
  <FakeSyncService data={{localStatus: {local: 1}, latestSyncedCommitDate: now.toISOString()}}>
    <FwLiteSyncView syncStatus={SyncStatus.NoServer} projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="FW Lite: Unknown Server">
  <FakeSyncService data={{localStatus: {local: 1}, latestSyncedCommitDate: now.toISOString()}}>
    <FwLiteSyncView syncStatus={SyncStatus.NoServer} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="FW Lite: Offline">
  <FakeSyncService data={{localStatus: {local: 10}, latestSyncedCommitDate: now.toISOString()}}>
    <FwLiteSyncView syncStatus={SyncStatus.Offline} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="FW Lite: Not Logged In">
  <FakeSyncService data={{localStatus: {local: 1}, latestSyncedCommitDate: now.toISOString()}}>
    <FwLiteSyncView syncStatus={SyncStatus.NotLoggedIn} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>
<Story name="FW Lite: Never synced">
  <FakeSyncService data={{localStatus: {local: 1}}}>
    <FwLiteSyncView syncStatus={SyncStatus.Success} {server} serverId="test-server" projectCode="test-project" />
  </FakeSyncService>
</Story>

<Story name="FW Classic: Normal">
  <FakeSyncService data={{remoteStatus: {
    status: ProjectSyncStatusEnum.ReadyToSync,
    pendingCrdtChanges: 1,
    pendingMercurialChanges: 2,
    lastCrdtCommitDate: oneDayAgo.toISOString(),
    lastMercurialCommitDate: oneWeekAgo.toISOString(),
  }}}>
    <FwClassicSyncView syncStatus={SyncStatus.Success} {server} canSync />
  </FakeSyncService>
</Story>
<Story name="FW Classic: No Changes">
  <FakeSyncService data={{remoteStatus: {
    status: ProjectSyncStatusEnum.ReadyToSync,
    pendingCrdtChanges: 0,
    pendingMercurialChanges: 0,
    lastCrdtCommitDate: now.toISOString(),
    lastMercurialCommitDate: now.toISOString(),
  }}}>
    <FwClassicSyncView syncStatus={SyncStatus.Success} {server} canSync />
  </FakeSyncService>
</Story>
<Story name="FW Classic: Loading">
  <FakeSyncService data={{}}>
    <FwClassicSyncView syncStatus={SyncStatus.Success} {server} canSync />
  </FakeSyncService>
</Story>
<Story name="FW Classic: Offline">
  <FakeSyncService data={{}}>
    <FwClassicSyncView syncStatus={SyncStatus.Offline} {server} canSync={false} />
  </FakeSyncService>
</Story>
<Story name="FW Classic: Not Logged In">
  <FakeSyncService data={{}}>
    <FwClassicSyncView syncStatus={SyncStatus.NotLoggedIn} {server} canSync />
  </FakeSyncService>
</Story>
