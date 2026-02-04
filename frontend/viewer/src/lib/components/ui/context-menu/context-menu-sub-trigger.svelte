<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils.js';
  import {ContextMenu as ContextMenuPrimitive, type WithoutChild} from 'bits-ui';

  let {
    ref = $bindable(null),
    class: className,
    inset,
    children,
    ...restProps
  }: WithoutChild<ContextMenuPrimitive.SubTriggerProps> & {
    inset?: boolean;
  } = $props();
</script>

<ContextMenuPrimitive.SubTrigger
  bind:ref
  class={cn(
    'data-[highlighted]:bg-accent data-[state=open]:bg-accent data-[highlighted]:text-accent-foreground data-[state=open]:text-accent-foreground flex cursor-default select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none',
    inset && 'pl-8',
    className,
  )}
  {...restProps}
>
  {@render children?.()}
  <Icon icon="i-mdi-chevron-right" class="size-4 ml-auto" />
</ContextMenuPrimitive.SubTrigger>
