<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithoutChildrenOrChild} from 'bits-ui';
  import * as ResizablePrimitive from 'paneforge';
  import {Icon} from '../icon';
  import Button, {type ButtonProps} from '../button/button.svelte';
  import {Debounced} from 'runed';
  import {type IconClass} from '$lib/icon-class';
  import {onMount, tick} from 'svelte';

  let {
    ref = $bindable(null),
    class: className,
    withHandle = false,
    leftPane,
    rightPane,
    ...restProps
  }: WithoutChildrenOrChild<ResizablePrimitive.PaneResizerProps> & {
    withHandle?: boolean;
    leftPane?: ResizablePrimitive.Pane;
    rightPane?: ResizablePrimitive.Pane;
  } = $props();

  let dragging = $state(false);
  const draggingDebounced = new Debounced(() => dragging, 300);

  type OnClickHandler = ButtonProps['onclick'];
  const leftResizerOnClick = $derived.by(() => getPaneResizerOnClick(leftPane, rightPane));
  const rightResizerOnClick = $derived.by(() => getPaneResizerOnClick(rightPane, leftPane));
  function getPaneResizerOnClick(pane?: ResizablePrimitive.Pane, otherPane?: ResizablePrimitive.Pane): OnClickHandler {
    if (!pane || !otherPane) return undefined;
    if (pane.isCollapsed()) return undefined;
    return () => {
      if (otherPane.isCollapsed()) {
        otherPane.expand();
      } else {
        pane.collapse();
      }
    };
  }

  let defaultPaneProportions = $state<[number, number]>();

  onMount(async () => {
    await tick(); // one of the panes almost definitely gets mounted after us
    if (!leftPane || !rightPane) return;
    const totalSize = leftPane.getSize() + rightPane.getSize();
    defaultPaneProportions = [leftPane.getSize() / totalSize, rightPane.getSize() / totalSize];
  });

  function resetPanes() {
    if (!leftPane || !rightPane || !defaultPaneProportions) return;
    const totalSize = leftPane.getSize() + rightPane.getSize();
    leftPane.resize(defaultPaneProportions[0] * totalSize);
    rightPane.resize(defaultPaneProportions[1] * totalSize);
  }

  const showResizers = $derived(leftPane && rightPane && defaultPaneProportions && !draggingDebounced.current);
</script>


<ResizablePrimitive.PaneResizer
  onDraggingChange={(isDragging) => {
    dragging = isDragging;
    restProps?.onDraggingChange?.(isDragging);
  }}
  bind:ref
  class={cn(
    'bg-border focus-visible:ring-ring relative flex w-px items-center justify-center after:absolute after:inset-y-0 after:left-1/2 after:w-1 after:-translate-x-1/2 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-offset-1 data-[direction=vertical]:h-px data-[direction=vertical]:w-full data-[direction=vertical]:after:left-0 data-[direction=vertical]:after:h-1 data-[direction=vertical]:after:w-full data-[direction=vertical]:after:-translate-y-1/2 data-[direction=vertical]:after:translate-x-0 [&[data-direction=vertical]>div]:rotate-90',
    className,
  )}
  ondblclick={resetPanes}
  {...restProps}
>
  {#if withHandle}
    <div class="flex gap-1 items-center group z-10 min-w-3 overflow-hidden justify-center" class:hover:overflow-visible={showResizers} class:hover:min-w-max={showResizers}>
      {@render paneResizer(leftResizerOnClick, 'i-mdi-chevron-left')}
      <div class="bg-border z-10 flex h-4 w-3 items-center justify-center rounded-sm border">
        <Icon icon="i-mdi-drag-vertical" class="size-2.5" />
      </div>
      {@render paneResizer(rightResizerOnClick, 'i-mdi-chevron-right')}
    </div>

    {#snippet paneResizer(onclick: OnClickHandler, icon: IconClass)}
      <Button class={cn(
        'aspect-square p-0 rounded-full invisible transition-[visibility] delay-0 group-hover:delay-300 bg-accent/50',
        onclick && 'group-hover:visible',
      )} size="xs" {icon} variant="ghost" {onclick}></Button>
    {/snippet}
  {/if}
</ResizablePrimitive.PaneResizer>
