<script lang="ts">
  export let grid = false;
</script>

<span class:grid class:inline-flex={!grid} class="badge-list flex-wrap gap-3 justify-items-stretch">
  <slot />
</span>

<style>
  .badge-list {
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  }
</style>
