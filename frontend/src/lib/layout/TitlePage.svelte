<script lang="ts">
  import Page from './Page.svelte';
  import PageTitle from './PageTitle.svelte';

  export let title: string;
</script>

<Page {title}>
  <svelte:fragment slot="header">
    <PageTitle {title} />
  </svelte:fragment>
  <div class="py-6">
    <slot />
  </div>
</Page>
