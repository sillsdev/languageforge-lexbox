<script lang="ts">
  import type { Snippet } from 'svelte';
  import CopyToClipboardButton from '$lib/components/CopyToClipboardButton.svelte';
  import Loader from '$lib/components/Loader.svelte';

  interface Props {
    title: string;
    text?: string | null | undefined;
    copyToClipboard?: boolean;
    loading?: boolean;
    wrap?: boolean;
    children?: Snippet;
    extras?: Snippet;
  }

  let {
    title,
    text = undefined,
    copyToClipboard = false,
    loading = false,
    wrap = false,
    children,
    extras,
  }: Props = $props();
</script>

<div class="text-lg flex items-center gap-2 detail-item whitespace-nowrap" class:flex-wrap={wrap}>
  {title}:
  {#if loading}
    <Loader loading size="loading-xs" />
  {:else if text}
    <span class="text-secondary x-ellipsis">{text}</span>
  {:else}
    {@render children?.()}
  {/if}
  {#if copyToClipboard}
    <CopyToClipboardButton textToCopy={text ?? ''} size="btn-sm" outline={false} />
  {/if}
  {#if extras}
    {@render extras?.()}
  {/if}
</div>
