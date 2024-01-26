<script lang="ts">
  import SetTitle from './SetTitle.svelte';
  import { PageBreadcrumb } from '$lib/layout';

  export let title: string | undefined = undefined;
  export let wide = false;
  export let setBreadcrumb = true;

  $: maxWidth = wide ? 'md:max-w-4xl' : 'md:max-w-2xl';
</script>

{#if title}
  <SetTitle {title}/>
    {#if setBreadcrumb}
      <PageBreadcrumb>{title}</PageBreadcrumb>
    {/if}
{/if}

<div class="md:px-8 md:mx-auto {maxWidth} w-full">
  {#if $$slots.header}
    <slot name="header" />
  {/if}
  <main>
    <slot />
  </main>
</div>
