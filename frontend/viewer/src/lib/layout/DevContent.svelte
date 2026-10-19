<script module lang="ts">
  import {multiClick} from '$lib/attachments/multiClick';
  import {DEV_CHANNEL} from '$lib/feature-flags/feature-flags';
  import {featureFlags} from '$lib/feature-flags/feature-flags.svelte';

  // Indicates that the user is on the `dev` release channel. Show them features
  // that are not ready for production, etc. Does not indicate this is at development time.
  globalThis.enableDevMode = (enable = true) => {
    featureFlags.channel = enable ? DEV_CHANNEL : '';
  };
  export const devModeToggle = multiClick({
    count: 5,
    timeoutMs: 500,
    onTrigger: () => globalThis.enableDevMode(!featureFlags.isDev),
  });
</script>

<script lang="ts">
  import type {Snippet} from 'svelte';

  interface Props {
    invisible?: boolean;
    children?: Snippet;
  }

  const {invisible = false, children}: Props = $props();
</script>

{#if featureFlags.isDev}
  {@render children?.()}
{:else if invisible}
  <div class="invisible">
    {@render children?.()}
  </div>
{/if}
