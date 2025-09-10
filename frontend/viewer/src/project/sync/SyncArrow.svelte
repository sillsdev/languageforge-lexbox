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
  const height = $derived(isVertical ? tailLength : 20);
  const width = $derived(isHorizontal ? tailLength : 20);
  const boxHeight = $derived((size * 20) + height - 20);
  const boxWidth = $derived((size * 20) + width - 20);
</script>
<div class={cn(
    'block',
    (dir === 'down' || dir === 'right') && 'rotate-180',
    className,
)}>
  <svg xmlns="http://www.w3.org/2000/svg" width="{boxWidth}px" height="{boxHeight}px" viewBox="0 0 {width} {height}">
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
