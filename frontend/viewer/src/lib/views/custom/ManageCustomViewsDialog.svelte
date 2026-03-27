<script lang="ts">
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import {Button} from '$lib/components/ui/button';
  import {t} from 'svelte-i18n-lingui';
  import {useViewService} from '../view-service.svelte';
  import {FW_CLASSIC_VIEW, FW_LITE_VIEW} from '../view-data';
  import {Icon} from '$lib/components/ui/icon';
  import CreateCustomViewDialog from './CreateCustomViewDialog.svelte';
  import EditCustomViewDialog from './EditCustomViewDialog.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useCustomViewService} from '$project/data/custom-view-service.svelte';
  import {ViewBase, type ICustomView} from '$lib/dotnet-types';

  interface Props {
    open: boolean;
  }

  let {open = $bindable()}: Props = $props();
  const customViewService = useCustomViewService();
  const viewService = useViewService();
  const dialogsService = useDialogsService();

  const customViews = $derived(customViewService.current);

  let createOpen = $state(false);
  let editOpen = $state(false);
  let editValue = $state<ICustomView | undefined>(undefined);

  const subDialogOpen = $derived(createOpen || editOpen);
  // We close the "manage" dialog if a "sub" dialog is open, because
  // the sub dialog appears behind the "manage" drawer on mobile.
  // We also happen to get more elegant animations this way.
  const manageOpen = $derived(open && !subDialogOpen);

  function openEdit(view: ICustomView) {
    editValue = structuredClone($state.snapshot(view));
    editOpen = true;
  }

  async function onCreate(result: ICustomView) {
    const created = await customViewService.add(result);
    viewService.selectView(created.id);
  }

  async function onDelete(view: ICustomView) {
    const shouldDelete = await dialogsService.promptDelete($t`Custom view`, view.name);
    if (!shouldDelete) return;
    await customViewService.delete(view.id);
  }

  async function onSave(result: ICustomView) {
    if (!result.id) throw new Error('Edited custom view missing ID');
    if (result.id !== editValue?.id) throw new Error('Custom view IDs don\'t match')
    await customViewService.update(result);
    viewService.selectView(result.id);
  }
</script>

  <ResponsiveDialog open={manageOpen} onOpenChange={(v) => { if (!v) open = false; }} title={$t`Manage Custom Views`} contentProps={{class: 'max-w-lg'}}>
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
            <button
              type="button"
              class="flex w-full items-center justify-between px-3 py-2 text-left hover:bg-muted cursor-pointer"
              onclick={() => openEdit(view)}
            >
              <div class="min-w-0">
                <div class="truncate text-sm font-medium">{view.name}</div>
                <div class="truncate text-xs text-muted-foreground">
                  {$t`Based on ${view.base === ViewBase.FieldWorks ? FW_CLASSIC_VIEW.name : FW_LITE_VIEW.name}`}
                </div>
              </div>
              <div class="flex items-center gap-1">
                <Button
                  size="icon"
                  variant="ghost"
                  class="hover:bg-destructive/20! hover:text-destructive"
                  icon="i-mdi-trash-can-outline"
                  onclick={(e: MouseEvent) => {
                    e.stopPropagation();
                    void onDelete(view);
                  }}
                />
                <Icon icon="i-mdi-chevron-right" class="text-muted-foreground" />
              </div>
            </button>
          {/each}
        </div>
      {/if}
    </div>
  </ResponsiveDialog>

<CreateCustomViewDialog bind:open={createOpen} {onCreate} />

{#if editValue}
  <EditCustomViewDialog bind:open={editOpen} value={editValue} {onSave} />
{/if}
