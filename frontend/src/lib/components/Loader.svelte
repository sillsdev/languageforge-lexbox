<script lang="ts">
  export let loading = false;
  export let size: undefined | 'loading-xs' = undefined;
</script>

{#if loading}
  <span class="loading loading-spinner {size ?? ''}" />
{/if}
