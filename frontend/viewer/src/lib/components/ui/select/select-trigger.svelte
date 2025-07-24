<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {Select as SelectPrimitive, type WithoutChild} from 'bits-ui';
  import {Icon} from '../icon';
  import type {IconClass} from '$lib/icon-class';

  let {
    ref = $bindable(null),
    class: className,
    downIcon = 'i-mdi-chevron-down',
    children,
    ...restProps
  }: WithoutChild<SelectPrimitive.TriggerProps> & {
    downIcon?: IconClass | null;
  } = $props();
</script>

<SelectPrimitive.Trigger
  bind:ref
  class={cn(
    'border-input bg-background ring-offset-background data-[placeholder]:text-muted-foreground focus:ring-ring flex h-10 w-full items-center justify-between rounded-md border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 [&>span]:line-clamp-1',
    className,
  )}
  {...restProps}
>
  {@render children?.()}
  {#if downIcon}
    <Icon icon={downIcon} class="size-4 opacity-50" />
  {/if}
</SelectPrimitive.Trigger>
