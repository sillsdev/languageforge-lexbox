<script lang="ts">
  import type { Snippet } from 'svelte';
  import { page } from '$app/state';
  interface Props {
    children?: Snippet;
  }

  const { children }: Props = $props();
</script>

<!-- eslint-disable-next-line @typescript-eslint/no-unsafe-argument -->
{#if page.data.user?.isAdmin}
  {@render children?.()}
{/if}
