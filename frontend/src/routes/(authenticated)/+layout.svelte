<script lang="ts">
  import { Layout } from '$lib/layout';
  import { HomeBreadcrumb } from '$lib/layout';
  interface Props {
    children?: import('svelte').Snippet;
  }

  let { children }: Props = $props();
</script>

<HomeBreadcrumb />
<Layout>
  {@render children?.()}
</Layout>
