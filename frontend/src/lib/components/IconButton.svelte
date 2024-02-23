<script lang="ts">
  import type { IconString } from '$lib/icons';
  import Loader from './Loader.svelte';

  // See https://icon-sets.iconify.design/mdi/
  export let icon: IconString;
  export let disabled = false;
  export let loading = false;
  export let active = false;
  export let join = false;
  export let variant: 'btn-success' | 'btn-ghost' | undefined = undefined;
  export let size: 'btn-sm' | undefined = undefined;
  export let outline = variant !== 'btn-ghost';
  export let fake = false; // for display purposes only
</script>

<!-- type="button" ensures the button doen't act as a submit button when in a form -->
<button type="button" on:click
  disabled={disabled && !loading}
  class:pointer-events-none={fake || loading}
  class="btn btn-square {variant ?? ''} {size ?? ''} {$$restProps.class ?? ''}"
  class:btn-outline={outline}
  class:btn-active={active}
  class:join-item={join}>
  {#if !loading}
    <span class="{icon} text-lg" />
  {:else}
    <Loader loading />
  {/if}
</button>
