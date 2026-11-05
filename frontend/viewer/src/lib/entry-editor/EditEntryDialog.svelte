<script lang="ts">
  import {resource} from 'runed';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import {EntryPersistence} from '$lib/entry-editor/entry-persistence.svelte';
  import * as Dialog from '$lib/components/ui/dialog';
  import {Button} from '$lib/components/ui/button';
  import {useViewService} from '$lib/views/view-service.svelte';
  import RootFields from '$lib/views/RootFields.svelte';
  import {pt} from '$lib/views/view-text';
  import {t} from 'svelte-i18n-lingui';
  import {AppNotification} from '$lib/notifications/notifications';

  const api = useMiniLcmApi();
  const viewService = useViewService();
  let entryLabel = $derived(pt($t`Entry`, $t`Word`, viewService.currentView));
  let {
    entryId,
    open = $bindable(false),
    mode = 'edit',
  }: {
    entryId?: string,
    open: boolean,
    /** 'view' opens read-only with an Edit button; 'edit' opens editable (default). */
    mode?: 'view' | 'edit',
  } = $props();
  // Own the mode internally so the in-dialog Edit button can flip it; reset to the requested mode
  // whenever the dialog (re)opens.
  let currentMode = $state<'view' | 'edit'>('edit');
  $effect(() => {
    if (open) currentMode = mode;
  });
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

  let editor = $state<EntryEditor>();

  async function updateEntry() {
    if (!entry) return;
    updating = true;
    await editor?.commit();
    await entryPersistence.updateEntry(entry).finally(() => updating = false);
    open = false;
  }
</script>
<Dialog.Root bind:open>
  <Dialog.DialogContent>
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>
        {currentMode === 'view' ? $t`View ${entryLabel}` : $t`Update ${entryLabel}`}
      </Dialog.DialogTitle>
    </Dialog.DialogHeader>
    {#if entryResource.loading}
      Loading...
    {:else if entry}
      <!--
      RootFields, so that the user is not limited to the fields of a potentially selected custom-view.
      Custom-views should perhaps be scoped to the browse-view. I'm not sure.
      -->
      <RootFields>
        <EntryEditor bind:this={editor} autofocus={currentMode === 'edit'} modalMode readonly={currentMode === 'view'} bind:entry canAddSense={false} canAddExample={false}/>
      </RootFields>
    {/if}
    <Dialog.DialogFooter>
      {#if currentMode === 'view'}
        <Button onclick={() => open = false} variant="secondary">{$t`Close`}</Button>
        <Button icon="i-mdi-pencil" onclick={() => currentMode = 'edit'} disabled={!entry || entryResource.loading}>{$t`Edit`}</Button>
      {:else}
        <Button onclick={() => open = false} variant="secondary">{$t`Cancel`}</Button>
        <Button onclick={() => updateEntry()} disabled={updating || !entry || entryResource.loading} loading={updating}>
          {$t`Update ${entryLabel}`}
        </Button>
      {/if}
    </Dialog.DialogFooter>
  </Dialog.DialogContent>
</Dialog.Root>
