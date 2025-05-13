<script lang="ts">
  import {NewTabLinkMarkdown} from '$lib/components/Markdown';
  import type { ErrorMessage } from './types';

  interface Props {
    error?: ErrorMessage;
    markdown?: boolean;
    right?: boolean; // when the form submit button is on the right
  }

  let { error = undefined, markdown = false, right = false }: Props = $props();
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
