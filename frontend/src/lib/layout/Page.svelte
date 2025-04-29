<script lang="ts">
  import type { Snippet } from 'svelte';
  import SetTitle from './SetTitle.svelte';
  import { PageBreadcrumb } from '$lib/layout';

  interface Props {
    title?: string | undefined;
    wide?: boolean;
    setBreadcrumb?: boolean;
    header?: Snippet;
    children?: Snippet;
  }

  let {
    title = undefined,
    wide = false,
    setBreadcrumb = true,
    header,
    children
  }: Props = $props();

  let maxWidth = $derived(wide ? 'md:max-w-4xl' : 'md:max-w-2xl');
</script>

{#if title}
  <SetTitle {title}/>
    {#if setBreadcrumb}
      <PageBreadcrumb>{title}</PageBreadcrumb>
    {/if}
{/if}

<div class="md:px-8 md:mx-auto {maxWidth} w-full">
  {#if header}
    {@render header?.()}
  {/if}
  <main>
    {@render children?.()}
  </main>
</div>
