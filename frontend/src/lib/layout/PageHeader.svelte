<script lang="ts">
    import { onMount } from 'svelte';

    let headerElem: HTMLElement | undefined;

    onMount(() => {
      const title = headerElem?.textContent;
      window.document.title = title?.trim() ?? '';
    });
  </script>

  <h1 bind:this={headerElem} class="text-center text-xl">
    <slot />
  </h1>
