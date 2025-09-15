<script module lang="ts">
  import {setupServiceProvider} from '$lib/services/service-provider';
  import {defineMeta} from '@storybook/addon-svelte-csf';

  const {Story} = defineMeta({});
  setupServiceProvider();
</script>
<script lang="ts">
import SyncStatusPrimitive from '../../project/sync/SyncStatusPrimitive.svelte';
import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
import {onDestroy} from 'svelte';
import {DotnetService} from '$lib/dotnet-types';


if (!window.lexbox.ServiceProvider.tryGetService(DotnetService.AuthService)) {
  window.lexbox.ServiceProvider.setService(DotnetService.AuthService, {
    useSystemWebView: () => Promise.resolve(true),
    logout: () => Promise.resolve(),
    signInWebView: () => Promise.resolve(),
    servers: () => Promise.resolve([]),
  });
  onDestroy(() => {
    window.lexbox.ServiceProvider.removeService(DotnetService.AuthService);
  });
}

function delayAsync(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}

const now = new Date();
const oneHourAgo = new Date(now.getTime() - 60 * 60 * 1000);
const oneDayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);
const oneWeekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);

</script>

<Story name="Normal">
  <SyncStatusPrimitive
    canSyncLexboxToFlex
    serverId="test-server"
    projectCode="test-project"
    syncLexboxToFlex={() => delayAsync(1000)}
    syncLexboxToLocal={() => delayAsync(1000)}
    syncStatus={SyncStatus.Success}
    localStatus={{local: 1, remote: 200}}
    remoteStatus={{
      status: ProjectSyncStatusEnum.ReadyToSync,
      pendingCrdtChanges: 1,
      pendingMercurialChanges: 2,
      lastCrdtCommitDate: oneDayAgo.toISOString(),
      lastMercurialCommitDate: oneWeekAgo.toISOString(),
      errorCode: undefined,
      errorMessage: undefined,
    }}
    latestSyncedCommitDate={oneHourAgo.toISOString()}
    server={{
      id: 'test-server',
      displayName: 'Lexbox',
      authority: 'https://test-server.com',
    }}
  />
</Story>
<Story name="No Changes">
  <SyncStatusPrimitive
    canSyncLexboxToFlex
    serverId="test-server"
    projectCode="test-project"
    syncStatus={SyncStatus.Success}
    localStatus={{local: 0, remote: 0}}
    remoteStatus={{
      status: ProjectSyncStatusEnum.ReadyToSync,
      pendingCrdtChanges: 0,
      pendingMercurialChanges: 0,
      lastCrdtCommitDate: new Date().toISOString(),
      lastMercurialCommitDate: new Date().toISOString(),
      errorCode: undefined,
      errorMessage: undefined,
    }}
    latestSyncedCommitDate={new Date().toISOString()}
    server={{
      id: 'test-server',
      displayName: 'Lexbox',
      authority: 'https://test-server.com',
    }}
  />
</Story>
<Story name="Loading remote status">
  <SyncStatusPrimitive
    canSyncLexboxToFlex
    serverId="test-server"
    projectCode="test-project"
    syncStatus={SyncStatus.Success}
    localStatus={{local: 1, remote: 2}}
    latestSyncedCommitDate={new Date().toISOString()}
    server={{
      id: 'test-server',
      displayName: 'Lexbox',
      authority: 'https://test-server.com',
    }}
  />
</Story>
<Story name="No Server">
  <SyncStatusPrimitive
    canSyncLexboxToFlex
    serverId={undefined}
    projectCode="test-project"
    syncStatus={SyncStatus.NoServer}
    localStatus={{local: 1}}
    latestSyncedCommitDate={new Date().toISOString()}
  />
</Story>
<Story name="Unknown Server">
  <SyncStatusPrimitive
    canSyncLexboxToFlex
    serverId="test-server"
    projectCode="test-project"
    syncStatus={SyncStatus.NoServer}
    localStatus={{local: 1}}
    latestSyncedCommitDate={new Date().toISOString()}
  />
</Story>
<Story name="Offline">
  <SyncStatusPrimitive
    canSyncLexboxToFlex={false}
    serverId="test-server"
    projectCode="test-project"
    syncStatus={SyncStatus.Offline}
    localStatus={{local: 10}}
    latestSyncedCommitDate={new Date().toISOString()}
    server={{
      id: 'test-server',
      displayName: 'Lexbox',
      authority: 'https://test-server.com',
    }}
  />
</Story>
<Story name="Not Logged In">
  <SyncStatusPrimitive
    canSyncLexboxToFlex
    serverId="test-server"
    projectCode="test-project"
    syncStatus={SyncStatus.NotLoggedIn}
    localStatus={{local: 1}}
    latestSyncedCommitDate={new Date().toISOString()}
    server={{
      id: 'test-server',
      displayName: 'Lexbox',
      authority: 'https://test-server.com',
    }}
  />
</Story>

<Story name="Never synced">
  <SyncStatusPrimitive
    canSyncLexboxToFlex
    serverId="test-server"
    projectCode="test-project"
    syncStatus={SyncStatus.Success}
    localStatus={{local: 1}}
    latestSyncedCommitDate={undefined}
  />
</Story>
