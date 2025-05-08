<script lang="ts">
  import type { Snippet } from 'svelte';
  import Page from './Page.svelte';
  import PageTitle from './PageTitle.svelte';

  interface Props {
    title: string;
    children?: Snippet;
  }

  const { title, children }: Props = $props();
</script>

<Page {title}>
  {#snippet header()}
    <PageTitle {title} />
  {/snippet}
  <div class="py-6">
    {@render children?.()}
  </div>
</Page>
