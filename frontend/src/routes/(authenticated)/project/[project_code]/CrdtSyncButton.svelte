<script lang="ts">
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';
  import { Button } from '$lib/forms';
  import { Icon } from '$lib/icons';
  import { useNotifications } from '$lib/notify';

  export let projectId: string;
  export let hasHarmonyCommits: boolean;

  const { notifySuccess, notifyWarning } = useNotifications();

  let syncing = false;

  async function triggerSync(): Promise<string | undefined> {
    syncing = true;
    try {
      const response = await fetch(`/api/crdt/sync/${projectId}`, {
        method: 'POST',
      });

      if (response.ok) {
        const { crdtChanges, fwdataChanges } = await response.json();
        notifySuccess(`Synced successfully (${fwdataChanges} FwData changes. ${crdtChanges} CRDT changes)`);
      } else {
        const error = `Failed to sync: ${response.statusText} (${response.status})`;
        notifyWarning(error);
        console.error(error, await response.text());
        return error;
      }
    } finally {
      syncing = false;
    }
    return;
  }

  async function useInFwLite(): Promise<void> {
    await confirmModal.open(async () => {
      return await triggerSync();
    });
  }
  let confirmModal: ConfirmModal;
</script>

{#if hasHarmonyCommits}
  <Button variant="btn-primary" class="gap-1" on:click={triggerSync} loading={syncing} customLoader>
    FwData
    <Icon icon="i-mdi-sync" spin={syncing} spinReverse />
    CRDT
  </Button>
{:else}
  <Button variant="btn-primary" class="gap-1" loading={syncing} on:click={useInFwLite}>Use in FieldWorks Lite</Button>
  <ConfirmModal bind:this={confirmModal} title="Use in FieldWorks Lite" submitText="Confirm" cancelText="Don't use">
    <p>
      This will make your project available to FieldWorks Lite. This is still experimental and it will effect your
      FieldWorks project data. However all your data is safe and backed up on Lexbox.
      If you run into problems with your project please tell support that you are using FieldWorks Lite.
    </p>
  </ConfirmModal>
{/if}
