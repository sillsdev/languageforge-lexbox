<script lang="ts">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Link} from 'svelte-routing';
  import {t} from 'svelte-i18n-lingui';
  import {cn} from '$lib/utils';
  import type {IEntry} from '$lib/dotnet-types';
  import Headwords from '$lib/components/dictionary/Headwords.svelte';
  import EditEntryDialog from '$lib/entry-editor/EditEntryDialog.svelte';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';
  import {entryBrowseParams} from '$lib/utils/search-params';

  // The entry's actions: edit it in a dialog (keeping your place), navigate to it, or open it in a new
  // window. Two shapes: the default trigger shows the headword with all actions in its menu (used where the
  // headword identifies which entry a panel is about, e.g. the two complex-form panels); `generic` renders a
  // split button — "Edit Word" as the visible primary action, navigation behind the caret — for the change-card
  // header, where the summary sentence already names the headword.
  let {entry, generic = false}: {entry: IEntry; generic?: boolean} = $props();

  const multiWindowService = useMultiWindowService();
  const viewService = useViewService();
  const deleted = $derived(Boolean(entry.deletedAt));
  const entryLabel = $derived(pt($t`Entry`, $t`Word`, viewService.currentView));

  let editOpen = $state(false);
</script>

{#if editOpen}
  <!-- Mounted on demand: EditEntryDialog fetches its entry eagerly, and this button renders per change card. -->
  <EditEntryDialog bind:open={editOpen} entryId={entry.id} />
{/if}

{#snippet navItems()}
  <DropdownMenu.Item class="cursor-pointer" onclick={e => e.preventDefault()}>
    {#snippet child({props})}
      <Link {...props} to="./browse?{entryBrowseParams(entry.id)}">
        <Icon icon="i-mdi-link" />
        {$t`Go to ${entryLabel}`}
      </Link>
    {/snippet}
  </DropdownMenu.Item>
  {#if multiWindowService}
    <DropdownMenu.Item class="cursor-pointer" onSelect={() => multiWindowService.openEntryInNewWindow(entry.id)}>
      <Icon icon="i-mdi-open-in-new" />
      {$t`Open in new Window`}
    </DropdownMenu.Item>
  {/if}
{/snippet}

{#if generic}
  <div class="flex items-center">
    <Button variant="secondary" class="rounded-e-none" icon="i-mdi-pencil-outline" onclick={() => editOpen = true}>
      {$t`Edit ${entryLabel}`}
    </Button>
    <DropdownMenu.Root>
      <DropdownMenu.Trigger>
        {#snippet child({props})}
          <Button {...props} variant="secondary" class="rounded-s-none border-s border-background px-1.5"
                  icon="i-mdi-chevron-down" title={$t`More options`} aria-label={$t`More options`} />
        {/snippet}
      </DropdownMenu.Trigger>
      <DropdownMenu.Content align="end">
        <DropdownMenu.Group>
          {@render navItems()}
        </DropdownMenu.Group>
      </DropdownMenu.Content>
    </DropdownMenu.Root>
  </div>
{:else}
  <DropdownMenu.Root>
    <DropdownMenu.Trigger class={cn('text-base w-fit mr-2 justify-between flex-wrap whitespace-break-spaces text-start min-h-max py-1.5', deleted && 'pointer-events-none')}>
      {#snippet child({props})}
        <Button {...props} variant="secondary" size="sm">
          <Headwords {entry} showHomograph placeholder={$t`Untitled`} />
          {#if !deleted}
            <Icon icon="i-mdi-dots-vertical" />
          {:else}
            <span class="text-destructive font-normal">
              ({$t`Deleted`})
            </span>
          {/if}
        </Button>
      {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="start">
      <DropdownMenu.Group>
        <DropdownMenu.Item class="cursor-pointer" onSelect={() => editOpen = true}>
          <Icon icon="i-mdi-pencil-outline" />
          {$t`Edit ${entryLabel}`}
        </DropdownMenu.Item>
        {@render navItems()}
      </DropdownMenu.Group>
    </DropdownMenu.Content>
  </DropdownMenu.Root>
{/if}
