<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import {cn, type WithoutChild} from '$lib/utils.js';
  import {ContextMenu as ContextMenuPrimitive} from 'bits-ui';

  let {
    ref = $bindable(null),
    class: className,
    children: childrenProp,
    ...restProps
  }: WithoutChild<ContextMenuPrimitive.RadioItemProps> = $props();
</script>

<ContextMenuPrimitive.RadioItem
  bind:ref
  data-slot="context-menu-radio-item"
  class={cn(
    "data-highlighted:bg-accent data-highlighted:text-accent-foreground relative flex cursor-default items-center gap-2 rounded-sm py-1.5 ps-8 pe-2 text-sm outline-hidden select-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
    className,
  )}
  {...restProps}
>
  {#snippet children({checked})}
    <span class="pointer-events-none absolute start-2 flex size-3.5 items-center justify-center">
      {#if checked}
        <!-- If not centered, see the change made in radio-group-item.svelte -->
        <Icon icon="i-mdi-circle" class="size-3 text-current" />
      {/if}
    </span>
    {@render childrenProp?.({checked})}
  {/snippet}
</ContextMenuPrimitive.RadioItem>
