<script lang="ts">
  import {cn, type WithoutChild} from '$lib/utils.js';
  import {mergeProps, Select as SelectPrimitive} from 'bits-ui';
  import {Icon} from '../icon';
  import type {IconClass} from '$lib/icon-class';
  import XButton from '../button/x-button.svelte';
  import type {ComponentProps} from 'svelte';

  let {
    ref = $bindable(null),
    class: className,
    children,
    size = 'default',
    downIcon = 'i-mdi-chevron-down',
    onClear,
    style: styleProps,
    ...restProps
  }: WithoutChild<SelectPrimitive.TriggerProps> & {
    size?: 'sm' | 'default';
    downIcon?: IconClass | null;
    onClear?: ComponentProps<typeof XButton>['onclick'];
  } = $props();

  // turn style into a normal html format
  const style = $derived(mergeProps({style: styleProps}).style);
</script>

<div class={cn('relative group w-full', className)} {style}>
  <SelectPrimitive.Trigger
    bind:ref
    data-slot="select-trigger"
    data-size={size}
    class={cn(
      "border-input data-placeholder:text-muted-foreground [&_svg:not([class*='text-'])]:text-muted-foreground focus-visible:border-ring focus-visible:ring-ring/50 aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive dark:bg-input/30 dark:hover:bg-input/50 flex w-fit items-center justify-between gap-2 rounded-md border bg-transparent px-3 py-2 text-sm whitespace-nowrap shadow-xs transition-[color,box-shadow] outline-none select-none focus-visible:ring-[3px] disabled:cursor-not-allowed disabled:opacity-50 data-[size=default]:h-9 data-[size=sm]:h-8 *:data-[slot=select-value]:line-clamp-1 *:data-[slot=select-value]:flex *:data-[slot=select-value]:items-center *:data-[slot=select-value]:gap-2 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
      // matches the Button 'outline' variant
      'hover:bg-accent hover:text-accent-foreground',
    )}
    {...restProps}
  >
    {@render children?.()}
  </SelectPrimitive.Trigger>
  <div class="absolute right-0 top-1/2 -translate-y-1/2 flex items-center gap-0.5 pointer-events-none">
    {#if onClear && !restProps.disabled}
      <XButton class="pointer-events-auto group-has-data-placeholder:hidden" onclick={onClear} />
    {/if}
    {#if downIcon}
      <Icon icon={downIcon} class="mr-2 size-5 shrink-0 opacity-50" />
    {/if}
  </div>
</div>
