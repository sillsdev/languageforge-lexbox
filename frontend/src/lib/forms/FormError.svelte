<script lang="ts">
  import Markdown from 'svelte-exmarkdown';
  import type { ErrorMessage } from './types';
  import { NewTabLinkRenderer } from '$lib/components/Markdown';

  export let error: ErrorMessage = undefined;
  export let markdown = false;
  export let right = false; // when the form submit button is on the right
</script>

{#if error}
  <span class="label text-lg text-error" class:justify-end={right}>
    {#if markdown}
      <Markdown md={error} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
    {:else}
      {error}
    {/if}
  </span>
{/if}
