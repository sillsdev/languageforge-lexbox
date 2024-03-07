<script lang="ts">
  import { Duration, delay } from '$lib/util/time';
  import IconButton from './IconButton.svelte';
  import t from '$lib/i18n';

  var copyingToClipboard = false;
  var copiedToClipboard = false;

  export let getTextToCopy: () => string;
  export let delayMs: Duration | number = Duration.Default;
  export let size: 'btn-sm' | undefined = undefined;
  export let outline: boolean = true;

  async function copyToClipboard(): Promise<void> {
    copyingToClipboard = true;
    await navigator.clipboard.writeText(getTextToCopy());
    copiedToClipboard = true;
    copyingToClipboard = false;
    await delay(delayMs);
    copiedToClipboard = false;
  }
</script>

{#if copiedToClipboard}
<div class="tooltip tooltip-open" data-tip={$t('clipboard.copied')}>
  <IconButton fake icon="i-mdi-check" {size} variant={outline ? undefined : 'btn-ghost'} class={outline ? 'btn-success' : 'text-success'} />
</div>
{:else}
  <IconButton
  loading={copyingToClipboard}
  icon="i-mdi-content-copy"
  {size}
  variant={outline ? undefined : 'btn-ghost'}
  on:click={copyToClipboard}
/>
{/if}
