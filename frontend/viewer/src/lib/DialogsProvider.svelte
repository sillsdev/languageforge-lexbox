<script lang="ts">
  import NewEntryDialog from '$lib/entry-editor/NewEntryDialog.svelte';
  import DeleteDialog from '$lib/entry-editor/DeleteDialog.svelte';
  import ManageCustomViewsDialog from '$lib/views/ManageCustomViewsDialog.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectContext} from '$project/project-context.svelte';
  const projectContext = useProjectContext();
  const dialogsService = useDialogsService();
  let deleteDialog = $state<DeleteDialog>();
  $effect(() => {
    dialogsService.invokeDeleteDialog = deleteDialog?.prompt;
  })

  let manageCustomViewsOpen = $state(false);
  $effect(() => {
    dialogsService.invokeManageCustomViews = () => manageCustomViewsOpen = true;
  });
</script>

{#if projectContext.maybeApi}
  <NewEntryDialog/>
{/if}
<DeleteDialog bind:this={deleteDialog}/>
<ManageCustomViewsDialog bind:open={manageCustomViewsOpen} />
