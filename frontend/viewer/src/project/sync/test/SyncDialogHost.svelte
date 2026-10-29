<script lang="ts">
  import {Router} from 'svelte-routing';
  import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
  import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
  import {initProjectContext} from '$project/project-context.svelte';
  import SyncDialog from '../../SyncDialog.svelte';
  import {untrack} from 'svelte';

  const {syncService}: {syncService: ISyncServiceJsInvokable} = $props();

  initProjectContext({
    api: {supportedFeatures: () => Promise.resolve({sync: true, write: true})} as unknown as IMiniLcmJsInvokable,
    projectName: 'test-project',
    projectCode: 'test-project',
    server: {id: 'test-server', displayName: 'Lexbox', authority: 'https://test-server.com'},
    syncService: untrack(() => syncService),
  });
  let dialog = $state<SyncDialog>();
  export function open() {
    dialog?.open();
  }
</script>

<Router>
  <SyncDialog bind:this={dialog} />
</Router>
