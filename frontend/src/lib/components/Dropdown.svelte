<script lang="ts">
  import type { Snippet } from 'svelte';
  import { overlay } from '$lib/overlay';

  interface Props {
    disabled?: boolean;
    children?: Snippet;
    content?: Snippet;
  }

  const { disabled = false, children, content }: Props = $props();
</script>

<div class="dropdown-container inline-flex" use:overlay={{ disabled, closeClickSelector: '.menu li' }}>
  {@render children?.()}
  <div class="overlay-content">
    {@render content?.()}
  </div>
</div>

<style>
  :global(.dropdown-container > *) {
    max-width: 100%;
    flex-grow: 1;
  }
</style>
