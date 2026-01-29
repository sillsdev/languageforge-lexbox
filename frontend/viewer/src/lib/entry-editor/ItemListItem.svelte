<script lang="ts" module>
  import {type Snippet} from 'svelte';

  export type ItemListItemProps<T> = {
    item: T;
    items: T[];
    readonly: boolean;
    orderable?: boolean;
    getDisplayName: (item: T) => string | undefined;
    onchange?: (value: T[]) => void;
    menuItems?: Snippet<[T]>;
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
          <DropdownMenu.Sub>
            <Reorderer.Root {item} bind:items {getDisplayName} {onchange}>
              <Reorderer.Trigger>
                {#snippet child({arrowIcon, props})}
                  <DropdownMenu.SubTrigger {...props}>
                    <Icon icon={arrowIcon} />
                    {$t`Move`}
                  </DropdownMenu.SubTrigger>
                {/snippet}
              </Reorderer.Trigger>
              <DropdownMenu.SubContent>
                <Reorderer.ItemList />
              </DropdownMenu.SubContent>
            </Reorderer.Root>
          </DropdownMenu.Sub>
          {/if}
          <DropdownMenu.Item class="cursor-pointer" onSelect={() => remove(item)}>
            <Icon icon="i-mdi-trash-can-outline" />
            {$t`Remove`}
          </DropdownMenu.Item>
        </DropdownMenu.Group>
      {/if}
    </DropdownMenu.Group>
  </DropdownMenu.Content>
</DropdownMenu.Root>
