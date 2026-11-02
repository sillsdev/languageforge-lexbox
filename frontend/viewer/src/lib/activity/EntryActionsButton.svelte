<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Link} from 'svelte-routing';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {Button} from '$lib/components/ui/button';
  import * as ButtonGroup from '$lib/components/ui/button-group';
  import {Icon} from '$lib/components/ui/icon';
  import EditEntryDialog from '$lib/entry-editor/EditEntryDialog.svelte';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';

  let {entryId, label, headword}: {
    entryId: string;
    /** Defaults to Edit Entry/Word */
    label?: string;
    /** Appended to the accessible names, for cards with several of these buttons */
    headword?: string;
  } = $props();

  const multiWindowService = useMultiWindowService();
  const viewService = useViewService();

  let editOpen = $state(false);
  const editText = $derived(label ?? pt($t`Edit Entry`, $t`Edit Word`, viewService.currentView));
</script>

{#if editOpen}
  <!-- Mounted on demand: the dialog fetches its entry eagerly, and every change card has this button -->
  <EditEntryDialog bind:open={editOpen} {entryId} />
{/if}

<ButtonGroup.Root>
  <!-- The accessible name starts with the visible text (WCAG label in name) -->
  <Button variant="secondary" size="xs" icon="i-mdi-pencil-outline" class="flex-1 justify-start" onclick={() => editOpen = true}
          aria-label={headword ? `${editText}: ${headword}` : undefined}>
    {editText}
  </Button>
  <DropdownMenu.Root>
    <DropdownMenu.Trigger>
      {#snippet child({props})}
        <Button {...props} variant="secondary" size="xs" class="border-s border-background px-1.5"
                icon="i-mdi-chevron-down" aria-label={headword ? $t`More options for ${headword}` : $t`More options`} />
      {/snippet}
    </DropdownMenu.Trigger>
    <DropdownMenu.Content align="end">
      <DropdownMenu.Item class="cursor-pointer">
        {#snippet child({props})}
          <Link {...props} to="./browse?{entryBrowseParams(entryId)}">
            <Icon icon="i-mdi-link" />
            {pt($t`Go to Entry`, $t`Go to Word`, viewService.currentView)}
          </Link>
        {/snippet}
      </DropdownMenu.Item>
      {#if multiWindowService}
        <DropdownMenu.Item class="cursor-pointer" onSelect={() => multiWindowService.openEntryInNewWindow(entryId)}>
          <Icon icon="i-mdi-open-in-new" />
          {$t`Open in new Window`}
        </DropdownMenu.Item>
      {/if}
    </DropdownMenu.Content>
  </DropdownMenu.Root>
</ButtonGroup.Root>
