<script lang="ts" module>
  export type ReordererItemListProps = Omit<HTMLAttributes<HTMLElement>, 'onchange'> & {
    id?: string;
  };
</script>

<script lang="ts" generics="T">
  import {Icon} from '$lib/components/ui/icon';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import type {HTMLAttributes} from 'svelte/elements';
  import {mergeProps} from 'bits-ui';
  import {cn} from '$lib/utils';
  import {useReordererItemList} from './reorderer.svelte';

  let {...rest}: ReordererItemListProps = $props();

  const itemListState = useReordererItemList<T>();
  const {item, items, getDisplayName, onchange} = $derived(itemListState);

  const currIndex = $derived(items.indexOf(item));
  let displayIndex = $derived(currIndex);
  let displayItems = $derived.by(() => {
    const newDisplayItems = [...items];
    newDisplayItems.splice(currIndex, 1);
    newDisplayItems.splice(displayIndex, 0, items[currIndex]);
    return newDisplayItems;
  });

  function move(newIndex: number): void {
    if (newIndex !== displayIndex) throw new Error(`newIndex (${newIndex}) must match displayIndex (${displayIndex})`);
    const from = currIndex;
    const to = newIndex;
    itemListState.items = displayItems; // this immediately updates/changes currIndex
    onchange?.(items, from, to);
  }

  const mergedProps = $derived(
    mergeProps(
      {
        class: 'grid gap-2 max-h-[50vh] overflow-auto',
        style: 'grid-template-columns: max-content 1fr max-content;',
        onmouseleave: () => (displayIndex = currIndex),
      },
      rest,
    ),
  );
</script>

<DropdownMenu.Group {...mergedProps}>
  <!-- using an each-key confuses the dropdown menu when we shuffle things around -->
  <!-- eslint-disable-next-line svelte/require-each-key -->
  {#each displayItems as item, i}
    {@const reorderName = getDisplayName(item) || '–'}
    <DropdownMenu.Item
      class="grid grid-cols-subgrid col-span-full justify-items-start items-center"
      data-current={i === currIndex ? '' : null}
      onfocus={() => (displayIndex = i)}
      onSelect={() => {
        if (i !== currIndex) move(i);
      }}
    >
      <span class="justify-self-end">{i + 1}:</span>
      <span class="max-w-52 overflow-x-clip text-ellipsis whitespace-nowrap">{reorderName}</span>
      {#if i === displayIndex || i === currIndex}
        <Icon icon="i-mdi-chevron-double-left" class={cn('size-4', i === displayIndex && 'text-primary')} />
      {/if}
    </DropdownMenu.Item>
  {/each}
</DropdownMenu.Group>
