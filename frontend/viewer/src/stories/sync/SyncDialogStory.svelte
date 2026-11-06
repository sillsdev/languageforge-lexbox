<script lang="ts">
  // The real Synchronize dialog, opened, on a fake sync service: the demo project has no server, so
  // its sidebar can't show the Lexbox tab.
  import {onMount} from 'svelte';
  import {Router} from 'svelte-routing';
  import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
  import {SyncJobStatusEnum} from '$lib/dotnet-types';
  import {initProjectContext, useProjectContext} from '$project/project-context.svelte';
  import {delay} from '$lib/utils/time';
  import SyncDialog from '../../project/SyncDialog.svelte';
  import type {FakeSyncData} from './FakeSyncService.svelte';

  const {data}: {data: FakeSyncData} = $props();

  const server = {id: 'test-server', displayName: 'Lexbox', authority: 'https://lexbox.org'};
  const syncService: ISyncServiceJsInvokable = {
    getSyncStatus: () => data.remoteStatus ? Promise.resolve(data.remoteStatus) : new Promise(() => {}),
    triggerFwHeadlessSync: () => delay(1000).then(() => ({status: SyncJobStatusEnum.Success})),
    countPendingCrdtCommits: () => Promise.resolve(data.localStatus),
    getLatestSyncedCommitDate: () => Promise.resolve(data.latestSyncedCommitDate),
    executeSync: () => delay(1000).then(() => ({isSynced: true, missingFromLocal: [], missingFromRemote: []})),
    getCurrentServer: () => Promise.resolve(server),
  };

  const demoApi = useProjectContext().api;
  initProjectContext({
    api: new Proxy(demoApi, {
      get: (target, prop, receiver) => prop === 'supportedFeatures'
        ? async () => ({...await target.supportedFeatures(), sync: true, fieldWorksSync: true})
        : Reflect.get(target, prop, receiver) as unknown,
    }),
    projectName: 'Sena 3',
    projectCode: 'qaa-sena-flex',
    server,
    syncService,
  });

  let dialog = $state<SyncDialog>();
  onMount(() => dialog?.open());
</script>

<Router>
  <SyncDialog bind:this={dialog} />
</Router>
