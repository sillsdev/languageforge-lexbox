<script module lang="ts">
  import {multiClick} from '$lib/attachments/multiClick';
  import {get, writable} from 'svelte/store';

  //indicates that the user is a developer, show them features that are not ready for production, etc.
  //does not indicate this is at development time
  export const isDev = writable(false);

  globalThis.enableDevMode = (enable = true) => {
    isDev.set(enable);
    if (enable) {
      localStorage.setItem('devMode', 'true');
    } else {
      localStorage.removeItem('devMode');
    }
  };
  isDev.set(localStorage.getItem('devMode') === 'true');
  export const devModeToggle = multiClick({
    count: 5,
    timeoutMs: 500,
    onTrigger: () => globalThis.enableDevMode(!get(isDev)),
  });
</script>

<script lang="ts">
  import type { Snippet } from 'svelte';
  interface Props {
    invisible?: boolean;
    children?: Snippet;
  }

  const { invisible = false, children }: Props = $props();
</script>

{#if $isDev}
  {@render children?.()}
{:else if invisible}
  <div class="invisible">
    {@render children?.()}
  </div>
{/if}
