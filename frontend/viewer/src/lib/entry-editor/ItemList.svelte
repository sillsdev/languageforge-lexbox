<script lang="ts" module>
  import type {Snippet} from 'svelte';

  export interface ItemListProps<T> extends Omit<ItemListItemProps<T>, 'item'> {
    actions?: Snippet;
  }
</script>

<script lang="ts" generics="T">

  import ItemListItem, {type ItemListItemProps} from './ItemListItem.svelte';

  let {
    items = $bindable(),
    actions,
    readonly = false,
    ...rest
  } : ItemListProps<T> = $props();

</script>

<div class="flex gap-2 flex-wrap items-center">
  {#each items as item (item)}
    <ItemListItem
      {item}
      bind:items
      {readonly}
      {...rest} />
  {/each}
  {#if !readonly}
    <div class="grow text-right">
      {@render actions?.()}
    </div>
  {/if}
</div>
