<script lang="ts" generics="T">
  import HistoryView from '../history/HistoryView.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import { Reorderer } from '$lib/components/reorderer';
  import {Button} from '$lib/components/ui/button';

  type Props<T> = {
    items: T[];
    i: number;
    id?: string;
    getDisplayName: (item: T) => string | undefined;
    readonly: boolean;
    onmove?: (newIndex: number) => void;
    ondelete?: () => void;
  };

  let {
    items = $bindable(),
    i,
    id,
    getDisplayName,
    readonly,
    onmove,
    ondelete,
  }: Props<T> = $props();

  const features = useFeatures();

  let showHistoryView = $state(false);
</script>

{#if !readonly || features.history}
<div class="flex gap-2">
  {#if !readonly}
    <Reorderer
      direction="vertical"
      item={items[i]}
      {items}
      {getDisplayName}
      onchange={(_newItems, _fromIndex, newIndex) => onmove?.(newIndex)}
    />
  {/if}
  {#if features.history && id}
    <Button onclick={() => showHistoryView = true} size="icon" variant="secondary" icon="i-mdi-history" />
    <HistoryView bind:open={showHistoryView} {id}/>
  {/if}
  {#if !readonly}
    <Button onclick={ondelete} size="icon" variant="secondary" icon="i-mdi-trash-can" />
  {/if}
</div>
{/if}
