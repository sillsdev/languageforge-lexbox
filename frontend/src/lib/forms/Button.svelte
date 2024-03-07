<script lang="ts">
  import Loader from '$lib/components/Loader.svelte';

  export let loading = false;
  export let variant: 'btn-primary' | 'btn-success' | 'btn-error' | 'btn-ghost' | undefined = undefined;
  export let outline = false;
  export let type: undefined | 'submit' = undefined;
  export let size: undefined | 'btn-sm' = undefined;
  export let disabled = false;
</script>

<!-- https://daisyui.com/components/button -->
<button on:click {...$$restProps} class="btn whitespace-nowrap {variant ?? ''} {$$restProps.class ?? ''} {size ?? ''}" {type}
  class:btn-outline={outline}
  disabled={disabled && !loading}
  class:pointer-events-none={loading}>
  <Loader {loading} />
  <slot />
</button>
