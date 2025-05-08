<script lang="ts">
  import { Duration, delay } from '$lib/util/time';
  import IconButton from './IconButton.svelte';
  import t from '$lib/i18n';

  var copyingToClipboard = $state(false);
  var copiedToClipboard = $state(false);

  interface Props {
    textToCopy: string;
    delayMs?: Duration | number;
    size?: 'btn-sm' | undefined;
    outline?: boolean;
  }

  const { textToCopy, delayMs = Duration.Default, size = undefined, outline = true }: Props = $props();

  async function copyToClipboard(): Promise<void> {
    copyingToClipboard = true;
    await navigator.clipboard.writeText(textToCopy);
    copiedToClipboard = true;
    copyingToClipboard = false;
    await delay(delayMs);
    copiedToClipboard = false;
  }
</script>

{#if copiedToClipboard}
  <div class="tooltip tooltip-open" data-tip={$t('clipboard.copied')}>
    <IconButton
      fake
      icon="i-mdi-check"
      {size}
      variant={outline ? undefined : 'btn-ghost'}
      class={outline ? 'btn-success' : 'text-success'}
    />
  </div>
{:else}
  <IconButton
    loading={copyingToClipboard}
    icon="i-mdi-content-copy"
    {size}
    variant={outline ? undefined : 'btn-ghost'}
    onclick={copyToClipboard}
  />
{/if}
