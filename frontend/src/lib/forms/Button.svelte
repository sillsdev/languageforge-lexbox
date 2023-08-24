<script lang="ts">
  import Loader from '$lib/components/Loader.svelte';

  export let loading = false;
  export let style: 'btn-primary' | 'btn-success' | 'btn-error' | undefined = undefined;
  export let type: undefined | 'submit' = undefined;
</script>

<!-- https://daisyui.com/components/button -->
<button on:click class="btn whitespace-nowrap {style ?? ''}" {type} {...$$restProps}>
  <Loader {loading} />
  <slot />
</button>
