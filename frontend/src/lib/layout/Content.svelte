<script lang="ts">
  import type { Snippet } from 'svelte';
  interface Props {
    children?: Snippet;
  }

  const { children }: Props = $props();
</script>

<main class="max-w-none px-2 md:px-6 py-8 flex-grow flex flex-col">
  {@render children?.()}
</main>
