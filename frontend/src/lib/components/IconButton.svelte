<script lang="ts">
  import type { CssClassList } from '$lib/type.utils';
  import Loader from './Loader.svelte';

  // See https://icon-sets.iconify.design/mdi/
  export let icon: `i-mdi-${string}`;
  export let disabled = false;
  export let loading = false;
  export let style: CssClassList<'btn-success', 'btn-ghost' | 'btn-outline'> = 'btn-outline';
  export let active = false;
  export let join = false;

</script>

<button class:disabled={disabled || loading} on:click class="btn btn-square {style}" class:btn-active={active} class:join-item={join}>
  {#if !loading}
    <span class="{icon} text-lg" />
  {:else}
    <Loader loading />
  {/if}
</button>

<style lang="postcss">
  .disabled {
    pointer-events: none;
  }
</style>
