<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Layout } from '$lib/layout';
  interface Props {
    children?: Snippet;
  }

  const { children }: Props = $props();
</script>

<Layout hideToolbar>
  {@render children?.()}
</Layout>
