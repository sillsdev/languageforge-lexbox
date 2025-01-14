<script lang="ts">
  import CopyToClipboardButton from '$lib/components/CopyToClipboardButton.svelte';
  import Loader from '$lib/components/Loader.svelte';

  export let title: string;
  export let text: string | null | undefined = undefined;
  export let copyToClipboard = false;
  export let loading = false;
</script>

<div class="text-lg flex items-center gap-2 detail-item">
  {title}:
  {#if loading}
    <Loader loading size="loading-xs" />
  {:else if text}
    <span class="text-secondary">{text}</span>
  {:else}
    <slot/>
  {/if}
  {#if copyToClipboard}
    <CopyToClipboardButton textToCopy={text ?? ''} size="btn-sm" outline={false} />
  {/if}
  {#if $$slots.extras}
    <slot name="extras" />
  {/if}
</div>
