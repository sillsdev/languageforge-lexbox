<script module lang="ts">
  import { writable } from 'svelte/store';
  import { browser } from '$app/environment';

  //indicates that the user is a developer, show them features that are not ready for production, etc.
  //does not indicate this is at development time
  export let isDev = writable(false);

  if (browser) {
    globalThis.enableDevMode = (enable = true) => {
      isDev.set(enable);
      // eslint-disable-next-line @typescript-eslint/no-unused-expressions
      enable ? localStorage.setItem('devMode', 'true') : localStorage.removeItem('devMode');
    };
    isDev.set(localStorage.getItem('devMode') === 'true');
  }
</script>

<script lang="ts">
  import type { Snippet } from 'svelte';
  interface Props {
    children?: Snippet;
  }

  let { children }: Props = $props();
</script>

{#if $isDev}
  {@render children?.()}
{/if}
