<script lang="ts" module>
  import {type Snippet} from 'svelte';

  export type ItemListItemProps<T> = {
    item: T;
    index: number;
    items: T[];
    readonly: boolean;
    orderable?: boolean;
    getDisplayName: (item: T) => string | undefined;
    onchange?: (value: T[]) => void;
    menuItems?: Snippet<[T]>;
    actions?: Snippet;
  };
</script>

<script lang="ts" generics="T">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {badgeVariants} from '$lib/components/ui/badge';
  import * as Reorderer from '$lib/components/reorderer';

  let {
    item,
    items = $bindable(),
    readonly,
    orderable = false,
    getDisplayName,
    onchange,
    menuItems,
  } : ItemListItemProps<T> = $props();

  const displayName = $derived(getDisplayName(item) || '–');

  function remove(item: T): void {
    items = items.filter((_item) => _item !== item);
    onchange?.(items);
  }
</script>

<DropdownMenu.Root>
  <DropdownMenu.Trigger class={badgeVariants({ class: 'space-x-2 pr-1 text-sm' })}>
    <span>{displayName}</span>
    <Icon icon="i-mdi-dots-vertical" />
  </DropdownMenu.Trigger>
  <DropdownMenu.Content>
    <DropdownMenu.Group>
      <DropdownMenu.Group>
        {@render menuItems?.(item)}
      </DropdownMenu.Group>
      {#if !readonly}
        <DropdownMenu.Group>
          {#if orderable}
          <Reorderer.Root {item} {items} {getDisplayName} {onchange}>
            {#snippet swapper({props, arrow})}
              <DropdownMenu.Item {...props}>
                {@render arrow()}
                {$t`Move`}
              </DropdownMenu.Item>
            {/snippet}
            {#snippet children({first, last})}
              <Reorderer.Trigger {first} {last}>
                {#snippet child({arrow})}
                  <DropdownMenu.Sub>
                    <DropdownMenu.SubTrigger>
                      {@render arrow()}
                      {$t`Move`}
                    </DropdownMenu.SubTrigger>
                    <Reorderer.ItemList {item} bind:items {getDisplayName}>
                      {#snippet child({props, itemList})}
                      <DropdownMenu.SubContent {...props}>
                        {@render itemList()}
                      </DropdownMenu.SubContent>
                      {/snippet}
                    </Reorderer.ItemList>
                  </DropdownMenu.Sub>
                {/snippet}
              </Reorderer.Trigger>
            {/snippet}
          </Reorderer.Root>
          {/if}
          <DropdownMenu.Item onSelect={() => remove(item)}>
            <Icon icon="i-mdi-trash-can-outline" />
            {$t`Remove`}
          </DropdownMenu.Item>
        </DropdownMenu.Group>
      {/if}
    </DropdownMenu.Group>
  </DropdownMenu.Content>
</DropdownMenu.Root>
