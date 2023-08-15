<script lang="ts">
  import Page from './Page.svelte';

  export let title: string;
  export let wide = false;
</script>

<Page {title} {wide}>
  <svelte:fragment slot="header">
    <slot name="banner" />
    <div class="flex flex-row-reverse flex-wrap justify-between mb-4 gap-y-2 gap-x-4">
      <div class="inline-flex flex-wrap header-actions gap-2">
        <slot name="actions" />
      </div>
      <h1 class="text-3xl text-left grow max-w-full flex items-end flex-wrap">
        {#if $$slots.title}
          <slot name="title" />
        {:else}
          {title}
        {/if}
      </h1>
    </div>
    <slot name="header-content" />
  </svelte:fragment>
  <div class="divider" />
  <div class="pb-6">
    <slot />
  </div>
</Page>
