<script lang="ts">
  import type { Snippet } from 'svelte';
  interface Props {
    grid?: boolean;
    children?: Snippet;
  }

  const { grid = false, children }: Props = $props();
</script>

<span class:grid class:inline-flex={!grid} class="badge-list flex-wrap gap-3 justify-items-stretch">
  {@render children?.()}
</span>

<style>
  .badge-list {
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  }
</style>
