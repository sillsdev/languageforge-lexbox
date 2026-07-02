<script lang="ts">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Link} from 'svelte-routing';
  import {t} from 'svelte-i18n-lingui';
  import {cn} from '$lib/utils';
  import type {IEntry} from '$lib/dotnet-types';
  import Headwords from '$lib/components/dictionary/Headwords.svelte';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';
  import {entryBrowseParams} from '$lib/utils/search-params';

  // The entry's headword as a menu button: open in a new window, or navigate to it in the browser. Shared by
  // the per-change preview and the collapsed create-entry / create-sense previews.
  let {entry}: {entry: IEntry} = $props();

  const multiWindowService = useMultiWindowService();
  const viewService = useViewService();
  const deleted = $derived(Boolean(entry.deletedAt));
</script>

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
      {#if multiWindowService}
        <DropdownMenu.Item class="cursor-pointer" onSelect={() => multiWindowService.openEntryInNewWindow(entry.id)}>
          <Icon icon="i-mdi-open-in-new" />
          {$t`Open in new Window`}
        </DropdownMenu.Item>
      {/if}
      <DropdownMenu.Item class="cursor-pointer" onclick={e => e.preventDefault()}>
        {#snippet child({props})}
          <Link {...props} to="./browse?{entryBrowseParams(entry.id)}">
            <Icon icon="i-mdi-link" />
            {$t`Go to ${pt($t`Entry`, $t`Word`, viewService.currentView)}`}
          </Link>
        {/snippet}
      </DropdownMenu.Item>
    </DropdownMenu.Group>
  </DropdownMenu.Content>
</DropdownMenu.Root>
