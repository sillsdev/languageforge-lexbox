<script lang="ts">
  import Loader from '$lib/components/Loader.svelte';

  export let loading = false;
  export let style: 'btn-primary' | 'btn-success' | 'btn-error' | 'btn-outline' | 'btn-ghost' | undefined = undefined;
  export let type: undefined | 'submit' = undefined;
  export let size: undefined | 'btn-sm' = undefined;
  export let disabled = false;
</script>

<!-- https://daisyui.com/components/button -->
<button on:click {...$$restProps} class="btn whitespace-nowrap {style ?? ''} {$$restProps.class ?? ''} {size ?? ''}" {type}
  disabled={disabled && !loading}
  class:pointer-events-none={loading}>
  <Loader {loading} />
  <slot />
</button>
