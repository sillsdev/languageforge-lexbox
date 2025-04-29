<script lang="ts">
  import Page from './Page.svelte';
  import PageTitle from './PageTitle.svelte';

  interface Props {
    title: string;
    children?: import('svelte').Snippet;
  }

  let { title, children }: Props = $props();
</script>

<Page {title}>
  {#snippet header()}
  
      <PageTitle {title} />
    
  {/snippet}
  <div class="py-6">
    {@render children?.()}
  </div>
</Page>
