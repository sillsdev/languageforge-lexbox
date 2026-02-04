<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {ContextMenu as ContextMenuPrimitive} from 'bits-ui';

  let {
    ref = $bindable(null),
    class: className,
    inset,
    ...restProps
  }: ContextMenuPrimitive.ItemProps & {
    inset?: boolean;
  } = $props();
</script>

<ContextMenuPrimitive.Item
  bind:ref
  class={cn(
    'data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground relative flex cursor-pointer select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50',
    inset && 'pl-8',
    className,
  )}
  {...restProps}
/>
