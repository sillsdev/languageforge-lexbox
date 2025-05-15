<script lang="ts" module>
  // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
  export type IconSize = 'text-md' | 'text-lg' | 'text-xl' | 'text-2xl' | 'text-5xl';
</script>

<script lang="ts">
  import type { IconString } from '$lib/icons';

  interface Props {
    icon?: IconString;
    size?: IconSize;
    color?: `text-${string}`;
    pale?: boolean;
    spin?: boolean;
    spinReverse?: boolean;
    // For pixel perfect text alignment, because the svgs often contain vertical white-space
    y?: string;
  }

  const {
    icon,
    size = 'text-lg',
    color,
    pale = false,
    spin = false,
    spinReverse = false,
    y
  }: Props = $props();

  let transform = $derived(y ? `translateY(${y})` : '');
</script>

{#if icon}
  <span class="{icon} {size} {color ?? ''} shrink-0" class:rotate-180={spinReverse} class:pale style:transform class:animate-spin={spin}></span>
{/if}
