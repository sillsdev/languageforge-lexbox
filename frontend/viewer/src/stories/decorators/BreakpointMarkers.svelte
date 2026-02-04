<script lang="ts">
  import type { Snippet } from 'svelte';

  let { children }: { children?: Snippet } = $props();
</script>

<!-- See sizes here: https://github.com/tailwindlabs/tailwindcss-container-queries?tab=readme-ov-file#configuration -->
<div class="breakpoint-marker w-[32rem] text-orange-600">@lg</div>
<div class="breakpoint-marker w-[48rem] text-green-600">@3xl</div>

{@render children?.()}

<style lang="postcss">
  @reference "../../app.css";
  .breakpoint-marker {
    @apply fixed h-full top-0 flex items-end justify-end border-r-current border-r -z-10 text-right pr-2;
  }
</style>
