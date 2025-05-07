<script lang="ts">
  import type { Snippet } from 'svelte';
  import Page, { type Props as PageProps } from './Page.svelte';

  export interface Props extends PageProps {
    titleText?: string;
    banner?: Snippet;
    actions?: Snippet;
    headerContent?: Snippet;
  }

  let {
    wide = false,
    setBreadcrumb = true,
    banner,
    actions,
    title,
    titleText,
    headerContent,
    children,
  }: Props = $props();
</script>

<Page title={titleText ?? title} {wide} {setBreadcrumb}>
  {#snippet header()}
    {@render banner?.()}
    <div class="flex flex-row-reverse flex-wrap justify-between mb-4 gap-y-2 gap-x-4">
      <div class="inline-flex flex-wrap header-actions gap-2 justify-end">
        {@render actions?.()}
      </div>
      <h1 class="text-3xl text-left grow max-w-full flex gap-4 items-end flex-wrap">
        {#if title && typeof(title) === 'function'}
          {@render title?.()}
        {:else if title}
          {title}
        {:else if titleText}
          {titleText}
        {/if}
      </h1>
    </div>
    {@render headerContent?.()}
  {/snippet}
  <div class="divider"></div>
  <div class="pb-6">
    {@render children?.()}
  </div>
</Page>
