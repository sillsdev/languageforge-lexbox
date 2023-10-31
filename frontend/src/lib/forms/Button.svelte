<script lang="ts">
  import Loader from '$lib/components/Loader.svelte';

  export let loading = false;
  export let style: 'btn-primary' | 'btn-success' | 'btn-error' | undefined = undefined;
  export let type: undefined | 'submit' = undefined;
  export let disabled = false;
</script>

<!-- https://daisyui.com/components/button -->
<button on:click {...$$restProps} class="btn whitespace-nowrap {style ?? ''} {$$restProps.class ?? ''}" {type} {disabled}>
  <Loader {loading} />
  <slot />
</button>
