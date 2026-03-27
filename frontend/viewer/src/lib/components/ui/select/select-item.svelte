<script lang="ts">
  import {Icon} from '../icon';
  import {Select as SelectPrimitive} from 'bits-ui';
  import {cn, type WithoutChild} from '$lib/utils.js';

  let {
    ref = $bindable(null),
    class: className,
    value,
    label,
    children: childrenProp,
    ...restProps
  }: WithoutChild<SelectPrimitive.ItemProps> = $props();
</script>

<SelectPrimitive.Item
  bind:ref
  {value}
  data-slot="select-item"
  class={cn(
    "data-highlighted:bg-accent data-highlighted:text-accent-foreground [&>[class*='i-mdi-']:not([class*='text-'])]:text-muted-foreground relative flex w-full cursor-default items-center gap-2 rounded-sm py-1.5 ps-2 pe-8 text-sm outline-hidden select-none data-disabled:pointer-events-none data-disabled:opacity-50 **:[[class*='i-mdi-']]:pointer-events-none *:[[class*='i-mdi-']]:shrink-0 [&_[class*='i-mdi-']:not([class*='size-'])]:size-4 *:[span]:last:flex *:[span]:last:items-center *:[span]:last:gap-2",
    className,
  )}
  {...restProps}
>
  {#snippet children({selected, highlighted})}
    <span class="absolute inset-e-2 flex size-3.5 items-center justify-center">
      {#if selected}
        <Icon icon="i-mdi-check" class="size-4" />
      {/if}
    </span>
    {#if childrenProp}
      {@render childrenProp({selected, highlighted})}
    {:else}
      {label || value}
    {/if}
  {/snippet}
</SelectPrimitive.Item>
