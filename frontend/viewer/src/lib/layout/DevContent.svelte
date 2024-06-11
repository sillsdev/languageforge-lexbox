<script context="module" lang="ts">
  import { writable } from 'svelte/store';

  //indicates that the user is a developer, show them features that are not ready for production, etc.
  //does not indicate this is at development time
  export let isDev = writable(false);

  globalThis.enableDevMode = (enable = true) => {
    isDev.set(enable);
    enable ? localStorage.setItem('devMode', 'true') : localStorage.removeItem('devMode');
  };
  isDev.set(localStorage.getItem('devMode') === 'true');
</script>

{#if $isDev}
  <slot />
{/if}
