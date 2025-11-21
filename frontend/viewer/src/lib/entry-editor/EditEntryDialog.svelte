<script lang="ts">
  import {resource} from 'runed';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import {EntryPersistence} from '$lib/entry-editor/entry-persistence.svelte';
  import * as Dialog from '$lib/components/ui/dialog';
  import {Button} from '$lib/components/ui/button';
  import {useCurrentView} from '$lib/views/view-service';
  import {pt} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';
  import {AppNotification} from '$lib/notifications/notifications';

  const api = useMiniLcmApi();
  const currentView = useCurrentView();
  let entryLabel = $derived(pt($t`Entry`, $t`Word`, $currentView));
  let {
    entryId,
    open = $bindable(false),
  }: {
    entryId?: string,
    open: boolean,
  } = $props();
  let entryResource = resource(() => entryId, async (entryId) => {
    if (!entryId) return undefined;
    return await api.getEntry(entryId);
  });
  $effect(() => {
    if (entryResource.error) {
      AppNotification.error('Failed to load entry', entryResource.error.message);
    }
  });
  let entry = $derived(entryResource.current);
  const entryPersistence = new EntryPersistence(() => entry);
  let updating = $state(false);

  async function updateEntry() {
    if (!entry) return;
    updating = true;
    await entryPersistence.updateEntry(entry).finally(() => updating = false);
    open = false;
  }
</script>
<Dialog.Root bind:open>
  <Dialog.DialogContent>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`Update ${entryLabel}`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    {#if entryResource.loading}
      Loading...
    {:else if entry}
      <EntryEditor autofocus modalMode {entry} canAddSense={false} canAddExample={false}/>
    {/if}
    <Dialog.DialogFooter>
      <Button onclick={() => open = false} variant="secondary">{$t`Cancel`}</Button>
      <Button onclick={() => updateEntry()} disabled={updating || !entry || entryResource.loading} loading={updating}>
        {$t`Update ${entryLabel}`}
      </Button>
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
