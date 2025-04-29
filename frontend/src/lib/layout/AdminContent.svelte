<script lang="ts">
  import { page } from '$app/state'
  interface Props {
    children?: import('svelte').Snippet;
  }

  let { children }: Props = $props();
</script>
<!-- eslint-disable-next-line @typescript-eslint/no-unsafe-argument -->
{#if page.data.user?.isAdmin}
    {@render children?.()}
{/if}
