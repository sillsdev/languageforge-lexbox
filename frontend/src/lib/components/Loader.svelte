<script lang="ts">
  export let loading = false;
</script>

{#if loading}
  <span class="loading loading-spinner" />
{/if}
