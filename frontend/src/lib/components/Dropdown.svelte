<script lang="ts">
  import { overlay } from '$lib/overlay';

  export let disabled = false;
</script>

<div use:overlay={{disabled}}>
  <slot />
  <div class="overlay-content">
    <slot name="content" />
  </div>
</div>
