<script lang="ts" generics="T">
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import { Icon } from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import ItemList, { type ItemListProps } from './ItemList.svelte';

  import { Link } from 'svelte-routing';

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
    <DropdownMenu.Item>
      <Link to="?entryId={getEntryId(entry)}">
        <Icon icon="i-mdi-book-outline" />
        {$t`Go to ${getHeadword(entry) || '–'}`}
      </Link>
    </DropdownMenu.Item>
  {/snippet}
</ItemList>
