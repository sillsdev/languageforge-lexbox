<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {DropdownMenu as DropdownMenuPrimitive, type WithoutChild} from 'bits-ui';
  import {Icon} from '../icon';

  let {
    ref = $bindable(null),
    class: className,
    children: childrenProp,
    ...restProps
  }: WithoutChild<DropdownMenuPrimitive.RadioItemProps> = $props();
</script>

<DropdownMenuPrimitive.RadioItem
  bind:ref
  data-slot="dropdown-menu-radio-item"
  class={cn(
    "focus:bg-accent focus:text-accent-foreground relative flex cursor-default items-center gap-2 rounded-sm py-1.5 ps-8 pe-2 text-sm outline-hidden select-none data-disabled:pointer-events-none data-disabled:opacity-50 **:[[class*='i-mdi-']]:pointer-events-none *:[[class*='i-mdi-']]:shrink-0 [&_[class*='i-mdi-']:not([class*='size-'])]:size-4",
    className,
  )}
  {...restProps}
>
  {#snippet children({checked})}
    <span class="pointer-events-none absolute inset-s-2 flex size-3.5 items-center justify-center">
      {#if checked}
        <!-- If not centered, see the change made in radio-group-item.svelte -->
        <Icon icon="i-mdi-circle" class="size-2 text-current" />
      {/if}
    </span>
    {@render childrenProp?.({checked})}
  {/snippet}
</DropdownMenuPrimitive.RadioItem>
