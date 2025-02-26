<script lang="ts">
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';
  import type { ErrorMessage } from './types';

  export let error: ErrorMessage = undefined;
  export let markdown = false;
  export let right = false; // when the form submit button is on the right
</script>

{#if error}
  <span class="label text-lg text-error pl-0" class:justify-end={right}>
    {#if markdown}
      <NewTabLinkMarkdown md={error} />
    {:else}
      {error}
    {/if}
  </span>
{/if}
