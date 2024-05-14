<script lang="ts">
  import HeaderPage from './HeaderPage.svelte';

  export let title: string;
  export let wide = false;
  export let setBreadcrumb = true;
</script>

<HeaderPage {wide} {title} {setBreadcrumb}>
  <svelte:fragment slot="banner"><slot name="banner"></slot></svelte:fragment>
  <svelte:fragment slot="actions"><slot name="actions"></slot></svelte:fragment>
  <svelte:fragment slot="title"><slot name="title"></slot></svelte:fragment>
  <svelte:fragment slot="header-content"><slot name="header-content"></slot></svelte:fragment>
  <slot />
</HeaderPage>
