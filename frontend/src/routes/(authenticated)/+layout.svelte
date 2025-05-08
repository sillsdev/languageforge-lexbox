<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Layout } from '$lib/layout';
  import { HomeBreadcrumb } from '$lib/layout';
  interface Props {
    children?: Snippet;
  }

  let { children }: Props = $props();
</script>

<HomeBreadcrumb />
<Layout>
  {@render children?.()}
</Layout>
