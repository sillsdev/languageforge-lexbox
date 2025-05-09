<script lang="ts" module>
  import {type Snippet} from 'svelte';

  export type ReordererItemListProps<T> = Omit<HTMLAttributes<HTMLElement>, 'onchange'> & {
    item: T;
    items: T[];
    getDisplayName: (item: T) => string | undefined;
    onchange?: (value: T[], fromIndex: number, toIndex: number) => void;
    child?: Snippet<[{ props: Record<string, unknown>, itemList: Snippet }]>;
    id?: string;
  };
</script>

<script lang="ts" generics="T">
  import {watch} from 'runed';
  import {Icon} from '$lib/components/ui/icon';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import type {HTMLAttributes} from 'svelte/elements';
  import {mergeProps} from 'bits-ui';
  import {cn} from '$lib/utils';

  let {
    item,
    items = $bindable<T[]>(),
    getDisplayName,
    onchange,
    child,
    ...rest
  } : ReordererItemListProps<T> = $props();

  const currIndex = $derived(items.indexOf(item));
  let displayItems = $derived(items);
  let displayIndex = $derived(currIndex);

  watch([() => items, () => displayIndex], () => {
    const newDisplayItems = [...items];
    newDisplayItems.splice(currIndex, 1);
    newDisplayItems.splice(displayIndex, 0, items[currIndex]);
    displayItems = newDisplayItems;
  });

  function move(): void {
    items = displayItems;
    onchange?.(items, currIndex, displayIndex);
  }

  const mergedProps = $derived(mergeProps({
    class: 'grid gap-2 max-h-[50vh] overflow-auto',
    style: 'grid-template-columns: max-content 1fr max-content;',
    onmouseleave: () => displayIndex = currIndex,
  }, rest));
</script>

{#snippet itemList()}
  {#each displayItems as item, i}
    {@const reorderName = getDisplayName(item) || '–'}
    <DropdownMenu.Item class="grid grid-cols-subgrid col-span-full justify-items-start items-center cursor-pointer"
      onmouseover={() => displayIndex = i}
      onfocus={() => displayIndex = i}
      onSelect={() => {
        if (i !== currIndex) move();
      }}>
      <span class="justify-self-end">{i + 1}:</span>
      <span class="max-w-52 overflow-x-clip text-ellipsis whitespace-nowrap">{reorderName}</span>
      {#if i === displayIndex || i === currIndex}
        <Icon icon="i-mdi-chevron-double-left" class={cn('size-4', i === displayIndex && 'text-primary')} />
      {/if}
    </DropdownMenu.Item>
  {/each}
{/snippet}

{#if child}
  {@render child({ props: mergedProps, itemList })}
{:else}
  <DropdownMenu.Group {...mergedProps}>
    {@render itemList()}
  </DropdownMenu.Group>
{/if}
