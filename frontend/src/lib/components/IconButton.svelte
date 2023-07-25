<script lang="ts">
  import Loader from './Loader.svelte';

  // See https://icon-sets.iconify.design/mdi/
  export let icon: `i-mdi-${string}`;
  export let disabled = false;
  export let loading = false;
  export let ghost = false;
</script>

<button {disabled} on:click class="btn btn-square {ghost ? 'btn-ghost' : 'btn-outline'}">
  {#if !loading}
    <span class="{icon} text-lg" />
  {:else}
    <Loader loading />
  {/if}
</button>
