<script lang="ts">
  import Page from './Page.svelte';

  export let title: string;
  export let wide = false;
</script>

<Page {title} {wide}>
  <svelte:fragment slot="header">
    <slot name="banner" />
    <div class="flex flex-row-reverse flex-wrap justify-between mb-4 gap-y-2 gap-x-4">
      <div class="contents header-actions">
        <slot name="actions" />
      </div>
      <div class="text-3xl text-left grow max-w-full flex items-end flex-wrap">
        <slot name="title" />
      </div>
    </div>
    <slot name="header-content" />
  </svelte:fragment>
  <div class="divider" />
  <slot />
</Page>

<style lang="postcss">
  :global(.header-actions .btn) {
    @apply btn-sm sm:btn-md;
  }
</style>
