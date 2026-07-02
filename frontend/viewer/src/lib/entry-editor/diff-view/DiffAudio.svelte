<script lang="ts">
  import AudioInput from '$lib/components/field-editors/audio-input.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {diffEmpty} from './diff-classes';

  // A diff of an audio writing-system value. The value is a sil-media URI (playable directly via AudioInput,
  // which resolves it through the media service). On a change we show the replaced recording marked removed
  // above the current one; a plain add/unchanged shows a single player.
  let {before, after}: {before?: string; after?: string} = $props();

  const changed = $derived((before ?? '') !== (after ?? ''));
</script>

{#if !before && !after}
  <span class={diffEmpty}>&nbsp;</span>
{:else}
  <div class="flex flex-col items-start gap-1">
    {#if before && changed}
      <div class="flex items-center gap-1">
        <span class="text-xs text-destructive">{$t`Removed`}</span>
        <AudioInput audioId={before} readonly />
      </div>
    {/if}
    {#if after}
      <AudioInput audioId={after} readonly />
    {:else if before && !changed}
      <AudioInput audioId={before} readonly />
    {/if}
  </div>
{/if}
