<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithoutChildrenOrChild} from 'bits-ui';
  import * as ResizablePrimitive from 'paneforge';
  import {Icon} from '../icon';
  import Button from '../button/button.svelte';
  import {Debounced} from 'runed';

  let {
    ref = $bindable(null),
    class: className,
    canClickLeft = false,
    canClickRight = false,
    onClickLeft,
    onClickRight,
    withHandle = false,
    ...restProps
  }: WithoutChildrenOrChild<ResizablePrimitive.PaneResizerProps> & {
    withHandle?: boolean;
    canClickLeft?: boolean;
    canClickRight?: boolean;
    onClickLeft?: () => void;
    onClickRight?: () => void;
  } = $props();

  let dragging = $state(false);
  let draggingDebounced = new Debounced(() => dragging, 300);
</script>

<ResizablePrimitive.PaneResizer
  onDraggingChange={(isDragging) => {
    console.log(isDragging);
    dragging = isDragging;
    restProps?.onDraggingChange?.(isDragging);
  }}
  bind:ref
  class={cn(
    'bg-border focus-visible:ring-ring relative flex w-px items-center justify-center after:absolute after:inset-y-0 after:left-1/2 after:w-1 after:-translate-x-1/2 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-offset-1 data-[direction=vertical]:h-px data-[direction=vertical]:w-full data-[direction=vertical]:after:left-0 data-[direction=vertical]:after:h-1 data-[direction=vertical]:after:w-full data-[direction=vertical]:after:-translate-y-1/2 data-[direction=vertical]:after:translate-x-0 [&[data-direction=vertical]>div]:rotate-90',
    className,
  )}
  {...restProps}
>
  {#if withHandle}
    <div class="flex gap-1 items-center group z-10 min-w-3 overflow-hidden justify-center" class:hover:overflow-visible={!draggingDebounced.current} class:hover:min-w-max={!draggingDebounced.current}>
      <Button class={cn(
        'aspect-square p-0 rounded-full invisible transition-[visibility] delay-300 bg-accent/50',
        canClickLeft && 'group-hover:visible',
      )} size="xs" icon="i-mdi-chevron-left" variant="ghost" onclick={onClickLeft}></Button>
      <div class="bg-border z-10 flex h-4 w-3 items-center justify-center rounded-sm border">
        <Icon icon="i-mdi-drag-vertical" class="size-2.5" />
      </div>
      <Button class={cn(
        'aspect-square p-0 rounded-full invisible transition-[visibility] delay-300 bg-accent/50',
        canClickRight && 'group-hover:visible',
      )} size="xs" icon="i-mdi-chevron-right" variant="ghost" onclick={onClickRight}></Button>
    </div>
  {/if}
</ResizablePrimitive.PaneResizer>
