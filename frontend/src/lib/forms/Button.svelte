<script lang="ts">
  import Loader from '$lib/components/Loader.svelte';

  export let loading = false;
  export let active = false;
  export let variant: 'btn-primary' | 'btn-success' | 'btn-error' | 'btn-ghost' | 'btn-warning' | 'btn-accent' | undefined = undefined;
  export let outline = false;
  export let type: undefined | 'submit' = undefined;
  export let size: undefined | 'btn-sm' = undefined;
  export let disabled = false;
  export let customLoader = false;
</script>

<!-- https://daisyui.com/components/button -->
<button on:click {...$$restProps} class="btn whitespace-nowrap {variant ?? ''} {$$restProps.class ?? ''} {size ?? ''}" {type}
  class:btn-outline={outline}
  class:btn-active={active}
  disabled={disabled && !loading}
  class:pointer-events-none={loading || $$restProps.class?.includes('pointer-events-none')}>
  {#if !customLoader}
    <Loader {loading} />
  {/if}
  <slot />
</button>
