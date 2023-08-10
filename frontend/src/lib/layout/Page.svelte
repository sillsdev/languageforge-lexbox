<script lang="ts">
  import PageHeader from './PageHeader.svelte';

  export let wide = false;

  $: maxWidth = wide ? 'md:max-w-4xl' : 'md:max-w-2xl';
  $: yPadding = $$slots.header ? 'py-6' : 'pb-6';
</script>

{#if $$slots.header}
  <PageHeader>
    <slot name="header" />
  </PageHeader>
{/if}

<main class="md:px-8 {yPadding} md:mx-auto {maxWidth}">
  <slot />
</main>
