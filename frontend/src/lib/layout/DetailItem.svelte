<script lang="ts">
  import CopyToClipboardButton from '$lib/components/CopyToClipboardButton.svelte';

  export let title: string;
  export let text: string | null | undefined;
  export let copyToClipboard = false;
</script>

<div class="text-lg flex items-center gap-1">
  {title}: {#if text}<span class="text-secondary">{text}</span>{/if}
  {#if copyToClipboard}
    <CopyToClipboardButton textToCopy={text ?? ''} size="btn-sm" outline={false} />
  {/if}
  {#if $$slots.extras}
    <slot name="extras" />
  {/if}
</div>
