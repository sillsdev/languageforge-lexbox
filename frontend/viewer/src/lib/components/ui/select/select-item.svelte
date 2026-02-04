<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Select as SelectPrimitive, type WithoutChild} from 'bits-ui';
  import {Icon} from '../icon';

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
  class={cn(
    'data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground relative flex w-full cursor-default select-none items-center rounded-sm py-1.5 pl-8 pr-2 text-sm outline-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50',
    className,
  )}
  {...restProps}
>
  {#snippet children({selected, highlighted})}
    <span class="absolute left-2 flex size-3.5 items-center justify-center">
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
