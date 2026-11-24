<script lang="ts">
  import AudioInput from '$lib/components/field-editors/audio-input.svelte';
  import DiffMarker from './DiffMarker.svelte';
  import {diffAddedSurface, diffRemovedSurface} from './diff-classes';

  let {before, after}: {before?: string; after?: string} = $props();

  const changed = $derived((before ?? '') !== (after ?? ''));
</script>

{#if !changed}
  <AudioInput audioId={after || undefined} readonly />
{:else}
  <div class="flex flex-col gap-1 w-full">
    {#if before}
      <div class="rounded {diffRemovedSurface}">
        <DiffMarker state="removed" />
        <AudioInput audioId={before} readonly />
      </div>
    {/if}
    {#if after}
      <div class="rounded {diffAddedSurface}">
        <DiffMarker state="added" />
        <AudioInput audioId={after} readonly />
      </div>
    {/if}
  </div>
{/if}
