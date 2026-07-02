<script lang="ts">
  import AudioInput from '$lib/components/field-editors/audio-input.svelte';
  import {diffAddedSurface, diffRemovedSurface} from './diff-classes';

  // A diff of an audio writing-system value. The value is a sil-media URI, played directly via AudioInput
  // (which also renders the editor's "No audio" placeholder when empty). On a change the replaced/removed
  // recording sits in a destructive gutter above the added one in an emerald gutter.
  let {before, after}: {before?: string; after?: string} = $props();

  const changed = $derived((before ?? '') !== (after ?? ''));
</script>

{#if !changed}
  <!-- Unchanged (or empty on both sides): AudioInput shows the player, or its own "No audio" placeholder. -->
  <AudioInput audioId={after || undefined} readonly />
{:else}
  <div class="flex flex-col items-start gap-1">
    {#if before}
      <div class="rounded border-s-2 border-destructive ps-2 {diffRemovedSurface}">
        <AudioInput audioId={before} readonly />
      </div>
    {/if}
    {#if after}
      <div class="rounded border-s-2 border-emerald-500 ps-2 {diffAddedSurface}">
        <AudioInput audioId={after} readonly />
      </div>
    {/if}
  </div>
{/if}
