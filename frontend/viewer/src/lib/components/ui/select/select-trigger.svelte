<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {mergeProps, Select as SelectPrimitive, type WithoutChild} from 'bits-ui';
  import {Icon} from '../icon';
  import type {IconClass} from '$lib/icon-class';
  import XButton from '../button/x-button.svelte';
  import type {ComponentProps} from 'svelte';

  let {
    ref = $bindable(null),
    class: className,
    downIcon = 'i-mdi-chevron-down',
    children,
    onClear,
    style: styleProps,
    ...restProps
  }: WithoutChild<SelectPrimitive.TriggerProps> & {
    downIcon?: IconClass | null;
    onClear?: ComponentProps<typeof XButton>['onclick'];
  } = $props();

  // turn style into a normal html format
  const style = $derived(mergeProps({style: styleProps}).style);
</script>

<div class={cn('relative group w-full', className)} {style}>
  <SelectPrimitive.Trigger
    bind:ref
    class={cn(
      'border-input bg-background ring-offset-background data-[placeholder]:text-muted-foreground focus:ring-ring flex h-10 w-full items-center justify-between rounded-md border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 [&>span]:line-clamp-1',
      // matches the Button 'outline' variant
      'hover:bg-accent hover:text-accent-foreground',
    )}
    {...restProps}
  >
    {@render children?.()}
  </SelectPrimitive.Trigger>
  <div class="absolute right-0 top-1/2 -translate-y-1/2 flex items-center gap-0.5 pointer-events-none">
    {#if onClear && !restProps.disabled}
      <XButton class="pointer-events-auto group-has-[[data-placeholder]]:hidden" onclick={onClear} />
    {/if}
    {#if downIcon}
      <Icon icon={downIcon} class="mr-2 size-5 shrink-0 opacity-50" />
    {/if}
  </div>
</div>
