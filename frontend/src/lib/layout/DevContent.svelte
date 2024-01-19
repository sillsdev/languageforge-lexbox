<script context="module" lang="ts">
  import { writable } from 'svelte/store';
  import { browser } from '$app/environment';

  //indicates that the user is a developer, show them features that are not ready for production, etc.
  //does not indicate this is at development time
  export let isDev = writable(false);

  if (browser) {
    globalThis.enableDevMode = () => {
      isDev.set(true);
      localStorage.setItem('devMode', 'true');
    };
    isDev.set(localStorage.getItem('devMode') === 'true');
  }
</script>

{#if $isDev}
  <slot />
{/if}
