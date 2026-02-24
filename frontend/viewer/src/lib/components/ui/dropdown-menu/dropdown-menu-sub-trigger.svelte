<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {DropdownMenu as DropdownMenuPrimitive} from 'bits-ui';
  import {Icon} from '../icon';

  let {
    ref = $bindable(null),
    class: className,
    inset,
    children,
    ...restProps
  }: DropdownMenuPrimitive.SubTriggerProps & {
    inset?: boolean;
  } = $props();
</script>

<DropdownMenuPrimitive.SubTrigger
  bind:ref
  class={cn(
    'data-[highlighted]:bg-accent data-[state=open]:bg-accent flex cursor-default select-none items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0',
    inset && 'pl-8',
    className,
  )}
  {...restProps}
>
  {@render children?.()}
  <Icon icon="i-mdi-chevron-right" class="ml-auto" />
</DropdownMenuPrimitive.SubTrigger>
