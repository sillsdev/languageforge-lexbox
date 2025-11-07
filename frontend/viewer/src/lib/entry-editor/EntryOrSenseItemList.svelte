<script lang="ts" generics="T">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import { Icon } from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import ItemList, { type ItemListProps } from './ItemList.svelte';

  import { Link } from 'svelte-routing';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import {useMultiWindowService} from '$lib/services/multi-window-service';

  const currentView = useCurrentView();
  const multiWindowService = useMultiWindowService();

  interface Props extends Omit<ItemListProps<T>, 'getDisplayName'> {
    getEntryId: (item: T) => string;
    getHeadword: (item: T) => string | undefined;
  }

  let { items = $bindable(), getEntryId, getHeadword, ...rest }: Props = $props();
</script>

<ItemList
  bind:items
  {...rest}
  getDisplayName={getHeadword}>
  {#snippet menuItems(entry)}
    {@const entryId = getEntryId(entry)}
    <DropdownMenu.Item class="cursor-pointer">
      {#snippet child({props})}
        <Link {...props} to="browse?entryId={entryId}">
          <Icon icon="i-mdi-book-outline" />
          {$t`Go to ${pt($t`Entry`, $t`Word`, $currentView)}`}
        </Link>
      {/snippet}
    </DropdownMenu.Item>
    {#if multiWindowService}
      <DropdownMenu.Item class="cursor-pointer" onclick={() => multiWindowService.openEntryInNewWindow(entryId)}>
        <Icon icon="i-mdi-open-in-new" />
        {$t`Open in new Window`}
      </DropdownMenu.Item>
    {/if}
  {/snippet}
</ItemList>
