<script lang="ts">
  import {Button} from '$lib/forms';
  import {Icon} from '$lib/icons';
  import {useNotifications} from '$lib/notify';

  export let projectId: string;

  const {notifySuccess, notifyWarning} = useNotifications();

  let syncing = false;

  async function triggerSync(): Promise<void> {
    syncing = true;
    try {
      const response = await fetch(`/api/crdt/crdt-sync/${projectId}`, {
        method: 'POST',
      });

      if (response.ok) {
        const { crdtChanges, fwdataChanges } = await response.json();
        notifySuccess(`Synced successfully (${fwdataChanges} FwData changes. ${crdtChanges} CRDT changes)`);
      } else {
        const error = `Failed to sync: ${response.statusText} (${response.status})`;
        notifyWarning(error);
        console.error(error, await response.text());
      }
    } finally {
      syncing = false;
    }
  }
</script>

<Button
  variant="btn-primary"
  class="gap-1"
  on:click={triggerSync}
  loading={syncing}
  customLoader
>
  FwData
  <Icon icon="i-mdi-sync" spin={syncing} spinReverse />
  CRDT
</Button>
