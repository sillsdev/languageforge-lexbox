<script lang="ts">
  import type { Snippet } from 'svelte';
  import Page, { type Props as PageProps } from './Page.svelte';

  export interface Props extends Omit<PageProps, 'title'> {
    titleText: string;
    banner?: Snippet;
    actions?: Snippet;
    title?: Snippet;
    headerContent?: Snippet;
  }

  const { titleText, banner, actions, title, headerContent, children, ...rest }: Props = $props();
</script>

<Page {...rest} title={titleText}>
  {#snippet header()}
    {@render banner?.()}
    <div class="flex flex-row-reverse flex-wrap justify-between mb-4 gap-y-2 gap-x-4">
      <div class="inline-flex flex-wrap header-actions gap-2 justify-end">
        {@render actions?.()}
      </div>
      <h1 class="text-3xl text-left grow max-w-full flex gap-4 items-end flex-wrap">
        {#if title}
          {@render title?.()}
        {:else}
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
