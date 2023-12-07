<script lang="ts">
  import SetTitle from './SetTitle.svelte';

  export let title: string | undefined = undefined;
  export let wide = false;

  $: maxWidth = wide ? 'md:max-w-4xl' : 'md:max-w-2xl';
  $: yPadding = !$$slots.header ? 'py-6' : 'pb-6';
</script>

{#if title}
  <SetTitle {title} />
{/if}

<div class="md:px-8 md:mx-auto {maxWidth} w-full">
  {#if $$slots.header}
    <slot name="header" />
  {/if}

  <main class={yPadding}>
    <slot />
  </main>
</div>
