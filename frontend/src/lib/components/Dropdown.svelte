<script lang="ts">
  import { overlay } from '$lib/overlay';

  export let disabled = false;
</script>

<div class="dropdown-container inline-flex" use:overlay={{ disabled }}>
  <slot />
  <div class="overlay-content">
    <slot name="content" />
  </div>
</div>

<style>
  :global(.dropdown-container > *) {
    max-width: 100%;
    flex-grow: 1;
  }
</style>
