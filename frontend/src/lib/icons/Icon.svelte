<script lang="ts" context="module">
  // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
  export type IconSize = 'text-md' | 'text-lg' | 'text-xl' | 'text-2xl' | 'text-5xl';
</script>

<script lang="ts">
  import type { IconString } from '$lib/icons';

  export let icon: IconString | undefined = undefined;
  export let size: IconSize = 'text-lg';
  export let color: `text-${string}` | undefined = undefined;
  export let pale = false;
  export let spin = false;
  export let spinReverse = false;
  // For pixel perfect text alignment, because the svgs often contain vertical white-space
  export let y: string | undefined = undefined;

  $: transform = y ? `translateY(${y})` : '';
</script>

{#if icon}
  <span class="{icon} {size} {color ?? ''} shrink-0 {spinReverse ? 'transform rotate-180' : ''}" class:pale style:transform class:animate-spin={spin} />
{/if}
