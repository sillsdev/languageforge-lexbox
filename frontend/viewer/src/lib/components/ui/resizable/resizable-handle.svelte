<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithoutChildrenOrChild} from 'bits-ui';
  import * as ResizablePrimitive from 'paneforge';
  import {Icon} from '../icon';
  import Button, {type ButtonProps} from '../button/button.svelte';
  import {Debounced} from 'runed';
  import {type IconClass} from '$lib/icon-class';
  import {onMount, tick} from 'svelte';
  import {DEFAULT_DEBOUNCE_TIME} from '$lib/utils/time';

  let {
    ref = $bindable(null),
    class: className,
    withHandle = false,
    leftPane,
    rightPane,
    resetTo,
    ...restProps
  }: WithoutChildrenOrChild<ResizablePrimitive.PaneResizerProps> & {
    withHandle?: boolean;
    resetTo?: Readonly<[number, number]>;
    leftPane?: ResizablePrimitive.Pane;
    rightPane?: ResizablePrimitive.Pane;
  } = $props();

  let dragging = $state(false);
  const draggingDebounced = new Debounced(() => dragging, DEFAULT_DEBOUNCE_TIME);

  type OnClickHandler = ButtonProps['onclick'];
  const leftResizerOnClick = $derived(getPaneResizerOnClick(leftPane, rightPane));
  const rightResizerOnClick = $derived(getPaneResizerOnClick(rightPane, leftPane));
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

  onMount(async () => {
    await tick(); // one of the panes almost definitely gets mounted after us
    if (!leftPane || !rightPane) return;
    resetTo ??= [leftPane.getSize(), rightPane.getSize()];
  });

  function resetPanes() {
    if (!leftPane || !rightPane || !resetTo) return;
    leftPane.resize(resetTo[0]);
    // rightPane.resize(resetTo[1]); // redundant
  }

  const showResizers = $derived((leftResizerOnClick || rightResizerOnClick) && resetTo && !draggingDebounced.current);
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
    <div
      class="flex gap-1 items-center justify-center z-10 min-w-3 overflow-hidden transition-all delay-100 hover:delay-300 duration-0"
      class:hover:min-w-max={showResizers}
    >
      {@render paneResizer(leftResizerOnClick, 'i-mdi-chevron-left')}
      <div class="bg-border z-10 flex h-4 w-3 items-center justify-center rounded-sm border">
        <Icon icon="i-mdi-drag-vertical" class="size-2.5" />
      </div>
      {@render paneResizer(rightResizerOnClick, 'i-mdi-chevron-right')}
    </div>

    {#snippet paneResizer(onclick: OnClickHandler, icon: IconClass)}
      <Button
        class={cn('aspect-square p-0 rounded-full bg-accent/50', onclick || 'invisible')}
        size="xs"
        {icon}
        variant="ghost"
        {onclick}
      ></Button>
    {/snippet}
  {/if}
</ResizablePrimitive.PaneResizer>
