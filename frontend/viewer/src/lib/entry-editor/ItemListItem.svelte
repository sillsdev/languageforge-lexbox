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
  import {watch} from 'runed';
  import {Icon} from '$lib/components/ui/icon';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {badgeVariants} from '$lib/components/ui/badge';

  let {
    item,
    index,
    items = $bindable(),
    readonly,
    orderable = false,
    getDisplayName,
    onchange,
    menuItems,
  } : ItemListItemProps<T> = $props();

  const count = $derived(items.length);
  const displayName = $derived(getDisplayName(item) || '–');

  let displayItems = $derived(items);
  let displayIndex = $derived(index);

  watch([() => items, () => displayIndex], () => {
    if (index !== displayIndex) {
      const newDisplayItems = [...items];
      newDisplayItems.splice(index, 1);
      newDisplayItems.splice(displayIndex, 0, items[index]);
      displayItems = newDisplayItems;
    }
  });

  function remove(item: T): void {
    items = items.filter((_item) => _item !== item);
    onchange?.(items);
  }

  function move(): void {
    items = displayItems;
    onchange?.(items);
  }

  function swap(): void {
    if (items.length > 2) throw new Error(`Swap does not support more than 2 items (${items.length})`)
    if (items.length < 2) return;
    items = [items[1], items[0]];
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
          {@const first = index === 0}
          {@const last = index === count - 1}
          {#if orderable && count > 1}
            {#if count == 2}
              <DropdownMenu.Item onclick={() => { if (count === 2) { swap(); }}}>
                <Icon icon={first ? 'i-mdi-arrow-right-bold' : 'i-mdi-arrow-left-bold'} />
                {$t`Move`}
              </DropdownMenu.Item>
            {:else}
              <DropdownMenu.Sub>
                <DropdownMenu.SubTrigger>
                  <Icon icon={first ? 'i-mdi-arrow-right-bold' : last ? 'i-mdi-arrow-left-bold' : 'i-mdi-arrow-left-right-bold'} />
                  {$t`Move`}
                </DropdownMenu.SubTrigger>
                <DropdownMenu.SubContent
                    class="grid gap-2 max-h-[50vh] overflow-auto"
                    style="grid-template-columns: max-content 1fr max-content;"
                    onmouseleave={() => displayIndex = index}>
                  {#each displayItems as item, i}
                    {@const reorderName = getDisplayName(item) || '–'}
                    <DropdownMenu.Item class="grid grid-cols-subgrid col-span-full justify-items-start items-center"
                      onmouseover={() => displayIndex = i}
                      onfocus={() => displayIndex = i}
                      onclick={() => {
                        if (i !== index) move();
                      }}>
                      <span class="justify-self-end">{i + 1}:</span>
                      <span class="max-w-52 overflow-x-clip text-ellipsis">{reorderName}</span>
                      {#if i === displayIndex}
                        <Icon icon="i-mdi-chevron-double-left" class="size-4 text-primary" />
                      {/if}
                    </DropdownMenu.Item>
                  {/each}
                </DropdownMenu.SubContent>
              </DropdownMenu.Sub>
            {/if}
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
