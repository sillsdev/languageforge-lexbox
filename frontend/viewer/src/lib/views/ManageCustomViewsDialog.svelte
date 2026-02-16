<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {Button} from '$lib/components/ui/button';
  import {t} from 'svelte-i18n-lingui';
  import {useCurrentView, useViews, addCustomView, deleteCustomView, fieldIdsFromView, isCustomView, updateCustomView, useCustomViewWriteDeps} from './view-service';
  import type {View} from './view-data';
  import CreateCustomViewDialog from './CreateCustomViewDialog.svelte';
  import EditCustomViewDialog from './EditCustomViewDialog.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import type {CustomViewFormInitialValue, CustomViewFormValue} from './CustomViewForm.svelte';

  interface Props {
    open: boolean;
  }

  let {open = $bindable()}: Props = $props();
  const viewsStore = useViews();
  const currentView = useCurrentView();
  const customViewWriteDeps = useCustomViewWriteDeps();
  const dialogsService = useDialogsService();

  const customViews = $derived($viewsStore.filter(isCustomView));

  let createOpen = $state(false);
  let editOpen = $state(false);
  let editTargetId = $state<string>();

  const editTarget = $derived(customViews.find((view) => view.id === editTargetId));
  const editInitialValue = $derived.by<CustomViewFormInitialValue | undefined>(() => {
    if (!editTarget) return undefined;
    return {
      id: editTarget.id,
      label: editTarget.label,
      baseViewId: editTarget.parentView.id === 'fieldworks' ? 'fieldworks' : 'fwlite',
      fieldIds: fieldIdsFromView(editTarget),
      overrides: editTarget.overrides,
    };
  });

  async function handleCreate(result: CustomViewFormValue) {
    const created = await addCustomView(result, customViewWriteDeps);
    currentView.set(created);
  }

  async function onDelete(view: View) {
    if (!isCustomView(view)) return;
    const shouldDelete = await dialogsService.promptDelete($t`custom view`, view.label);
    if (!shouldDelete) return;
    await deleteCustomView(view.id, customViewWriteDeps);
  }

  async function onSaveEdit(result: CustomViewFormValue) {
    if (!editTargetId) return;
    await updateCustomView(editTargetId, result, customViewWriteDeps);
  }
</script>

<Dialog.Root bind:open>
  <Dialog.Content class="max-w-lg">
    <Dialog.Header>
      <Dialog.Title>{$t`Manage Custom Views`}</Dialog.Title>
    </Dialog.Header>

    <div class="space-y-3">
      <div class="flex justify-end">
        <Button icon="i-mdi-plus" onclick={() => createOpen = true}>
          {$t`New Custom View`}
        </Button>
      </div>

      {#if customViews.length === 0}
        <p class="text-sm text-muted-foreground">{$t`No custom views yet.`}</p>
      {:else}
        <div class="rounded-md border divide-y">
          {#each customViews as view (view.id)}
            <div class="flex items-center justify-between px-3 py-2">
              <div class="min-w-0">
                <div class="truncate text-sm font-medium">{view.label}</div>
                <div class="truncate text-xs text-muted-foreground">
                  {$t`Based on ${view.parentView.label}`}
                </div>
              </div>
              <div class="flex items-center gap-1">
                <Button
                  size="icon"
                  variant="ghost"
                  icon="i-mdi-pencil-outline"
                  onclick={() => {
                    editTargetId = view.id;
                    editOpen = true;
                  }}
                />
                <Button
                  size="icon"
                  variant="ghost"
                  icon="i-mdi-trash-can-outline"
                  onclick={() => void onDelete(view)}
                />
              </div>
            </div>
          {/each}
        </div>
      {/if}
    </div>
  </Dialog.Content>
</Dialog.Root>

<CreateCustomViewDialog bind:open={createOpen} onCreate={handleCreate} />
<EditCustomViewDialog bind:open={editOpen} initialValue={editInitialValue} onSave={onSaveEdit} />
