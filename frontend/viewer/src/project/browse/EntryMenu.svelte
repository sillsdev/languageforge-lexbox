<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import { type IconClass } from '$lib/icon-class';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import * as Drawer from '$lib/components/ui/drawer';
  import * as ContextMenu from '$lib/components/ui/context-menu';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import { t } from 'svelte-i18n-lingui';
  import { buttonVariants } from '$lib/components/ui/button';
  import Button from '$lib/components/ui/button/button.svelte';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import type {IEntry} from '$lib/dotnet-types';
  import type {Snippet} from 'svelte';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import {useMiniLcmApi} from '$lib/services/service-provider';

  const multiWindowService = useMultiWindowService();
  const dialogsService = useDialogsService();
  const projectEventBus = useProjectEventBus();
  const writingSystemService = useWritingSystemService();
  const miniLcmApi = useMiniLcmApi();

  let { entry, contextMenu = false, children = undefined } = $props<{
    entry: IEntry;
    contextMenu?: boolean;
    children?: Snippet
  }>();

  const headword = $derived((entry && writingSystemService.headword(entry)) || $t`Untitled`);

  let open = $state(false);
  const triggerVariant = buttonVariants({ variant: 'ghost', size: 'sm', class: 'float-right' });

  async function onDelete() {
    if (!await dialogsService.promptDelete($t`Entry`, headword)) return;
    await miniLcmApi.deleteEntry(entry.id);
    projectEventBus.notifyEntryDeleted(entry.id);
  }
</script>

{#snippet items()}
  {@render menuItem('i-mdi-delete', $t`Delete Entry`, onDelete)}
  {@render menuItem('i-mdi-history', $t`History`, () => {})}
  {#if multiWindowService}
    {@render menuItem('i-mdi-open-in-new', $t`Open in new Window`, () => multiWindowService.openEntryInNewWindow(entry.id))}
  {/if}
{/snippet}

{#snippet menuItem(icon: IconClass, label: string, onSelect: () => void)}
  {#if contextMenu}
    <ContextMenu.Item class="cursor-pointer" onclick={onSelect}>
      <Icon icon={icon} class="mr-2"/>
      {label}
    </ContextMenu.Item>
  {:else if !IsMobile.value}
    <DropdownMenu.Item class="cursor-pointer" {onSelect}>
      <Icon {icon} class="mr-2" />
      {label}
    </DropdownMenu.Item>
  {:else}
    <Button
      onclick={onSelect}
      variant="ghost"
      class="w-full justify-start"
      {icon}
    >
      {label}
    </Button>
  {/if}
{/snippet}
{#if contextMenu}
  <ContextMenu.Root>
    <ContextMenu.Trigger>
      {@render children?.()}
    </ContextMenu.Trigger>
    <ContextMenu.Content>
      {@render items()}
    </ContextMenu.Content>
  </ContextMenu.Root>
{:else if !IsMobile.value}
  <DropdownMenu.Root bind:open>
    <DropdownMenu.Trigger class={triggerVariant}>
      <Icon icon="i-mdi-dots-vertical" class="cursor-pointer" />
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="end">
      {@render items()}
    </DropdownMenu.Content>
  </DropdownMenu.Root>
{:else}
  <Drawer.Root bind:open>
    <Drawer.Trigger class={triggerVariant}>
      <Icon icon="i-mdi-dots-vertical" class="cursor-pointer" />
    </Drawer.Trigger>
    <Drawer.Content>
      <div class="mx-auto w-full max-w-sm p-4">
        <Drawer.Header class="justify-items-end">
          <Drawer.Close class={buttonVariants({ variant: 'ghost', size: 'icon' })}><Icon icon="i-mdi-close" /></Drawer.Close>
        </Drawer.Header>
        <div class="space-y-2 flex flex-col gap-2">
          {@render items()}
        </div>
        <Drawer.Footer>
        </Drawer.Footer>
      </div>
    </Drawer.Content>
  </Drawer.Root>
{/if}
