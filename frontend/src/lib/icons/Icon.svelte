<script lang="ts" context="module">
  // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
  export type IconSize = 'text-lg' | 'text-md';
</script>

<script lang="ts">
  import type { IconString } from '$lib/icons';

  export let icon: IconString | undefined = undefined;
  export let size: IconSize = 'text-lg';
</script>

{#if icon}
  <span class="{icon} {size}" />
{/if}
