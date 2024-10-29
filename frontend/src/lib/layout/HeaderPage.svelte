<script lang="ts">
  import Page from './Page.svelte';

  export let titleText: string;
  export let wide = false;
  export let setBreadcrumb = true;
</script>

<Page title={titleText} {wide} {setBreadcrumb}>
  <svelte:fragment slot="header">
    <slot name="banner" />
    <div class="flex flex-row-reverse flex-wrap justify-between mb-4 gap-y-2 gap-x-4">
      <div class="inline-flex flex-wrap header-actions gap-2 justify-end">
        <slot name="actions" />
      </div>
      <h1 class="text-3xl text-left grow max-w-full flex gap-4 items-end flex-wrap">
        {#if $$slots.title}
          <slot name="title" />
        {:else}
          {titleText}
        {/if}
      </h1>
    </div>
    <slot name="headerContent" />
  </svelte:fragment>
  <div class="divider" />
  <div class="pb-6">
    <slot />
  </div>
</Page>
