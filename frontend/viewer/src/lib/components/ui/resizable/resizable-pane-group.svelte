<script lang="ts">
  import {cn} from '$lib/utils.js';
  import * as ResizablePrimitive from 'paneforge';

  let {
    anyColumnIsCollapsed = $bindable(false),
    ref = $bindable(null),
    this: paneGroup = $bindable(),
    class: className,
    ...restProps
  }: ResizablePrimitive.PaneGroupProps & {
    this?: ResizablePrimitive.PaneGroup;
    anyColumnIsCollapsed?: boolean;
  } = $props();
</script>

<ResizablePrimitive.PaneGroup
  onLayoutChange={(layout) => {
    anyColumnIsCollapsed = layout.some((column) => column === 0);
    restProps.onLayoutChange?.(layout);
  }}
  bind:this={paneGroup}
  class={cn('flex h-full w-full data-[direction=vertical]:flex-col', className)}
  {...restProps}
/>
