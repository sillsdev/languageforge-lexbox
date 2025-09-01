<script lang="ts">
  import NewEntryDialog from '$lib/entry-editor/NewEntryDialog.svelte';
  import DeleteDialog from '$lib/entry-editor/DeleteDialog.svelte';
  import AudioDialog from './components/audio/AudioDialog.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  const dialogsService = useDialogsService();
  let deleteDialog = $state<DeleteDialog>();
  $effect(() => {
    dialogsService.invokeDeleteDialog = deleteDialog?.prompt;
  })
</script>

<NewEntryDialog/>
<DeleteDialog bind:this={deleteDialog}/>
<AudioDialog/>
