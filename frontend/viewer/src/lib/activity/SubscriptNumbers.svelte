<script lang="ts">
  import HomographNumber from '$lib/components/dictionary/HomographNumber.svelte';
  import {parseSubscriptNumbers} from '$lib/components/dictionary/homograph-subscript';

  let {text}: {text: string} = $props();
</script>

<!-- Activity labels from the backend carry homograph and sense numbers as Unicode subscripts (plain text, so they also work in titles); render them like HomographNumber -->
{#each parseSubscriptNumbers(text) as part, i (i)}{#if typeof part === 'number'}<HomographNumber homographNumber={part} />{:else}{part}{/if}{/each}
