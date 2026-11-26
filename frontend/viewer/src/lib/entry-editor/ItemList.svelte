<script lang="ts" module>
  import type {Snippet} from 'svelte';

  export interface ItemListProps<T> extends Omit<ItemListItemProps<T>, 'item'> {
    actions?: Snippet;
  }
</script>

<script lang="ts" generics="T">

  import ItemListItem, {type ItemListItemProps} from './ItemListItem.svelte';
  import ReadonlyField from '$lib/components/field-editors/readonly-field.svelte';

  let {
    items = $bindable(),
    actions,
    readonly = false,
    ...rest
  } : ItemListProps<T> = $props();

</script>

{#if readonly && !items.length}
  <ReadonlyField />
{:else}
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
{/if}
