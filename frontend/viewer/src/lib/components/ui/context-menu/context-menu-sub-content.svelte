<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {ContextMenu as ContextMenuPrimitive} from 'bits-ui';

  let {ref = $bindable(null), class: className, ...restProps}: ContextMenuPrimitive.SubContentProps = $props();
</script>

<ContextMenuPrimitive.SubContent
  bind:ref
  class={cn(
    'bg-popover text-popover-foreground z-50 min-w-[8rem] overflow-hidden rounded-md border p-1 shadow-md focus:outline-none',
    className,
  )}
  {...restProps}
/>
