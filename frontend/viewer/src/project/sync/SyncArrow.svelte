<script lang="ts">
  import {cn} from '$lib/utils';

  const {dir, tailLength = 20, size = 1, class: className}: {
    dir: 'up' | 'down' | 'left' | 'right',
    tailLength?: number,
    size?: number
    class?: string
  } = $props();
  const isVertical = $derived(dir === 'up' || dir === 'down');
  const isHorizontal = $derived(dir === 'left' || dir === 'right');
  // Base arrow head box is effectively ~16x16 (path spans ~0.08..15.92). Use 16 so the viewBox hugs the graphic.
  // This removes the horizontal whitespace we had when using 20.
  const headSize = 16; // canonical arrow-head box size tightly wrapping path
  const height = $derived(isVertical ? tailLength : headSize);
  const width = $derived(isHorizontal ? tailLength : headSize);
  // Actual rendered pixel dimensions: scale entire drawing uniformly by `size`.
  const boxHeight = $derived(height * size);
  const boxWidth = $derived(width * size);
</script>
<div class={cn(
    'block',
    (dir === 'down' || dir === 'right') && 'rotate-180',
    className,
)}>
  <svg xmlns="http://www.w3.org/2000/svg" class="max-w-full" width={boxWidth} height={boxHeight} viewBox="0 0 {width} {height}" preserveAspectRatio="xMidYMid meet">
    <rect width={width} height={height} fill="none"/>
    {#if isVertical}
      <path fill="currentColor"
            d="M 9 4 l 5.5 5.5 l 1.42 -1.42 L 8 0.16 L 0.08 8.08 L 1.5 9.5 L 7 4 V {tailLength} h 2 Z"/>
    {:else}
      <path fill="currentColor"
            d="M 4 7 l 5.5 -5.5 l -1.42 -1.42 L 0.16 8 L 8.08 15.92 L 9.5 14.5 L 4 9 H {tailLength} v -2 Z"/>
    {/if}
  </svg>
</div>
<style>

</style>
