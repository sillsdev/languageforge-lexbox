<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {ScrollArea as ScrollAreaPrimitive, type WithoutChild} from 'bits-ui';
  import {Scrollbar} from './index.js';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte.js';

  let {
    ref = $bindable(null),
    class: className,
    type: explicitType,
    orientation = 'vertical',
    scrollbarXClasses = '',
    scrollbarYClasses = '',
    children,
    ...restProps
  }: WithoutChild<ScrollAreaPrimitive.RootProps> & {
    orientation?: 'vertical' | 'horizontal' | 'both' | undefined;
    scrollbarXClasses?: string | undefined;
    scrollbarYClasses?: string | undefined;
  } = $props();

  const type = $derived(explicitType ?? (IsMobile.value ? 'scroll' : 'auto'));
</script>

<ScrollAreaPrimitive.Root {type} bind:ref {...restProps} class={cn('relative overflow-hidden', className)}>
  <ScrollAreaPrimitive.Viewport class="h-full w-full rounded-[inherit]">
    {@render children?.()}
  </ScrollAreaPrimitive.Viewport>
  {#if orientation === 'vertical' || orientation === 'both'}
    <Scrollbar orientation="vertical" class={scrollbarYClasses} />
  {/if}
  {#if orientation === 'horizontal' || orientation === 'both'}
    <Scrollbar orientation="horizontal" class={scrollbarXClasses} />
  {/if}
  <ScrollAreaPrimitive.Corner />
</ScrollAreaPrimitive.Root>
