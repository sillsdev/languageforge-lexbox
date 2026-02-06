<script context="module" lang="ts">
  import {multiClick} from '$lib/attachments/multiClick';
  import {get, writable} from 'svelte/store';

  //indicates that the user is a developer, show them features that are not ready for production, etc.
  //does not indicate this is at development time
  export let isDev = writable(false);

  globalThis.enableDevMode = (enable = true) => {
    isDev.set(enable);
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    enable ? localStorage.setItem('devMode', 'true') : localStorage.removeItem('devMode');
  };
  isDev.set(localStorage.getItem('devMode') === 'true');
  export const devModeToggle = multiClick({
    count: 3,
    timeoutMs: 500,
    onTrigger: () => globalThis.enableDevMode(!get(isDev)),
  });
</script>

<script lang="ts">
  export let invisible = false;
</script>

{#if $isDev}
  <slot />
{:else if invisible}
  <div class="invisible">
    <slot />
  </div>
{/if}
