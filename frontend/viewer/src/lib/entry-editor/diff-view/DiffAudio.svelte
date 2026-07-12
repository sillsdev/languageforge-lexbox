<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import AudioInput from '$lib/components/field-editors/audio-input.svelte';
  import {diffAddedSurface, diffRemovedSurface} from './diff-classes';

  // A diff of an audio writing-system value. The value is a sil-media URI, played directly via AudioInput
  // (which also renders the editor's "No audio" placeholder when empty). On a change the replaced/removed
  // recording sits in a destructive gutter above the added one in an emerald gutter; the gutter colour is
  // the only visual cue, so each box also carries a screen-reader label.
  let {before, after}: {before?: string; after?: string} = $props();

  const changed = $derived((before ?? '') !== (after ?? ''));
</script>

{#if !changed}
  <!-- Unchanged (or empty on both sides): AudioInput shows the player, or its own "No audio" placeholder. -->
  <AudioInput audioId={after || undefined} readonly />
{:else}
  <div class="flex flex-col gap-1 w-full">
    {#if before}
      <div class="rounded {diffRemovedSurface}">
        <span class="sr-only">{$t`Removed`}</span>
        <AudioInput audioId={before} readonly />
      </div>
    {/if}
    {#if after}
      <div class="rounded {diffAddedSurface}">
        <span class="sr-only">{$t`Added`}</span>
        <AudioInput audioId={after} readonly />
      </div>
    {/if}
  </div>
{/if}
