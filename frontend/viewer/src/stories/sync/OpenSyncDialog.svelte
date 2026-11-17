<script lang="ts">
  import {onMount} from 'svelte';
  import {Router} from 'svelte-routing';
  import {Button} from '$lib/components/ui/button';
  import SyncDialog from '../../project/SyncDialog.svelte';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';

  const {syncStatus = SyncStatus.Success}: {syncStatus?: SyncStatus} = $props();

  let dialog = $state<SyncDialog>();
  onMount(() => dialog?.open());
</script>

<Router>
  <Button variant="outline" icon="i-mdi-sync" onclick={() => dialog?.open()}>Open sync dialog</Button>
  <SyncDialog bind:this={dialog} {syncStatus} />
</Router>
