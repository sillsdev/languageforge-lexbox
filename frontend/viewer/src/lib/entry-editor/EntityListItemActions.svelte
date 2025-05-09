<script lang="ts">
  import HistoryView from '../history/HistoryView.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import * as Reorderer from '$lib/components/reorderer';
  import {Button} from '$lib/components/ui/button';

  type Props = {
    items: string[];
    i: number;
    id?: string;
    readonly: boolean;
    onmove?: (newIndex: number) => void;
    ondelete?: () => void;
  };

  const {
    items,
    i,
    id,
    readonly,
    onmove,
    ondelete,
  }: Props = $props();

  const features = useFeatures();

  let showHistoryView = $state(false);
</script>

{#if !readonly || features.history}
<div class="flex gap-2">
  {#if !readonly}
    <Reorderer.Root direction="vertical" item={items[i]} {items} getDisplayName={(item) => item} onchange={(_, _fromIndex, newIndex) => onmove?.(newIndex)} />
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
