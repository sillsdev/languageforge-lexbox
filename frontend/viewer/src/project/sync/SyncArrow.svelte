<script lang="ts">
  import {cn} from '$lib/utils';

  const {dir, tailLength = 20, size = 1, class: className}: {
    dir: 'up' | 'down' | 'left' | 'right',
    tailLength: number,
    size: number
    class?: string
  } = $props();
  const height = $derived(tailLength);
</script>
<div class={cn(
    'block',
    dir === 'right' && 'rotate-0',
    dir === 'down' && 'rotate-180',
    dir === 'left' && 'rotate-180',
    dir === 'up' && 'rotate-0',
    className,
)}>
  <svg xmlns="http://www.w3.org/2000/svg" width="{size * 20}px" height="{(size * 20) + height - 20}px" viewBox="0 0 20 {height}">
    <rect width="20" height={height} fill="none"/>
    <path fill="currentColor" d="M 9 4 l 5.5 5.5 l 1.42 -1.42 L 8 0.16 L 0.08 8.08 L 1.5 9.5 L 7 4 V {tailLength} h 2 Z"/>
  </svg>
</div>
<style>

</style>
