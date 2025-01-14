<script lang="ts">
  import {NewTabLinkRenderer} from '$lib/components/Markdown';
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';
  import { Button } from '$lib/forms';
  import t from '$lib/i18n';
  import { Icon } from '$lib/icons';
  import { useNotifications } from '$lib/notify';
  import Markdown from 'svelte-exmarkdown';

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
  <Button variant="btn-primary" on:click={useInFwLite}>{$t('project.crdt.try_fw_lite')}</Button>
  <ConfirmModal bind:this={confirmModal} title={$t('project.crdt.try_fw_lite')} submitText={$t('project.crdt.submit')} cancelText={$t('project.crdt.cancel')}>
    <div class="prose max-w-none underline-links">
      <Markdown md={$t('project.crdt.try_info')} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
    </div>
  </ConfirmModal>
{/if}
