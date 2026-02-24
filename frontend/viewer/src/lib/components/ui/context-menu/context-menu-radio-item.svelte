<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils.js';
  import {ContextMenu as ContextMenuPrimitive, type WithoutChild} from 'bits-ui';

  let {
    ref = $bindable(null),
    class: className,
    children: childrenProp,
    ...restProps
  }: WithoutChild<ContextMenuPrimitive.RadioItemProps> = $props();
</script>

<ContextMenuPrimitive.RadioItem
  bind:ref
  class={cn(
    'data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground relative flex cursor-default select-none items-center rounded-sm py-1.5 pl-8 pr-2 text-sm outline-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50',
    className,
  )}
  {...restProps}
>
  {#snippet children({checked})}
    <span class="absolute left-2 flex size-3.5 items-center justify-center">
      {#if checked}
        <!-- If not centered, see the change made in radio-group-item.svelte -->
        <Icon icon="i-mdi-circle" class="size-3 text-current" />
      {/if}
    </span>
    {@render childrenProp?.({checked})}
  {/snippet}
</ContextMenuPrimitive.RadioItem>
