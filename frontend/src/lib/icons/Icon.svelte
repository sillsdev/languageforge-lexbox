<script lang="ts" module>
  // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
  export type IconSize = 'text-md' | 'text-lg' | 'text-xl' | 'text-2xl' | 'text-5xl';
</script>

<script lang="ts">
  import type { IconString } from '$lib/icons';

  interface Props {
    icon?: IconString | undefined;
    size?: IconSize;
    color?: `text-${string}` | undefined;
    pale?: boolean;
    spin?: boolean;
    spinReverse?: boolean;
    // For pixel perfect text alignment, because the svgs often contain vertical white-space
    y?: string | undefined;
  }

  const {
    icon = undefined,
    size = 'text-lg',
    color = undefined,
    pale = false,
    spin = false,
    spinReverse = false,
    y = undefined
  }: Props = $props();

  let transform = $derived(y ? `translateY(${y})` : '');
</script>

{#if icon}
  <span class="{icon} {size} {color ?? ''} shrink-0" class:rotate-180={spinReverse} class:pale style:transform class:animate-spin={spin}></span>
{/if}
